using System;
using DeskStage.Shared;

namespace DeskStage.Stage
{
    /// <summary>ステージに置いたスタート・ゴール・チェックポイント（コースの物）の扱い。</summary>
    public static class StageCourse
    {
        /// <summary>
        /// コースの物をすべて消す（B の Step 8）。
        /// 並べ直すと、置いた物の下の物がなくなっているかもしれないため。
        /// </summary>
        public static void Clear(StageData stage)
        {
            if (stage == null)
            {
                return;
            }
            stage.spawn = new SpawnData();
            stage.goal = new GoalData();
            stage.checkpoints = Array.Empty<GoalData>();
        }
    }
}
