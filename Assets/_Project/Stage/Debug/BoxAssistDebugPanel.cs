using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>
    /// Test_B 用：たしかめ画面（D）の補助の代わりの、仮の操作（B の Step 9）。
    /// 右クリックで箱を選ぶ、Shift＋右クリックで箱を足す。ボタンで動かす・大きさ・回す・けす・もどす。
    /// D の画面と同じく、共通コードの依頼だけを出す。
    /// </summary>
    public class BoxAssistDebugPanel : MonoBehaviour
    {
        [SerializeField] BoxAssist assist;

        const float Step = 0.01f;

        void OnGUI()
        {
            // 右クリックはボタンが使わないので、画面のタップの代わりにする（座標は GUI の拡大の前に読む）
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 1)
            {
                var screen = new Vector2(e.mousePosition.x, Screen.height - e.mousePosition.y);
                if (e.shift)
                {
                    UiRequests.RequestAddBox(screen);
                }
                else
                {
                    UiRequests.RequestSelectBox(screen);
                }
                e.Use();
            }

            var scale = Screen.height / 720f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            float x = 20f, y = 340f;
            GUI.Label(new Rect(x, y, 600, 25), "補助：右クリックで選ぶ／Shift＋右クリックで箱を足す");
            var selected = ScanContext.WorkingBoxes.Find(b => b.id == ScanContext.SelectedBoxId);
            GUI.Label(new Rect(x, y + 25, 600, 25), selected == null
                ? "選んでいる箱：なし"
                : $"選んでいる箱：{selected.id}（{selected.source}） 幅 {selected.size[0] * 100:0.0} 高さ {selected.size[1] * 100:0.0} 奥行き {selected.size[2] * 100:0.0} cm、{selected.yawDegrees:0.0} 度");

            y += 55f;
            Row(x, y, "動かす", ("←", () => UiRequests.RequestMoveBox(new Vector2(-Step, 0f))), ("→", () => UiRequests.RequestMoveBox(new Vector2(Step, 0f))),
                ("奥", () => UiRequests.RequestMoveBox(new Vector2(0f, Step))), ("手前", () => UiRequests.RequestMoveBox(new Vector2(0f, -Step))));
            y += 32f;
            Row(x, y, "大きさ", ("幅＋", () => UiRequests.RequestResizeBox(new Vector3(Step, 0f, 0f))), ("幅－", () => UiRequests.RequestResizeBox(new Vector3(-Step, 0f, 0f))),
                ("高さ＋", () => UiRequests.RequestResizeBox(new Vector3(0f, Step, 0f))), ("高さ－", () => UiRequests.RequestResizeBox(new Vector3(0f, -Step, 0f))));
            y += 32f;
            Row(x, y, "", ("奥行き＋", () => UiRequests.RequestResizeBox(new Vector3(0f, 0f, Step))), ("奥行き－", () => UiRequests.RequestResizeBox(new Vector3(0f, 0f, -Step))));
            y += 32f;
            Row(x, y, "回す", ("左 7°", () => UiRequests.RequestRotateBox(-7f)), ("右 7°", () => UiRequests.RequestRotateBox(7f)));
            y += 32f;
            Row(x, y, "", ("けす", UiRequests.RequestDeleteBox), ($"もどす（{(assist != null ? assist.UndoCount : 0)}）", UiRequests.RequestUndoBox));
        }

        static void Row(float x, float y, string label, params (string text, System.Action action)[] buttons)
        {
            GUI.Label(new Rect(x, y, 60, 28), label);
            var bx = x + 60f;
            foreach (var (text, action) in buttons)
            {
                if (GUI.Button(new Rect(bx, y, 85, 28), text))
                {
                    action();
                }
                bx += 90f;
            }
        }
    }
}
