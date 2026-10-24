using NUnit.Framework;

namespace DeskStage.Shared.Tests
{
    /// <summary>ステージ一覧と、消す依頼（2026-10-24 に足した）。</summary>
    public class StageListEventsTests
    {
        [TearDown]
        public void Clear()
        {
            StageContext.SetStageList(new StageSummary[0]);
        }

        [Test]
        public void 一覧を書き換えると知らせが出て中身が入る()
        {
            var count = 0;
            void OnChanged() => count++;
            StageContext.StageListChanged += OnChanged;
            try
            {
                StageContext.SetStageList(new[]
                {
                    new StageSummary { id = "stage-2", name = "あたらしい", loadable = true },
                    new StageSummary { id = "stage-1", name = "ふるい", loadable = false, reason = "古い形式" },
                });
            }
            finally
            {
                StageContext.StageListChanged -= OnChanged;
            }

            Assert.That(count, Is.EqualTo(1));
            Assert.That(StageContext.StageList.Count, Is.EqualTo(2));
            Assert.That(StageContext.StageList[0].id, Is.EqualTo("stage-2"));
            Assert.That(StageContext.StageList[1].reason, Is.EqualTo("古い形式"));
        }

        [Test]
        public void 書き換えると前の一覧は残らない()
        {
            StageContext.SetStageList(new[] { new StageSummary { id = "a" }, new StageSummary { id = "b" } });
            StageContext.SetStageList(new[] { new StageSummary { id = "c" } });

            Assert.That(StageContext.StageList.Count, Is.EqualTo(1));
            Assert.That(StageContext.StageList[0].id, Is.EqualTo("c"));
        }

        [Test]
        public void 消す依頼にステージIDが付いて届く()
        {
            string received = null;
            void OnDelete(string id) => received = id;
            UiRequests.DeleteStage += OnDelete;
            try
            {
                UiRequests.RequestDeleteStage("stage-20261024-180000");
            }
            finally
            {
                UiRequests.DeleteStage -= OnDelete;
            }

            Assert.That(received, Is.EqualTo("stage-20261024-180000"));
        }
    }
}
