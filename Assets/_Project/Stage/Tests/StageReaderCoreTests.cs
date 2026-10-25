using System.IO;
using DeskStage.Shared;
using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Stage.Tests
{
    /// <summary>StageReaderCore：Claude の答えから箱を作る（B の Step 7）。</summary>
    public class StageReaderCoreTests
    {
        const float Tolerance = 1e-4f;

        static string AnswerFile(string name)
        {
            return File.ReadAllText(Path.Combine(Application.dataPath, "_Project", "Stage", "Tests", "Data", name));
        }

        static string OneBlock(string block, string desk = "null", bool paper = true)
        {
            return "{\"paperFound\":" + (paper ? "true" : "false") + ",\"scaleReference\":\"A4 の紙\",\"desk\":" + desk
                + ",\"blocks\":[" + block + "],\"notes\":\"\"}";
        }

        static string Block(float x, float z, float w, float d, float h, float yaw = 0f)
        {
            return "{\"label\":\"箱\",\"centerXCm\":" + x + ",\"centerZCm\":" + z + ",\"widthCm\":" + w + ",\"depthCm\":" + d
                + ",\"heightCm\":" + h + ",\"yawDegrees\":" + yaw + ",\"confidence\":0.8}";
        }

        static void AssertVector(Vector3 expected, float[] actual)
        {
            Assert.That(Vector3.Distance(expected, Vec3.To(actual)), Is.LessThan(Tolerance), $"expected {expected} but was {Vec3.To(actual)}");
        }

        [Test]
        public void 本物の答えから3つの箱ができる()
        {
            var result = new StageReaderCore().Read(AnswerFile("answer-photo-01.json"));

            Assert.That(result.Ok, Is.True, result.Reason);
            Assert.That(result.Boxes.Count, Is.EqualTo(3));
            Assert.That(result.Boxes[0].id, Is.EqualTo("box-01"));
            Assert.That(result.Boxes[2].id, Is.EqualTo("box-03"));
            Assert.That(result.PaperFound, Is.False);
            Assert.That(result.ScaleReference, Is.Not.Empty);
            // 机は物を囲む長方形（X 0〜28、Z 0〜48.5 cm）に 10cm の余白
            Assert.That(result.DeskSize.x, Is.EqualTo(0.48f).Within(Tolerance));
            Assert.That(result.DeskSize.y, Is.EqualTo(0.685f).Within(Tolerance));
            // 大きな段ボール箱：中心 (15, 37.5) cm → 机の中心 (14, 24.25) cm からの差
            AssertVector(new Vector3(0.01f, 0.13f, 0.1325f), result.Boxes[2].center);
            AssertVector(new Vector3(0.26f, 0.26f, 0.22f), result.Boxes[2].size);
            Assert.That(result.Boxes[2].source, Is.EqualTo(BoxData.SourceAuto));
        }

        [Test]
        public void 本物の答えの手前の2つの箱のすき間がふさがる()
        {
            var result = new StageReaderCore().Read(AnswerFile("answer-photo-01.json"));

            // マーカーの箱（低い方）の奥の辺が、細長い箱の手前の辺まで広がる
            var marker = result.Boxes[0];
            var longBox = result.Boxes[1];
            var markerBack = marker.center[2] + marker.size[2] / 2f;
            var longFront = longBox.center[2] - longBox.size[2] / 2f;
            Assert.That(markerBack, Is.EqualTo(longFront).Within(Tolerance));
        }

        [Test]
        public void 紙と机があるときは机の中心が原点になる()
        {
            var json = OneBlock(Block(45f, 22f, 20f, 10f, 3f), "{\"widthCm\":90,\"depthCm\":44}");

            var result = new StageReaderCore().Read(json);

            Assert.That(result.Ok, Is.True, result.Reason);
            Assert.That(result.DeskSize.x, Is.EqualTo(0.9f).Within(Tolerance));
            Assert.That(result.DeskSize.y, Is.EqualTo(0.44f).Within(Tolerance));
            AssertVector(new Vector3(0f, 0.015f, 0f), result.Boxes[0].center);
            AssertVector(new Vector3(0.2f, 0.03f, 0.1f), result.Boxes[0].size);
        }

        [Test]
        public void 反時計回りの回転がUnityの向きに直る()
        {
            var result = new StageReaderCore().Read(OneBlock(Block(30f, 20f, 20f, 10f, 3f, 30f)));

            Assert.That(result.Boxes[0].yawDegrees, Is.EqualTo(330f).Within(1e-3f));
        }

        [Test]
        public void 範囲を外れた物は箱にしない()
        {
            var blocks = Block(20f, 20f, 80f, 10f, 3f)      // 幅が 60cm を超える
                + "," + Block(40f, 20f, 10f, 10f, 0.3f)    // 高さが 0.5cm 未満
                + "," + Block(60f, 20f, 10f, 10f, 3f);     // これだけ残る

            var result = new StageReaderCore().Read(OneBlock(blocks));

            Assert.That(result.Ok, Is.True, result.Reason);
            Assert.That(result.Boxes.Count, Is.EqualTo(1));
            Assert.That(result.Skipped, Is.EqualTo(2));
        }

        [Test]
        public void 中心が机の外の物は箱にしない()
        {
            var blocks = Block(45f, 22f, 10f, 10f, 3f) + "," + Block(120f, 22f, 10f, 10f, 3f);

            var result = new StageReaderCore().Read(OneBlock(blocks, "{\"widthCm\":90,\"depthCm\":44}"));

            Assert.That(result.Boxes.Count, Is.EqualTo(1));
            Assert.That(result.Skipped, Is.EqualTo(1));
        }

        [Test]
        public void 紙がなくても箱を作る()
        {
            var result = new StageReaderCore().Read(OneBlock(Block(20f, 20f, 10f, 10f, 3f), paper: false));

            Assert.That(result.Ok, Is.True, result.Reason);
            Assert.That(result.Boxes.Count, Is.EqualTo(1));
        }

        [Test]
        public void 物がない答えは失敗にする()
        {
            var result = new StageReaderCore().Read(OneBlock(""));

            Assert.That(result.Ok, Is.False);
            Assert.That(result.Reason, Is.Not.Empty);
        }

        [Test]
        public void 壊れたJSONでも落ちずに失敗にする()
        {
            StageReadResult result = null;
            Assert.DoesNotThrow(() => result = new StageReaderCore().Read("{ これは JSON ではない"));

            Assert.That(result.Ok, Is.False);
        }

        [Test]
        public void 写真の中の位置が箱のidごとに入る()
        {
            const string quad = "{\"x0\":0.2,\"y0\":0.2,\"x1\":0.4,\"y1\":0.2,\"x2\":0.4,\"y2\":0.4,\"x3\":0.2,\"y3\":0.4}";
            const string zero = "{\"x0\":0,\"y0\":0,\"x1\":0,\"y1\":0,\"x2\":0,\"y2\":0,\"x3\":0,\"y3\":0}";
            var json = "{\"paperFound\":true,\"scaleReference\":\"A4\",\"desk\":null,\"deskInPhoto1\":" + quad
                + ",\"blocks\":["
                + "{\"label\":\"箱\",\"centerXCm\":10,\"centerZCm\":10,\"widthCm\":8,\"depthCm\":6,\"heightCm\":5,\"yawDegrees\":0,\"confidence\":0.8,\"topFaceInPhoto1\":" + quad + "},"
                + "{\"label\":\"箱\",\"centerXCm\":30,\"centerZCm\":10,\"widthCm\":8,\"depthCm\":6,\"heightCm\":9,\"yawDegrees\":0,\"confidence\":0.8,\"topFaceInPhoto1\":" + zero + "}"
                + "],\"notes\":\"\"}";

            var result = new StageReaderCore().Read(json);

            Assert.That(result.Ok, Is.True, result.Reason);
            Assert.That(result.DeskQuad, Is.Not.Null);
            // 1つ目の箱だけ、位置が分かる（2つ目は 8 つとも 0 ＝分からない）
            Assert.That(result.TopFaceQuads.Count, Is.EqualTo(1));
            Assert.That(result.TopFaceQuads.ContainsKey("box-01"), Is.True);
        }

        [Test]
        public void 写真の中の位置が無い答えでも箱は作れる()
        {
            // 位置の項目が無い古い形の答え
            var result = new StageReaderCore().Read(AnswerFile("answer-photo-01.json"));

            Assert.That(result.Ok, Is.True, result.Reason);
            Assert.That(result.DeskQuad, Is.Null);
            Assert.That(result.TopFaceQuads.Count, Is.EqualTo(0));
        }
    }
}
