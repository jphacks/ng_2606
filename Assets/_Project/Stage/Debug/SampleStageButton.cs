using System.IO;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>
    /// Test_B 用：画面のボタンでお手本ステージを読み込み、組み立てる（B の Step 5）。
    /// PC で試すためだけの部品。本番の読み込みは Step 10 の StageLoader で作る。
    /// </summary>
    public class SampleStageButton : MonoBehaviour
    {
        [SerializeField] StageBuilder builder;
        [SerializeField] string stageId = "sample-01";

        string message = "";

        void OnGUI()
        {
            var scale = Screen.height / 720f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            if (GUI.Button(new Rect(20, 20, 320, 60), $"{stageId} を組み立てる"))
            {
                LoadAndBuild();
            }
            GUI.Label(new Rect(20, 90, 600, 40), message);
        }

        void LoadAndBuild()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "Stages", stageId, "stage.json");
            if (!File.Exists(path))
            {
                message = $"見つかりません：{path}";
                return;
            }

            var stage = JsonUtility.FromJson<StageData>(File.ReadAllText(path));
            StageContext.RaiseStageLoaded(stage);
            builder.Build(stage);
            message = $"箱 {stage.boxes.Length} 個を組み立てました";
        }
    }
}
