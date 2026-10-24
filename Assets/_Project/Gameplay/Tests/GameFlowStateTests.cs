using NUnit.Framework;

namespace DeskStage.Gameplay.Tests
{
    /// <summary>
    /// ゲームの状態の切り替え（C_gameplay.md の Step 11 の表）を確かめる。
    /// </summary>
    public class GameFlowStateTests
    {
        static GameFlowState Playing()
        {
            var flow = new GameFlowState();
            flow.OnStageBuilt();
            flow.TryPlay();
            flow.TryStart();
            return flow;
        }

        [Test]
        public void 始めはステージ待ち()
        {
            Assert.AreEqual(GameState.WaitingStage, new GameFlowState().State);
        }

        [Test]
        public void StageBuiltの前はPlayできない()
        {
            var flow = new GameFlowState();
            Assert.IsFalse(flow.TryPlay());
            Assert.AreEqual(GameState.WaitingStage, flow.State);
        }

        [Test]
        public void StageBuiltの後のPlayで準備からプレイ中になる()
        {
            var flow = new GameFlowState();
            flow.OnStageBuilt();
            Assert.IsTrue(flow.TryPlay());
            Assert.AreEqual(GameState.Preparing, flow.State);
            Assert.IsTrue(flow.TryStart());
            Assert.AreEqual(GameState.Playing, flow.State);
        }

        [Test]
        public void 一時停止と再開()
        {
            var flow = Playing();
            Assert.IsTrue(flow.TrySetPaused(true));
            Assert.AreEqual(GameState.Paused, flow.State);
            Assert.IsTrue(flow.TrySetPaused(false));
            Assert.AreEqual(GameState.Playing, flow.State);
        }

        [Test]
        public void プレイ中に再開や一時停止中に一時停止はできない()
        {
            var flow = Playing();
            Assert.IsFalse(flow.TrySetPaused(false));
            flow.TrySetPaused(true);
            Assert.IsFalse(flow.TrySetPaused(true));
            Assert.AreEqual(GameState.Paused, flow.State);
        }

        [Test]
        public void ゴールに触れるとクリア()
        {
            var flow = Playing();
            Assert.IsTrue(flow.TryGoal());
            Assert.AreEqual(GameState.Cleared, flow.State);
        }

        [Test]
        public void 一時停止中はゴールにならない()
        {
            var flow = Playing();
            flow.TrySetPaused(true);
            Assert.IsFalse(flow.TryGoal());
            Assert.AreEqual(GameState.Paused, flow.State);
        }

        [Test]
        public void クリアからRetryで準備に戻る()
        {
            var flow = Playing();
            flow.TryGoal();
            Assert.IsTrue(flow.TryRetry());
            Assert.AreEqual(GameState.Preparing, flow.State);
        }

        [Test]
        public void プレイ中はRetryできない()
        {
            var flow = Playing();
            Assert.IsFalse(flow.TryRetry());
            Assert.AreEqual(GameState.Playing, flow.State);
        }

        [Test]
        public void どの状態でもBackToListでステージ待ちに戻り次のStageBuiltを待つ()
        {
            var flow = Playing();
            flow.TrySetPaused(true);
            flow.BackToList();
            Assert.AreEqual(GameState.WaitingStage, flow.State);
            Assert.IsFalse(flow.TryPlay());
            flow.OnStageBuilt();
            Assert.IsTrue(flow.TryPlay());
        }
    }
}
