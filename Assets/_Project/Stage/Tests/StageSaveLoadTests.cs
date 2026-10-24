using System;
using System.Collections.Generic;
using System.IO;
using DeskStage.Shared;
using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Stage.Tests
{
    /// <summary>ステージの確かめ・一覧・読み込み・保存・消す（B の Step 10・11）。</summary>
    public class StageSaveLoadTests
    {
        string bundled;
        string saved;

        static string SampleJson() => File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Stages", "sample-01", "stage.json"));

        static StageData Sample() => JsonUtility.FromJson<StageData>(SampleJson());

        [SetUp]
        public void MakeFolders()
        {
            var root = Path.Combine(Path.GetTempPath(), "DeskStageTests", Guid.NewGuid().ToString("N"));
            bundled = Path.Combine(root, "bundled");
            saved = Path.Combine(root, "saved");
            Directory.CreateDirectory(Path.Combine(bundled, "sample-01"));
            File.WriteAllText(Path.Combine(bundled, "sample-01", "stage.json"), SampleJson());
            File.WriteAllText(Path.Combine(bundled, "sample-01", "stage.json.meta"), "meta");
            WriteIndex(1);
        }

        [TearDown]
        public void RemoveFolders()
        {
            Directory.Delete(Path.GetDirectoryName(bundled), true);
        }

        void WriteIndex(int revision)
        {
            File.WriteAllText(Path.Combine(bundled, "index.json"),
                "{\"stages\":[{\"id\":\"sample-01\",\"revision\":" + revision + "}]}");
        }

        // --- 確かめ ---

        [Test]
        public void お手本ステージは読み込める()
        {
            Assert.That(StageValidator.Validate(Sample(), "sample-01", out var reason), Is.True, reason);
        }

        [Test]
        public void version1は古い形式()
        {
            var s = Sample();
            s.version = 1;

            Assert.That(StageValidator.Validate(s, "sample-01", out var reason), Is.False);
            Assert.That(reason, Is.EqualTo("古い形式"));
        }

        [Test]
        public void IDがフォルダ名と違うと読み込めない()
        {
            Assert.That(StageValidator.Validate(Sample(), "other", out _), Is.False);
        }

        [Test]
        public void 範囲を外れた箱があると読み込めない()
        {
            var s = Sample();
            s.boxes[0].size[0] = 0.9f;

            Assert.That(StageValidator.Validate(s, "sample-01", out _), Is.False);
        }

        [Test]
        public void 座標の配列の長さが3でないと読み込めない()
        {
            var s = Sample();
            s.boxes[0].center = new[] { 0f, 0.01f };

            Assert.That(StageValidator.Validate(s, "sample-01", out _), Is.False);
        }

        [Test]
        public void ゴールがないと読み込めない()
        {
            var s = Sample();
            s.goal = new GoalData();

            Assert.That(StageValidator.Validate(s, "sample-01", out var reason), Is.False);
            Assert.That(reason, Is.EqualTo("ゴールがありません"));
        }

        // --- 同梱用のコピー ---

        [Test]
        public void 初回は同梱用をコピーしmetaは写さない()
        {
            var storage = new StageStorage(bundled, saved);

            Assert.That(storage.CopyBundled(), Is.EqualTo(1));
            Assert.That(File.Exists(Path.Combine(saved, "sample-01", "stage.json")), Is.True);
            Assert.That(File.Exists(Path.Combine(saved, "sample-01", "stage.json.meta")), Is.False);
        }

        [Test]
        public void 二回目はコピーせず更新番号が上がったらコピーする()
        {
            var storage = new StageStorage(bundled, saved);
            storage.CopyBundled();

            Assert.That(storage.CopyBundled(), Is.EqualTo(0));

            WriteIndex(2);
            Assert.That(storage.CopyBundled(), Is.EqualTo(1));
        }

        [Test]
        public void 消した同梱用のステージは更新番号が上がるまで戻らない()
        {
            var storage = new StageStorage(bundled, saved);
            storage.CopyBundled();
            storage.Delete("sample-01");

            storage.CopyBundled();

            Assert.That(Directory.Exists(Path.Combine(saved, "sample-01")), Is.False);
        }

        // --- 保存・一覧・読み込み・消す ---

        [Test]
        public void 保存して読み込むと同じ箱とコースになる()
        {
            var storage = new StageStorage(bundled, saved);
            var current = Sample();
            var stage = StageSavePrep.Prepare(current, new List<BoxData>(), "たかい かべ", new DateTimeOffset(2026, 10, 24, 18, 30, 5, TimeSpan.FromHours(9)));

            Assert.That(storage.Save(stage, out var reason), Is.True, reason);
            Assert.That(storage.TryLoad("stage-20261024-183005", out var loaded, out reason), Is.True, reason);

            Assert.That(loaded.name, Is.EqualTo("たかい かべ"));
            Assert.That(loaded.createdAt, Is.EqualTo("2026-10-24T18:30:05+09:00"));
            Assert.That(loaded.boxes.Length, Is.EqualTo(current.boxes.Length));
            Assert.That(Vec3.To(loaded.boxes[2].size), Is.EqualTo(Vec3.To(current.boxes[2].size)));
            Assert.That(Vec3.To(loaded.goal.position), Is.EqualTo(Vec3.To(current.goal.position)));
        }

        [Test]
        public void 二回目の保存では前の中身がbakに残る()
        {
            var storage = new StageStorage(bundled, saved);
            var now = new DateTimeOffset(2026, 10, 24, 18, 0, 0, TimeSpan.FromHours(9));
            storage.Save(StageSavePrep.Prepare(Sample(), new List<BoxData>(), "1回目", now), out _);

            storage.Save(StageSavePrep.Prepare(Sample(), new List<BoxData>(), "2回目", now), out _);

            var dir = Path.Combine(saved, "stage-20261024-180000");
            Assert.That(File.ReadAllText(Path.Combine(dir, "stage.json")), Does.Contain("2回目"));
            Assert.That(File.ReadAllText(Path.Combine(dir, "stage.json.bak")), Does.Contain("1回目"));
            Assert.That(File.Exists(Path.Combine(dir, "stage.json.tmp")), Is.False);
        }

        [Test]
        public void ゴールがないステージは保存しない()
        {
            var storage = new StageStorage(bundled, saved);
            var current = Sample();
            current.goal = new GoalData();

            var ok = storage.Save(StageSavePrep.Prepare(current, new List<BoxData>(), "x", DateTimeOffset.Now), out var reason);

            Assert.That(ok, Is.False);
            Assert.That(reason, Is.EqualTo("ゴールがありません"));
        }

        [Test]
        public void 一覧は新しい順で読み込めないステージも理由付きで入る()
        {
            var storage = new StageStorage(bundled, saved);
            storage.CopyBundled();
            storage.Save(StageSavePrep.Prepare(Sample(), new List<BoxData>(), "あたらしい", new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero)), out _);
            Directory.CreateDirectory(Path.Combine(saved, "broken"));
            File.WriteAllText(Path.Combine(saved, "broken", "stage.json"), "{ こわれている");

            var list = storage.List();

            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[0].name, Is.EqualTo("あたらしい"));
            var broken = list.Find(s => s.id == "broken");
            Assert.That(broken.loadable, Is.False);
            Assert.That(broken.reason, Is.Not.Empty);
        }

        [Test]
        public void フォルダの外を指すIDは読まず消さない()
        {
            var storage = new StageStorage(bundled, saved);
            storage.CopyBundled();

            Assert.That(storage.TryLoad("..", out _, out _), Is.False);
            Assert.That(storage.Delete(".."), Is.False);
            Assert.That(storage.Delete("../bundled"), Is.False);
            Assert.That(Directory.Exists(bundled), Is.True);
        }

        // --- 保存の中身 ---

        [Test]
        public void 保存のときは1mmと0点1度に丸める()
        {
            var boxes = new List<BoxData>
            {
                new BoxData { id = "box-01", center = new[] { 0.12345f, 0.0125f, -0.0004f }, size = new[] { 0.2f, 0.025f, 0.1f }, yawDegrees = 359.97f },
            };

            var stage = StageSavePrep.Prepare(Sample(), boxes, "", DateTimeOffset.Now);

            Assert.That(stage.boxes[0].center[0], Is.EqualTo(0.123f).Within(1e-6f));
            Assert.That(stage.boxes[0].center[2], Is.EqualTo(0f).Within(1e-6f));
            Assert.That(stage.boxes[0].yawDegrees, Is.EqualTo(0f));
            Assert.That(stage.name, Is.EqualTo(stage.id), "表示名が空ならステージ ID にする");
        }
    }
}
