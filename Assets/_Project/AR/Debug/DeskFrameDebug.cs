using DeskStage.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeskStage.AR
{
    /// <summary>
    /// Step 8 の確認用。画面のタップを SetDesk の依頼にし、机が決まったら
    /// StageRoot の上に一辺5cmの立方体を置く。D の UI ができたら使わない（検証専用の使い捨て）。
    /// </summary>
    public class DeskFrameDebug : MonoBehaviour
    {
        // 下の仮ボタン（ScanSessionDebugPanel）の上のタップは、机のタップとして扱わない。
        // ボタンは画面の下から 114pt（ラベルを含む）までに出る。画面の向きで割合が変わるので、pt で持つ
        const float ButtonAreaPoints = 120f;

        GameObject m_Cube;

        void OnEnable() => StageContext.DeskDecided += OnDeskDecided;

        void OnDisable() => StageContext.DeskDecided -= OnDeskDecided;

        void Update()
        {
            if (StageContext.ScanStatus != ScanStatus.FindingDesk)
            {
                return;
            }

            var touch = Touchscreen.current;
            if (touch == null || !touch.primaryTouch.press.wasPressedThisFrame)
            {
                return;
            }

            var position = touch.primaryTouch.position.ReadValue();
            var scale = Screen.dpi > 0 ? Screen.dpi / 160f : 2f;
            if (position.y < ButtonAreaPoints * scale)
            {
                return;
            }

            UiRequests.RequestSetDesk(position);
        }

        void OnDeskDecided()
        {
            if (m_Cube != null)
            {
                Destroy(m_Cube);
            }

            m_Cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_Cube.name = "DeskCenterCube";
            m_Cube.transform.SetParent(StageContext.StageRoot, false);
            m_Cube.transform.localScale = Vector3.one * 0.05f;
            m_Cube.transform.localPosition = new Vector3(0f, 0.025f, 0f);
        }

        void OnGUI()
        {
            var scale = Screen.dpi > 0 ? Screen.dpi / 160f : 2f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var size = ScanContext.DeskSize;
            GUI.Label(new Rect(8f, 56f, Screen.width / scale - 16f, 22f),
                StageContext.StageRoot == null
                    ? "机：まだ（面をタップ）"
                    : $"机：幅 {size.x:F2} m × 奥行き {size.y:F2} m");
        }
    }
}
