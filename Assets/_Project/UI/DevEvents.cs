using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.UI
{
    /// <summary>
    /// 開発用：他の担当の部品がまだないうちに、知らせを自分で出すボタン用（Step 1）。
    /// 合流前（Step 15）に DevMarker と一緒に消す。
    /// </summary>
    public class DevEvents : MonoBehaviour
    {
        [SerializeField] ScreenRouter router;

        public void SetRouter(ScreenRouter value) => router = value;

        public void RaiseDeskDecided() => StageContext.RaiseDeskDecided();

        public void RaiseStageBuilt() => StageContext.RaiseStageBuilt();

        public void RaiseSaveOk() => StageContext.RaiseSaveCompleted(true);

        /// <summary>ゴールした知らせを出して、クリア画面へ移る。</summary>
        public void RaiseGoal()
        {
            GameEvents.RaiseGoal(12.34f, true);
            router.Show(ScreenId.Clear);
        }
    }
}
