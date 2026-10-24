using System.Text;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.AR
{
    /// <summary>
    /// Step 11 の確認用。整えた上面の一覧（ScanContext.Surfaces）を画面に出す。
    /// 番号、高さ、上から見た大きさ（外接する長方形）。検証専用の使い捨て。
    /// </summary>
    public class SurfaceDebugPanel : MonoBehaviour
    {
        readonly StringBuilder m_Text = new StringBuilder();
        string m_Shown = "";
        int m_Count = -1;
        float m_ChangedAt;

        void OnEnable() => ScanContext.SurfacesChanged += OnChanged;

        void OnDisable() => ScanContext.SurfacesChanged -= OnChanged;

        void OnChanged()
        {
            m_ChangedAt = Time.realtimeSinceStartup;
            m_Count = ScanContext.Surfaces.Count;
            m_Text.Clear();
            foreach (var s in ScanContext.Surfaces)
            {
                float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
                foreach (var p in s.Outline)
                {
                    minX = Mathf.Min(minX, p.x);
                    maxX = Mathf.Max(maxX, p.x);
                    minZ = Mathf.Min(minZ, p.y);
                    maxZ = Mathf.Max(maxZ, p.y);
                }

                m_Text.AppendLine($"#{s.Id}  高さ {s.Height * 100f:F1} cm  {(maxX - minX) * 100f:F0}×{(maxZ - minZ) * 100f:F0} cm");
            }

            m_Shown = m_Text.ToString();
            Debug.Log($"[SurfaceDebug] 上面 {m_Count} 件\n{m_Shown}");
        }

        void OnGUI()
        {
            if (m_Count < 0)
            {
                return;
            }

            var scale = Screen.dpi > 0 ? Screen.dpi / 160f : 2f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            var lines = Mathf.Max(1, m_Count) + 1;
            var x = Screen.width / scale - 270f;
            GUI.Box(new Rect(x, 60f, 262f, 8f + lines * 20f), "");
            GUI.Label(new Rect(x + 8f, 62f, 250f, 20f),
                $"上面：{m_Count} 件（{Time.realtimeSinceStartup - m_ChangedAt:F0} 秒前に更新）");
            GUI.Label(new Rect(x + 8f, 82f, 250f, lines * 20f), m_Shown);
        }
    }
}
