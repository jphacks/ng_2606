using DeskStage.Shared;
using NUnit.Framework;
using UnityEngine;

namespace DeskStage.Gameplay.Tests
{
    /// <summary>しばりの決まり（Docs/specs/course-rules の REQ-3・4・6・9・11）。</summary>
    public class CourseRuleStateTests
    {
        static CourseRuleState With(CourseRules rules)
        {
            var state = new CourseRuleState();
            state.Reset(rules);
            return state;
        }

        [Test]
        public void しばり無しなら_いつでも跳べて_時間切れにならない()
        {
            var state = With(new CourseRules());
            for (var i = 0; i < 50; i++)
            {
                Assert.IsTrue(state.CanJump(i % 2 == 1));
                state.OnJumped();
            }
            Assert.AreEqual(-1, state.JumpsLeft);
            Assert.IsFalse(state.IsTimeUp(9999f));
            Assert.AreEqual(-1f, state.SecondsLeft(10f));
            Assert.IsFalse(CourseRuleState.HasAny(new CourseRules()));
        }

        [Test]
        public void ジャンプきんしなら_地面からも空中からも跳べない()
        {
            var state = With(new CourseRules { maxJumps = 0 });
            Assert.IsFalse(state.CanJump(false));
            Assert.IsFalse(state.CanJump(true));
            Assert.AreEqual(0, state.JumpsLeft);
        }

        [Test]
        public void N回までなら_N回跳んだあとは跳べない_2段ジャンプも1回と数える()
        {
            var state = With(new CourseRules { maxJumps = 3 });
            Assert.IsTrue(state.CanJump(false)); state.OnJumped();
            Assert.IsTrue(state.CanJump(true)); state.OnJumped(); // 2 段ジャンプも 1 回
            Assert.AreEqual(1, state.JumpsLeft);
            Assert.IsTrue(state.CanJump(false)); state.OnJumped();
            Assert.AreEqual(0, state.JumpsLeft);
            Assert.IsFalse(state.CanJump(false));
            Assert.IsFalse(state.CanJump(true));
        }

        [Test]
        public void 二段ジャンプなしなら_地面からは跳べて_空中では跳べない()
        {
            var state = With(new CourseRules { noDoubleJump = true });
            Assert.IsTrue(state.CanJump(false));
            Assert.IsFalse(state.CanJump(true));
        }

        [Test]
        public void 制限時間に達した瞬間に時間切れ()
        {
            var state = With(new CourseRules { timeLimitSeconds = 30f });
            Assert.IsFalse(state.IsTimeUp(29.99f));
            Assert.IsTrue(state.IsTimeUp(30f));
            Assert.AreEqual(10f, state.SecondsLeft(20f), 1e-4f);
            Assert.AreEqual(0f, state.SecondsLeft(45f), 1e-4f);
        }

        [Test]
        public void やり直すと_使ったジャンプの数が0に戻る()
        {
            var rules = new CourseRules { maxJumps = 1 };
            var state = With(rules);
            state.OnJumped();
            Assert.IsFalse(state.CanJump(false));
            state.Reset(rules);
            Assert.IsTrue(state.CanJump(false));
        }

        [Test]
        public void しばりは保存して読み直しても残り_古い保存データはしばり無し()
        {
            var stage = new StageData { rules = new CourseRules { maxJumps = 5, noDoubleJump = true, oneHit = true, timeLimitSeconds = 60f } };
            var read = JsonUtility.FromJson<StageData>(JsonUtility.ToJson(stage)).rules;
            Assert.AreEqual(5, read.maxJumps);
            Assert.IsTrue(read.noDoubleJump);
            Assert.IsTrue(read.oneHit);
            Assert.AreEqual(60f, read.timeLimitSeconds);

            var old = JsonUtility.FromJson<StageData>("{\"version\":2,\"id\":\"old\"}");
            Assert.IsNotNull(old.rules);
            Assert.IsFalse(CourseRuleState.HasAny(old.rules));
        }
    }
}
