using DeskStage.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;

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
        DeskFrame m_DeskFrame;
        string m_LastTap = "タップ：まだ";

        float m_NextStateLog;

        void Awake() => m_DeskFrame = FindFirstObjectByType<DeskFrame>();

        void OnEnable() => StageContext.DeskDecided += OnDeskDecided;

        void OnDisable() => StageContext.DeskDecided -= OnDeskDecided;

        void Update()
        {
            // 実機のログ（xcrun devicectl --console）で状態を追えるように、2 秒ごとに出す
            if (Time.realtimeSinceStartup >= m_NextStateLog)
            {
                m_NextStateLog = Time.realtimeSinceStartup + 2f;
                Debug.Log($"[DeskDebug] AR={ARSession.state} 理由={ARSession.notTrackingReason} 状態={StageContext.ScanStatus} 画面={Screen.width}x{Screen.height} dpi={Screen.dpi}");
            }

            var touch = Touchscreen.current;
            if (touch == null || !touch.primaryTouch.press.wasPressedThisFrame)
            {
                return;
            }

            var position = touch.primaryTouch.position.ReadValue();
            if (StageContext.ScanStatus != ScanStatus.FindingDesk)
            {
                Debug.Log($"[DeskDebug] タップ位置={position} → 無視（状態 {StageContext.ScanStatus}）");
                m_LastTap = $"タップ：無視（状態が {StageContext.ScanStatus}。FindingDesk のときだけ受ける）";
                return;
            }

            var scale = Screen.dpi > 0 ? Screen.dpi / 160f : 2f;
            if (position.y < ButtonAreaPoints * scale)
            {
                return;
            }

            UiRequests.RequestSetDesk(position);
            Debug.Log($"[DeskDebug] タップ位置={position} → " + (m_DeskFrame != null ? m_DeskFrame.LastTapResult : "DeskFrame が無い"));
            m_LastTap = "タップ：" + (m_DeskFrame != null ? m_DeskFrame.LastTapResult : "DeskFrame が無い");
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
            var desk = StageContext.StageRoot == null
                ? "机：まだ（面をタップ）"
                : $"机：幅 {size.x:F2} m × 奥行き {size.y:F2} m";
            // 明るい背景（段ボールなど）でも読めるように、背景の箱を敷く
            GUI.Box(new Rect(8f, 88f, 420f, 46f), "");
            GUI.Label(new Rect(16f, 90f, 404f, 22f), desk);
            GUI.Label(new Rect(16f, 110f, 404f, 22f), m_LastTap);
        }
    }
}
