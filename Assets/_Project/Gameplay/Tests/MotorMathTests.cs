using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Gameplay.Tests
{
    /// <summary>
    /// CharacterMotor の計算（C_gameplay.md の Step 4）を確かめる。
    /// </summary>
    public class MotorMathTests
    {
        [Test]
        public void 水平の速さはスティックの倒した量に比例する()
        {
            var v = MotorMath.HorizontalVelocity(new Vector3(0f, 0f, 0.5f), 0.15f);
            Assert.AreEqual(0.075f, v.z, 1e-6f);
            Assert.AreEqual(0f, v.y);
        }

        [Test]
        public void 斜めに倒しても歩く速さを超えない()
        {
            var v = MotorMath.HorizontalVelocity(new Vector3(1f, 0f, 1f), 0.15f);
            Assert.AreEqual(0.15f, v.magnitude, 1e-6f);
        }

        [Test]
        public void 接地中の下向きの速さは小さな値に戻る()
        {
            Assert.AreEqual(-MotorMath.GroundStickSpeed, MotorMath.StickToGround(-0.8f, true));
        }

        [Test]
        public void 空中や上向きのときは戻さない()
        {
            Assert.AreEqual(-0.8f, MotorMath.StickToGround(-0.8f, false));
            Assert.AreEqual(0.99f, MotorMath.StickToGround(0.99f, true));
        }

        [Test]
        public void 落下の速さは上限まで()
        {
            Assert.AreEqual(-1f, MotorMath.ApplyGravity(-0.95f, 9.8f, 1f, 0.1f), 1e-6f);
            Assert.AreEqual(0.99f - 0.098f, MotorMath.ApplyGravity(0.99f, 9.8f, 1f, 0.01f), 1e-6f);
        }

        [Test]
        public void 接地中に押すと跳べる()
        {
            var timer = new JumpTimer();
            timer.RecordGrounded(1f);
            timer.RecordJumpPressed(1f);
            Assert.IsTrue(timer.TryConsume(1f, 0.1f, 0.1f));
        }

        [Test]
        public void 一度跳んだら同じ入力でもう一度は跳ばない()
        {
            var timer = new JumpTimer();
            timer.RecordGrounded(1f);
            timer.RecordJumpPressed(1f);
            timer.TryConsume(1f, 0.1f, 0.1f);
            Assert.IsFalse(timer.TryConsume(1.02f, 0.1f, 0.1f));
        }

        [Test]
        public void 着地の0_1秒前に押しても跳べる()
        {
            var timer = new JumpTimer();
            timer.RecordJumpPressed(1f);
            Assert.IsFalse(timer.TryConsume(1f, 0.1f, 0.1f));
            timer.RecordGrounded(1.08f);
            Assert.IsTrue(timer.TryConsume(1.08f, 0.1f, 0.1f));
        }

        [Test]
        public void 落ちた直後0_1秒以内なら跳べる()
        {
            var timer = new JumpTimer();
            timer.RecordGrounded(1f);
            timer.RecordJumpPressed(1.08f);
            Assert.IsTrue(timer.TryConsume(1.08f, 0.1f, 0.1f));
        }

        [Test]
        public void Resetの後は前の入力で跳ばない()
        {
            var timer = new JumpTimer();
            timer.RecordGrounded(1f);
            timer.RecordJumpPressed(1f);
            timer.Reset();
            timer.RecordGrounded(1.02f);
            Assert.IsFalse(timer.TryConsume(1.02f, 0.1f, 0.1f));
        }

        [Test]
        public void 落ちて0_1秒を過ぎると跳べない()
        {
            var timer = new JumpTimer();
            timer.RecordGrounded(1f);
            timer.RecordJumpPressed(1.2f);
            Assert.IsFalse(timer.TryConsume(1.2f, 0.1f, 0.1f));
        }
    }
}
