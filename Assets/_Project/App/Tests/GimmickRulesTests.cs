using DeskStage.Shared;
using NUnit.Framework;
using UnityEngine;

namespace DeskStage.App.Tests
{
    public class GimmickRulesTests
    {
        [Test]
        public void 箱の上面の真上にいれば乗っている()
        {
            // 高さ 10cm の箱：上面は local.y = 0.5。足元が上面と同じ高さ
            Assert.IsTrue(GimmickRules.IsOnTop(new Vector3(0f, 0.5f, 0f), 0.10f));
            Assert.IsTrue(GimmickRules.IsOnTop(new Vector3(0.4f, 0.5f + 0.001f / 0.10f, -0.4f), 0.10f));
        }

        [Test]
        public void 箱のはしの外にいれば乗っていない()
        {
            Assert.IsFalse(GimmickRules.IsOnTop(new Vector3(0.6f, 0.5f, 0f), 0.10f));
            Assert.IsFalse(GimmickRules.IsOnTop(new Vector3(0f, 0.5f, -0.51f), 0.10f));
        }

        [Test]
        public void 高く跳んでいるときは乗っていない()
        {
            // 上面から 12cm 上（ジャンプの頂点）
            Assert.IsFalse(GimmickRules.IsOnTop(new Vector3(0f, 0.5f + 0.12f / 0.10f, 0f), 0.10f));
        }

        [Test]
        public void 箱の横を通る低い位置は乗っていない()
        {
            // 上面から 5cm 下（箱の横で、机の上を歩いている）
            Assert.IsFalse(GimmickRules.IsOnTop(new Vector3(0f, 0.5f - 0.05f / 0.10f, 0f), 0.10f));
        }

        [Test]
        public void 水の向きは同じidなら同じ()
        {
            Assert.AreEqual(GimmickRules.WaterDirection("box-03"), GimmickRules.WaterDirection("box-03"));
        }

        [Test]
        public void 水の向きは前後左右のどれか1つ()
        {
            for (var i = 0; i < 30; i++)
            {
                var d = GimmickRules.WaterDirection($"box-{i:00}");
                Assert.AreEqual(1f, d.magnitude, 1e-5f);
                Assert.IsTrue(Mathf.Approximately(d.x, 0f) || Mathf.Approximately(d.y, 0f));
            }
        }

        [Test]
        public void 水の向きは箱によってちがう()
        {
            var seen = new System.Collections.Generic.HashSet<Vector2>();
            for (var i = 0; i < 30; i++) seen.Add(GimmickRules.WaterDirection($"box-{i:00}"));
            Assert.Greater(seen.Count, 1);
        }

        [Test]
        public void しかけの名前が引ける()
        {
            Assert.IsNotEmpty(GimmickRules.Name(BoxData.GimmickWater));
            Assert.IsNotEmpty(GimmickRules.Name(BoxData.GimmickFire));
            Assert.IsNotEmpty(GimmickRules.Name(BoxData.GimmickHeal));
            Assert.AreEqual("", GimmickRules.Name(""));
        }
    }

    public class PlayerHealthTests
    {
        [Test]
        public void はじめはハート3つ()
        {
            var h = new PlayerHealth();
            Assert.AreEqual(3, h.Hearts);
            Assert.IsFalse(h.IsDead);
        }

        [Test]
        public void ダメージで1つ減りしばらくは続けて減らない()
        {
            var h = new PlayerHealth();
            Assert.IsTrue(h.TryDamage(10f));
            Assert.AreEqual(2, h.Hearts);
            Assert.IsFalse(h.TryDamage(10.5f));
            Assert.AreEqual(2, h.Hearts);
            Assert.IsTrue(h.IsInvulnerable(11f));
            Assert.IsTrue(h.TryDamage(11.6f));
            Assert.AreEqual(1, h.Hearts);
        }

        [Test]
        public void ハートが0になるとゲームオーバー()
        {
            var h = new PlayerHealth();
            h.TryDamage(0f);
            h.TryDamage(2f);
            h.TryDamage(4f);
            Assert.AreEqual(0, h.Hearts);
            Assert.IsTrue(h.IsDead);
            Assert.IsFalse(h.TryDamage(6f));
            Assert.AreEqual(0, h.Hearts);
        }

        [Test]
        public void 回復は最大までで超えない()
        {
            var h = new PlayerHealth();
            Assert.IsFalse(h.Heal());
            h.TryDamage(0f);
            Assert.IsTrue(h.Heal());
            Assert.AreEqual(3, h.Hearts);
            Assert.IsFalse(h.Heal());
        }

        [Test]
        public void ゲームオーバーのあとは回復しない()
        {
            var h = new PlayerHealth();
            h.TryDamage(0f);
            h.TryDamage(2f);
            h.TryDamage(4f);
            Assert.IsFalse(h.Heal());
            Assert.AreEqual(0, h.Hearts);
        }

        [Test]
        public void リセットでもとに戻る()
        {
            var h = new PlayerHealth();
            h.TryDamage(0f);
            h.Reset();
            Assert.AreEqual(3, h.Hearts);
            Assert.IsTrue(h.TryDamage(0.1f)); // まもられていた時間も、リセットされる
        }

