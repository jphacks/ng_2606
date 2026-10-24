using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DeskStage.AR.Tests
{
    /// <summary>SurfaceStabilizerCore（面の安定化の計算）の確認。A の Step 11。</summary>
    public class SurfaceStabilizerCoreTests
    {
        const float Tol = 1e-3f;
        static readonly Vector2 Desk = new Vector2(1.2f, 0.6f); // 原点が中心

        ScanSettings m_Settings;
        SurfaceStabilizerCore m_Core;
        float m_Now;

        [SetUp]
        public void SetUp()
        {
            m_Settings = ScriptableObject.CreateInstance<ScanSettings>();
            m_Core = new SurfaceStabilizerCore(m_Settings);
            m_Now = 0f;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(m_Settings);

        /// <summary>中心 (cx, cz)、幅 w、奥行き d の長方形（ステージ座標の XZ）。</summary>
        static RawSurface Rect(int id, float height, float cx, float cz, float w, float d)
        {
            return new RawSurface
            {
                SourceId = id,
                Height = height,
                Outline = new[]
                {
                    new Vector2(cx - w / 2f, cz - d / 2f), new Vector2(cx + w / 2f, cz - d / 2f),
                    new Vector2(cx + w / 2f, cz + d / 2f), new Vector2(cx - w / 2f, cz + d / 2f),
                },
            };
        }

        /// <summary>同じ面を渡し続けて、落ち着くまで時間を進める。</summary>
        void Settle(params RawSurface[] raw)
        {
            m_Core.Update(m_Now, Desk, raw);
            m_Now += m_Settings.settleTime + 0.1f;
            m_Core.Update(m_Now, Desk, raw);
        }

        [Test]
        public void 本2冊を並べた形は2つになる()
        {
            // 幅 0.15 の本を、0.05 m 離して並べる（すき間 0.05 > 0.01 なのでまとめない）
            Settle(Rect(1, 0.03f, -0.1f, 0f, 0.15f, 0.2f), Rect(2, 0.03f, 0.1f, 0f, 0.15f, 0.2f));
            Assert.AreEqual(2, m_Core.Surfaces.Count);
        }

        [Test]
        public void 高さの差が大きい重なりは別の面のまま残る()
        {
            // 本の上に本を重ねた形：下の本の上面（0.03）と上の本の上面（0.06）。高さの差 0.03 > 0.01
            Settle(Rect(1, 0.03f, 0f, 0f, 0.2f, 0.15f), Rect(2, 0.06f, 0f, 0f, 0.1f, 0.08f));
            Assert.AreEqual(2, m_Core.Surfaces.Count);
            Assert.AreEqual(0.03f, m_Core.Surfaces[0].Height, Tol);
            Assert.AreEqual(0.06f, m_Core.Surfaces[1].Height, Tol);
        }

        [Test]
        public void 高さが近く重なる2枚は1つにまとまり高さは面積の大きい方に合う()
        {
            // 同じ物の上面が、高さ 0.050 と 0.055 で重なって見つかった
            Settle(Rect(1, 0.050f, 0f, 0f, 0.2f, 0.2f), Rect(2, 0.055f, 0.05f, 0f, 0.1f, 0.1f));
            Assert.AreEqual(1, m_Core.Surfaces.Count);
            Assert.AreEqual(0.050f, m_Core.Surfaces[0].Height, Tol);
        }

        [Test]
        public void 同じ箱の面が2枚に分かれた形は1つになる()
        {
            // 左右に分かれた 2 枚。すき間 0.005 m（0.01 以内）
            Settle(Rect(1, 0.10f, -0.0525f, 0f, 0.1f, 0.2f), Rect(2, 0.10f, 0.0525f, 0f, 0.1f, 0.2f));
            Assert.AreEqual(1, m_Core.Surfaces.Count);
            var original = m_Core.OriginalIds(m_Core.Surfaces[0].Id);
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, original);
        }

        [Test]
        public void すき間が大きければまとめない()
        {
            Settle(Rect(1, 0.10f, -0.06f, 0f, 0.1f, 0.2f), Rect(2, 0.10f, 0.06f, 0f, 0.1f, 0.2f));
            Assert.AreEqual(2, m_Core.Surfaces.Count);
        }

        [Test]
        public void 低いノートは出ない()
        {
            // 高さ 0.005 m < 0.008 m
            Settle(Rect(1, 0.005f, 0f, 0f, 0.2f, 0.15f));
            Assert.AreEqual(0, m_Core.Surfaces.Count);
        }

        [Test]
        public void 机の天面そのものは出ない()
        {
            Settle(Rect(1, 0f, 0f, 0f, 1.2f, 0.6f));
            Assert.AreEqual(0, m_Core.Surfaces.Count);
        }

        [Test]
        public void 高すぎる面は出ない()
        {
            Settle(Rect(1, 0.45f, 0f, 0f, 0.2f, 0.2f));
            Assert.AreEqual(0, m_Core.Surfaces.Count);
        }

        [Test]
        public void 小さすぎる面は出ない()
        {
            // 3cm x 3cm = 9cm² < 16cm²
            Settle(Rect(1, 0.05f, 0f, 0f, 0.03f, 0.03f));
            Assert.AreEqual(0, m_Core.Surfaces.Count);
        }

        [Test]
        public void 机の外の面は出ない()
        {
            // 机は X が ±0.6。マージン 0.05 を足しても ±0.65。0.9 の位置の面は外
            Settle(Rect(1, 0.05f, 0.9f, 0f, 0.1f, 0.1f));
            Assert.AreEqual(0, m_Core.Surfaces.Count);
        }

        [Test]
        public void 机のふちにはみ出した面は出る()
        {
            Settle(Rect(1, 0.05f, 0.6f, 0f, 0.1f, 0.1f));
            Assert.AreEqual(1, m_Core.Surfaces.Count);
        }

        [Test]
        public void 各辺は広げる量だけ外へ広がる()
        {
            Settle(Rect(1, 0.05f, 0f, 0f, 0.2f, 0.1f));
            var outline = m_Core.Surfaces[0].Outline;
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var p in outline)
            {
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
                minZ = Mathf.Min(minZ, p.y);
                maxZ = Mathf.Max(maxZ, p.y);
            }

            Assert.AreEqual(0.2f + 2f * m_Settings.edgeExpansion, maxX - minX, Tol);
            Assert.AreEqual(0.1f + 2f * m_Settings.edgeExpansion, maxZ - minZ, Tol);
        }

        [Test]
        public void 見つかってすぐは出さない()
        {
            var raw = new[] { Rect(1, 0.05f, 0f, 0f, 0.2f, 0.2f) };
            m_Core.Update(0f, Desk, raw);
            m_Core.Update(0.5f, Desk, raw); // settleTime 1 秒に満たない
            Assert.AreEqual(0, m_Core.Surfaces.Count);
            m_Core.Update(1.1f, Desk, raw);
            Assert.AreEqual(1, m_Core.Surfaces.Count);
        }

        [Test]
        public void 形が大きく変わり続ける面は出ない()
        {
            // 毎回、幅が 30% ずつ変わる
            for (var i = 0; i < 6; i++)
            {
                m_Core.Update(i * 0.4f, Desk, new[] { Rect(1, 0.05f, 0f, 0f, 0.2f * (i % 2 == 0 ? 1f : 1.5f), 0.2f) });
            }

            Assert.AreEqual(0, m_Core.Surfaces.Count);
        }

        [Test]
        public void 一度出た面は形が変わっても出し続ける()
        {
            Settle(Rect(1, 0.05f, 0f, 0f, 0.2f, 0.2f));
            Assert.AreEqual(1, m_Core.Surfaces.Count);
            m_Now += 1f;
            m_Core.Update(m_Now, Desk, new[] { Rect(1, 0.05f, 0f, 0f, 0.4f, 0.2f) });
            Assert.AreEqual(1, m_Core.Surfaces.Count);
        }

        [Test]
        public void 高さは1ミリ単位に丸められる()
        {
            Settle(Rect(1, 0.0504f, 0f, 0f, 0.2f, 0.2f));
            Assert.AreEqual(0.050f, m_Core.Surfaces[0].Height, 1e-5f);
        }

        [Test]
        public void 高さは1秒かけて新しい値に寄る()
        {
            Settle(Rect(1, 0.050f, 0f, 0f, 0.2f, 0.2f));
            m_Now += 0.25f; // notifyInterval を過ぎさせる
            m_Core.Update(m_Now, Desk, new[] { Rect(1, 0.100f, 0f, 0f, 0.2f, 0.2f) });
            // すぐには 0.100 にならない。0.050 より上、0.100 より下
            var height = m_Core.Surfaces[0].Height;
            Assert.Greater(height, 0.050f);
            Assert.Less(height, 0.100f);
        }

        [Test]
        public void 変わらない面の番号は変わらない()
        {
            var raw = new[] { Rect(1, 0.05f, 0f, 0f, 0.2f, 0.2f) };
            Settle(raw);
            var id = m_Core.Surfaces[0].Id;
            m_Now += 2f;
            m_Core.Update(m_Now, Desk, raw);
            Assert.AreEqual(id, m_Core.Surfaces[0].Id);
        }

        [Test]
        public void まとめた面には新しい番号が付く()
        {
            // 最初は別々の 2 枚。あとから 2 枚の間がつながって 1 つにまとまる
            Settle(Rect(1, 0.10f, -0.06f, 0f, 0.1f, 0.2f), Rect(2, 0.10f, 0.06f, 0f, 0.1f, 0.2f));
            var before = new List<int> { m_Core.Surfaces[0].Id, m_Core.Surfaces[1].Id };
            m_Now += 1f;
            m_Core.Update(m_Now, Desk, new[]
            {
                Rect(1, 0.10f, -0.06f, 0f, 0.1f, 0.2f), Rect(2, 0.10f, 0.06f, 0f, 0.1f, 0.2f),
                Rect(3, 0.10f, 0f, 0f, 0.05f, 0.2f),
            });
            m_Now += 1.2f;
            m_Core.Update(m_Now, Desk, new[]
            {
                Rect(1, 0.10f, -0.06f, 0f, 0.1f, 0.2f), Rect(2, 0.10f, 0.06f, 0f, 0.1f, 0.2f),
                Rect(3, 0.10f, 0f, 0f, 0.05f, 0.2f),
            });
            Assert.AreEqual(1, m_Core.Surfaces.Count);
            Assert.IsFalse(before.Contains(m_Core.Surfaces[0].Id));
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, m_Core.OriginalIds(m_Core.Surfaces[0].Id));
        }

        [Test]
        public void 一覧が変わっても最小間隔より早くは知らせない()
        {
            var raw = new[] { Rect(1, 0.05f, 0f, 0f, 0.2f, 0.2f) };
            m_Core.Update(0f, Desk, raw);
            Assert.IsTrue(m_Core.Update(1.1f, Desk, raw)); // 初めて出る
            // 0.1 秒後に高さが変わる。最小間隔 0.25 秒に満たないので知らせない
            var moved = new[] { Rect(1, 0.20f, 0f, 0f, 0.2f, 0.2f) };
            m_Settings.heightSmoothingTime = 0f;
            Assert.IsFalse(m_Core.Update(1.2f, Desk, moved));
            Assert.IsTrue(m_Core.Update(1.4f, Desk, moved));
        }

        [Test]
        public void 変わっていなければ知らせない()
        {
            var raw = new[] { Rect(1, 0.05f, 0f, 0f, 0.2f, 0.2f) };
            Settle(raw);
            m_Now += 1f;
            Assert.IsFalse(m_Core.Update(m_Now, Desk, raw));
        }

        [Test]
        public void 見えなくなった面は一覧から消える()
        {
            Settle(Rect(1, 0.05f, 0f, 0f, 0.2f, 0.2f));
            m_Now += 1f;
            Assert.IsTrue(m_Core.Update(m_Now, Desk, new RawSurface[0]));
            Assert.AreEqual(0, m_Core.Surfaces.Count);
        }

        [Test]
        public void Clearで一覧が空になる()
        {
            Settle(Rect(1, 0.05f, 0f, 0f, 0.2f, 0.2f));
            Assert.IsTrue(m_Core.Clear());
            Assert.AreEqual(0, m_Core.Surfaces.Count);
            Assert.IsFalse(m_Core.Clear());
        }
    }

    /// <summary>SurfaceGeometry（上から見た多角形の計算）の確認。</summary>
    public class SurfaceGeometryTests
    {
        const float Tol = 1e-4f;

        static Vector2[] Square(float cx, float cz, float size)
        {
            var h = size / 2f;
            return new[]
            {
                new Vector2(cx - h, cz - h), new Vector2(cx + h, cz - h),
                new Vector2(cx + h, cz + h), new Vector2(cx - h, cz + h),
            };
        }

        [Test]
        public void 面積と重心()
        {
            var square = Square(1f, 2f, 2f);
            Assert.AreEqual(4f, SurfaceGeometry.Area(square), Tol);
            Assert.AreEqual(1f, SurfaceGeometry.Centroid(square).x, Tol);
            Assert.AreEqual(2f, SurfaceGeometry.Centroid(square).y, Tol);
        }

        [Test]
        public void すき間と重なり()
        {
            Assert.AreEqual(0.5f, SurfaceGeometry.Distance(Square(0f, 0f, 1f), Square(1.5f, 0f, 1f)), Tol);
            Assert.AreEqual(0f, SurfaceGeometry.Distance(Square(0f, 0f, 1f), Square(0.5f, 0f, 1f)), Tol);
            // 一方がもう一方の中
            Assert.AreEqual(0f, SurfaceGeometry.Distance(Square(0f, 0f, 2f), Square(0f, 0f, 0.5f)), Tol);
            // 斜めに離れた角どうし
            Assert.AreEqual(Mathf.Sqrt(2f) * 0.5f, SurfaceGeometry.Distance(Square(0f, 0f, 1f), Square(1.5f, 1.5f, 1f)), Tol);
        }

        [Test]
        public void 十字に交わる形は重なりとみなす()
        {
            var horizontal = new[] { new Vector2(-2f, -0.1f), new Vector2(2f, -0.1f), new Vector2(2f, 0.1f), new Vector2(-2f, 0.1f) };
            var vertical = new[] { new Vector2(-0.1f, -2f), new Vector2(0.1f, -2f), new Vector2(0.1f, 2f), new Vector2(-0.1f, 2f) };
            Assert.AreEqual(0f, SurfaceGeometry.Distance(horizontal, vertical), Tol);
        }

        [Test]
        public void 凸包は内側の点を含めない()
        {
            var points = new List<Vector2>(Square(0f, 0f, 2f)) { Vector2.zero, new Vector2(0.2f, 0.3f) };
            var hull = SurfaceGeometry.ConvexHull(points);
            Assert.AreEqual(4, hull.Length);
            Assert.AreEqual(4f, SurfaceGeometry.Area(hull), Tol);
        }

        [Test]
        public void 広げると外へ広がり向きに関わらず反時計回りで返る()
        {
            var clockwise = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
            var expanded = SurfaceGeometry.Expand(clockwise, 0.1f);
            Assert.Greater(SurfaceGeometry.SignedArea(expanded), 0f);
            Assert.AreEqual(1.2f * 1.2f, SurfaceGeometry.Area(expanded), Tol);
        }
    }
}
