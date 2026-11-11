using DeskStage.Merge;
using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Merge.Tests
{
    /// <summary>立体のキャラの手足の角度（LimbPose）と、写真の貼り方（CharacterBody3D）。</summary>
    public class LimbPoseTests
    {
        [Test]
        public void 歩くと右腕と左足が同じ向きに振れる()
        {
            var pose = LimbPose.Evaluate(LimbPose.State.Ground, Mathf.PI / 2f, 1f, 0f);
            Assert.AreEqual(LimbPose.ArmSwing, pose.LeftArmSwing, 1e-4f);
            Assert.AreEqual(-LimbPose.ArmSwing, pose.RightArmSwing, 1e-4f);
            Assert.Greater(pose.RightLegSwing, 0f);
            Assert.Less(pose.LeftLegSwing, 0f);
        }

        [Test]
        public void 止まっていると手足は振らない()
        {
            var pose = LimbPose.Evaluate(LimbPose.State.Ground, 1.3f, 0f, 5f);
            Assert.AreEqual(0f, pose.LeftArmSwing, 1e-4f);
            Assert.AreEqual(0f, pose.RightLegSwing, 1e-4f);
            Assert.AreEqual(0f, pose.ArmRaise, 1e-4f);
        }

        [Test]
        public void 跳んでいると腕を上げる()
        {
            Assert.AreEqual(LimbPose.State.Rising, LimbPose.StateOf(false, 0.5f));
            Assert.AreEqual(LimbPose.JumpRaise, LimbPose.Evaluate(LimbPose.State.Rising, 0f, 1f, 0f).ArmRaise, 1e-4f);
        }

        [Test]
        public void 速く下へ動いていると落ちているとみなす()
        {
            Assert.AreEqual(LimbPose.State.Falling, LimbPose.StateOf(false, -1f));
            Assert.AreEqual(LimbPose.State.Rising, LimbPose.StateOf(false, -0.1f));
            Assert.AreEqual(LimbPose.State.Ground, LimbPose.StateOf(true, -1f));
        }

        [Test]
        public void 歩幅は足の長さと振る角度で決まる()
        {
            // 足の長さ 1cm、45 度：1 往復で 4 × 0.01 × sin45 ≒ 2.8cm 進む
            Assert.AreEqual(4f * 0.01f * Mathf.Sin(LimbPose.LegSwing * Mathf.Deg2Rad), LimbPose.CycleLength(0.01f), 1e-6f);
            Assert.Greater(LimbPose.CycleLength(0.02f), LimbPose.CycleLength(0.01f));
        }

        [Test]
        public void 前から見て写真の左が左に写る()
        {
            // 前（+z）から見ると、左は +x。写真の左（u が小さい）が、+x 側に来る
            var head = HumanMask.Face;
            var left = CharacterBody3D.PhotoUv(head, new Vector3(1f, 0f, 0f));
            var right = CharacterBody3D.PhotoUv(head, new Vector3(-1f, 0f, 0f));
            Assert.Less(left.x, head.x);
            Assert.Greater(right.x, head.x);
        }
    }
}