        // ---- 2026-11-06 に足したしかけ（ジャンプ台・敵・ワープ） ----

        [Test]
        public void 新しいしかけの名前()
        {
            Assert.AreEqual(AppText.GimmickJumpPad, GimmickRules.Name(BoxData.GimmickJumpPad));
            Assert.AreEqual(AppText.GimmickEnemy, GimmickRules.Name(BoxData.GimmickEnemy));
            Assert.AreEqual(AppText.GimmickWarp, GimmickRules.Name(BoxData.GimmickWarp));
        }

        [Test]
        public void 敵の動き方はくじで追いかけるか歩き回るかに分かれる()
        {
            Assert.AreEqual(GimmickRules.EnemyMode.Chase, GimmickRules.ChooseEnemyMode(0.2f));
            Assert.AreEqual(GimmickRules.EnemyMode.Wander, GimmickRules.ChooseEnemyMode(0.7f));
        }

        [Test]
        public void 出ている敵が全部同じ動き方なら次はもう一方になる()
        {
            // 追いかける敵だけなら、次は歩き回る敵（くじの値に関係なく）
            Assert.AreEqual(GimmickRules.EnemyMode.Wander, GimmickRules.ChooseEnemyMode(0.1f, 2, 0));
            // 歩き回る敵だけなら、次は追いかける敵
            Assert.AreEqual(GimmickRules.EnemyMode.Chase, GimmickRules.ChooseEnemyMode(0.9f, 0, 3));
        }

        [Test]
        public void 両方いるか1体もいなければくじで決める()
        {
            Assert.AreEqual(GimmickRules.EnemyMode.Chase, GimmickRules.ChooseEnemyMode(0.2f, 0, 0));
            Assert.AreEqual(GimmickRules.EnemyMode.Wander, GimmickRules.ChooseEnemyMode(0.8f, 0, 0));
            Assert.AreEqual(GimmickRules.EnemyMode.Chase, GimmickRules.ChooseEnemyMode(0.2f, 1, 1));
            Assert.AreEqual(GimmickRules.EnemyMode.Wander, GimmickRules.ChooseEnemyMode(0.8f, 2, 1));
        }

        [Test]
        public void 敵どうしは体2つ分より近いと重なる()
        {
            Assert.IsTrue(GimmickRules.EnemiesOverlap(Vector3.zero, new Vector3(0.015f, 0f, 0f)));
            Assert.IsFalse(GimmickRules.EnemiesOverlap(Vector3.zero, new Vector3(0.025f, 0f, 0f)));
            // 高さがちがえば（箱の上と机の上）、重ならない
            Assert.IsFalse(GimmickRules.EnemiesOverlap(Vector3.zero, new Vector3(0.005f, 0.05f, 0f)));
        }

        [Test]
        public void 敵がキャラに重なると当たる()
        {
            var feet = new Vector3(0f, 0.05f, 0f);
            Assert.IsTrue(GimmickRules.EnemyTouches(new Vector3(0.015f, 0.05f, 0f), feet, 0.008f, 0.03f));
            Assert.IsFalse(GimmickRules.EnemyTouches(new Vector3(0.03f, 0.05f, 0f), feet, 0.008f, 0.03f));
        }

        [Test]
        public void 高さがちがうと敵に当たらない()
        {
            // 敵は机の上、キャラは 10cm の箱の上
            var feet = new Vector3(0f, 0.10f, 0f);
            Assert.IsFalse(GimmickRules.EnemyTouches(new Vector3(0f, 0f, 0f), feet, 0.008f, 0.03f));
        }

        [Test]
        public void 追いかける敵はキャラの方へ進む()
        {
            var dir = GimmickRules.ChaseDirection(Vector3.zero, new Vector3(0.1f, 0.05f, 0f));
            Assert.AreEqual(1f, dir.x, 1e-5f);
            Assert.AreEqual(0f, dir.y);
            Assert.AreEqual(Vector3.zero, GimmickRules.ChaseDirection(Vector3.zero, Vector3.zero));
        }

        [Test]
        public void 歩き回る向きは水平で長さ1()
        {
            var dir = GimmickRules.WanderDirection(0.3f);
            Assert.AreEqual(0f, dir.y);
            Assert.AreEqual(1f, dir.magnitude, 1e-5f);
        }

        [Test]
        public void ワープ先の候補は机のふちより内側()
        {
            var a = GimmickRules.WarpCandidate(0f, 0f, 0.6f, 0.4f, 0.03f);
            var b = GimmickRules.WarpCandidate(1f, 1f, 0.6f, 0.4f, 0.03f);
            Assert.AreEqual(-0.27f, a.x, 1e-5f);
            Assert.AreEqual(-0.17f, a.y, 1e-5f);
            Assert.AreEqual(0.27f, b.x, 1e-5f);
            Assert.AreEqual(0.17f, b.y, 1e-5f);
        }

        [Test]
        public void 決まりの数値()
        {
            Assert.AreEqual(7f, GimmickRules.EnemySpawnSeconds);
            Assert.AreEqual(5, GimmickRules.MaxEnemies);
            Assert.AreEqual(3f, GimmickRules.WarpCooldownSeconds);
            Assert.AreEqual(2f, GimmickRules.JumpPadHeightScale);
        }
    }
}
