using System.IO;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Gameplay
{
    /// <summary>
    /// 練習用シーンでゲームの進行（Step 11）を試すための仮のボタン。B と D の画面がなくても、
    /// StageBuilt と D の依頼（Play、SetPaused、Retry、BackToList）を出せるようにする。合流（Step 13）では使わない。
    /// </summary>
    public class GameFlowDebugPanel : MonoBehaviour
    {
        public GameFlow gameFlow;

        [Tooltip("練習用の床の StageRoot")]
        public Transform practiceRoot;

        [Tooltip("メニュー「4. お手本ステージを練習用シーンに置く」で置いたお手本の StageRoot（なければ空）")]
        public Transform sampleRoot;

        public string sampleStageId = "sample-01";

        void OnGUI()
        {
            if (gameFlow == null)
            {
                return;
            }
            GUILayout.BeginArea(new Rect(Screen.width - 236, 16, 220, 420), GUI.skin.box);
            GUILayout.Label($"じょうたい：{gameFlow.State}");
            GUILayout.Label($"タイム：{gameFlow.ElapsedSeconds:0.00} 秒");
            if (GUILayout.Button("ステージかんせい（仮 StageBuilt）", GUILayout.Height(32))) BuildStage();
            if (GUILayout.Button("あそぶ（Play）", GUILayout.Height(32))) UiRequests.RequestPlay();
            if (GUILayout.Button("いちじていし", GUILayout.Height(32))) UiRequests.RequestSetPaused(true);
            if (GUILayout.Button("さいかい", GUILayout.Height(32))) UiRequests.RequestSetPaused(false);
            if (GUILayout.Button("ゴールに ふれた（仮）", GUILayout.Height(32))) gameFlow.ReachGoal();
            if (GUILayout.Button("もういちど（Retry）", GUILayout.Height(32))) UiRequests.RequestRetry();
            if (GUILayout.Button("いちらんへ（BackToList）", GUILayout.Height(32))) UiRequests.RequestBackToList();
            GUILayout.EndArea();
        }

        // B の代わりに、ステージの置き場所とデータを決めて StageBuilt を出す
        void BuildStage()
        {
            if (sampleRoot != null)
            {
                string path = Path.Combine(Application.streamingAssetsPath, "Stages", sampleStageId, "stage.json");
                StageContext.StageRoot = sampleRoot;
                StageContext.CurrentStage = JsonUtility.FromJson<StageData>(File.ReadAllText(path));
            }
            else
            {
                StageContext.StageRoot = practiceRoot;
                StageContext.CurrentStage = PracticeStage();
            }
            StageContext.RaiseStageBuilt();
        }

        // 練習用の床に合わせた仮のステージデータ（スタートは手前、向きは奥）
        static StageData PracticeStage()
        {
            var stage = new StageData { id = "practice", name = "れんしゅう" };
            stage.desk.width = 0.6f;
            stage.desk.depth = 0.6f;
            stage.spawn.position = Vec3.From(new Vector3(0f, 0f, -0.25f));
            stage.goal.position = Vec3.From(new Vector3(0.2f, 0f, -0.2f));
            stage.goal.radius = 0.03f;
            return stage;
        }
    }
}
