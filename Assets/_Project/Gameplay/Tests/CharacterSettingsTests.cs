using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Gameplay.Tests
{
    /// <summary>
    /// CharacterSettings の初期値が手順書（C_gameplay.md の Step 2）と合っていることを確かめる。
    /// </summary>
    public class CharacterSettingsTests
    {
        CharacterSettings settings;

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<CharacterSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void SkinWidthは初期値0_08のままではない()
        {
            // 0.08 のままだとキャラが完全に浮く
            Assert.AreEqual(0.0008f, settings.skinWidth);
        }

        [Test]
        public void ジャンプの初速は約0_89()
        {
            Assert.AreEqual(0.885f, settings.JumpVelocity, 0.005f);
        }

        [Test]
        public void 跳べる段の上限はジャンプの高さに0_5cm足した値()
        {
            Assert.AreEqual(settings.jumpHeight + 0.005f, settings.jumpableStepMax, 0.00001f);
        }

        [Test]
        public void 歩ける段の上限はStepOffsetと同じ()
        {
            Assert.AreEqual(settings.stepOffset, settings.walkableStepMax);
        }

        [Test]
        public void StepRulesに境目が渡る()
        {
            settings.walkableStepMax = 0.01f;
            settings.jumpableStepMax = 0.03f;
            var rules = settings.CreateStepRules();
            Assert.AreEqual(0.01f, rules.WalkMax);
            Assert.AreEqual(0.03f, rules.JumpMax);
        }

        [Test]
        public void カメラの初期値は範囲の中()
        {
            Assert.That(settings.cameraDistance, Is.InRange(settings.cameraDistanceMin, settings.cameraDistanceMax));
            Assert.That(settings.cameraPitch, Is.InRange(settings.cameraPitchMin, settings.cameraPitchMax));
        }
    }
}
