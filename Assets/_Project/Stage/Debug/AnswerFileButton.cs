using System.IO;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>
    /// Test_B 用：保存した Claude の答え（Stage/Tests/Data/）を A から届いたことにして、箱を作り、組み立てる（B の Step 7）。
    /// サーバーとカメラがなくても、PC で答えから箱ができることを見るためだけの部品。
    /// </summary>
    public class AnswerFileButton : MonoBehaviour
    {
        [SerializeField] StageBuilder builder;
        [SerializeField] string answerFile = "answer-photo-01.json";

        string message = "";

        void OnGUI()
        {
            var scale = Screen.height / 720f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            if (GUI.Button(new Rect(20, 140, 320, 60), $"{answerFile} から作る"))
            {
                LoadAndBuild();
            }
            if (GUI.Button(new Rect(20, 210, 320, 60), "もういちど撮る（Rescan）"))
            {
                UiRequests.RequestRescan();
                builder.Clear();
                message = $"箱を消しました（残り {ScanContext.WorkingBoxes.Count} 個）";
            }
            GUI.Label(new Rect(20, 280, 700, 40), message);
        }

        void LoadAndBuild()
        {
            var path = Path.Combine(Application.dataPath, "_Project", "Stage", "Tests", "Data", answerFile);
            if (!File.Exists(path))
            {
                message = $"見つかりません：{path}";
                return;
            }

            ScanContext.RaiseScanResultReceived(File.ReadAllText(path));
            if (ScanContext.WorkingBoxes.Count == 0 || StageContext.CurrentStage == null)
            {
                message = "箱を作れませんでした（Console を見てください）";
                return;
            }
            builder.Build(StageContext.CurrentStage);
            message = $"答えから箱を {ScanContext.WorkingBoxes.Count} 個作りました";
        }
    }
}
