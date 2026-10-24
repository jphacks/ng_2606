using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>
    /// Test_B 用：ステージ一覧の画面（D）の代わりの仮の一覧とボタン（B の Step 10・11）。
    /// D の画面と同じく、共通コードの一覧と依頼だけを使う。
    /// </summary>
    public class StageLibraryDebugPanel : MonoBehaviour
    {
        string message = "";
        string displayName = "ためし";

        void OnEnable()
        {
            StageContext.SaveCompleted += OnSaveCompleted;
            StageContext.StageLoaded += OnStageLoaded;
        }

        void OnDisable()
        {
            StageContext.SaveCompleted -= OnSaveCompleted;
            StageContext.StageLoaded -= OnStageLoaded;
        }

        void OnSaveCompleted(bool ok, string reason)
        {
            message = ok ? "保存しました" : $"保存できませんでした：{reason}";
        }

        void OnStageLoaded(StageData stage)
        {
            message = $"{stage.name} を読み込みました（箱 {stage.boxes.Length} 個）";
        }

        /// <summary>机の左手前にスタート、右奥にゴールを置く（机の天面の上、高さ0）。</summary>
        void PlaceTemporaryCourse()
        {
            var stage = StageContext.CurrentStage;
            if (stage == null || stage.desk == null || stage.desk.width <= 0f)
            {
                message = "先に、ステージを読み込むか、答えから箱を作ってください";
                return;
            }
            var hw = stage.desk.width / 2f - 0.05f;
            var hd = stage.desk.depth / 2f - 0.05f;
            stage.spawn = new SpawnData { position = new[] { -hw, 0f, -hd }, yawDegrees = 0f };
            stage.goal = new GoalData { position = new[] { hw, 0f, hd }, radius = 0.03f };
            stage.checkpoints = new GoalData[0];
            message = "仮のスタート（左手前）とゴール（右奥）を置きました";
        }

        void OnGUI()
        {
            var scale = Screen.height / 720f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            const float x = 380f;
            GUI.Label(new Rect(x, 20, 400, 30), "表示名");
            displayName = GUI.TextField(new Rect(x + 60, 20, 200, 30), displayName);
            if (GUI.Button(new Rect(x + 270, 20, 160, 30), "今のステージを保存"))
            {
                UiRequests.RequestSaveStage(displayName);
            }
            // 本当のスタートとゴールは C のコースの配置で置く。ここは保存を試すための仮
            if (GUI.Button(new Rect(x + 440, 20, 220, 30), "仮のスタートとゴールを置く"))
            {
                PlaceTemporaryCourse();
            }
            GUI.Label(new Rect(x, 55, 700, 30), message);

            GUI.Label(new Rect(x, 90, 400, 30), $"ステージ一覧（{StageContext.StageList.Count} 個、新しい順）");
            var y = 120f;
            foreach (var s in StageContext.StageList)
            {
                GUI.enabled = s.loadable;
                if (GUI.Button(new Rect(x, y, 300, 30), s.loadable ? $"{s.name}（{s.createdAt}）" : $"{s.name}：読み込めません（{s.reason}）"))
                {
                    UiRequests.RequestSelectStage(s.id);
                }
                GUI.enabled = true;
                if (GUI.Button(new Rect(x + 310, y, 60, 30), "けす"))
                {
                    UiRequests.RequestDeleteStage(s.id);
                }
                y += 35f;
            }
        }
    }
}
