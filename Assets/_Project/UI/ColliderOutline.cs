using DeskStage.Shared;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeskStage.UI
{
    /// <summary>
    /// 開発用：Terrain レイヤーの Box Collider の枠を線で描く（Step 6 の5、当たり判定の枠の表示）。
    /// デバッグ表示の切り替えでオン・オフする。
    /// </summary>
    public class ColliderOutline : MonoBehaviour
    {
        static readonly Color LineColor = new Color(0f, 1f, 0.3f);

        Material material;
        BoxCollider[] colliders = new BoxCollider[0];
        float nextRefresh;

        void OnEnable()
        {
            if (material == null)
            {
                material = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
                material.SetInt("_ZTest", (int)CompareFunction.Always);
            }
            RenderPipelineManager.endCameraRendering += OnEndCamera;
        }

        void OnDisable() => RenderPipelineManager.endCameraRendering -= OnEndCamera;

        void OnDestroy()
        {
            if (material != null)
            {
                Destroy(material);
            }
        }

        void Update()
        {
            // 箱は増えたり消えたりするので、0.5秒ごとに探し直す
            if (Time.unscaledTime < nextRefresh)
            {
                return;
            }
            nextRefresh = Time.unscaledTime + 0.5f;
            var all = FindObjectsByType<BoxCollider>(FindObjectsSortMode.None);
            colliders = System.Array.FindAll(all, c => c.gameObject.layer == Layers.Terrain && c.enabled);
        }

        void OnEndCamera(ScriptableRenderContext context, Camera cam)
        {
            if (material == null || colliders.Length == 0)
            {
                return;
            }
            material.SetPass(0);
            GL.PushMatrix();
            GL.LoadProjectionMatrix(GL.GetGPUProjectionMatrix(cam.projectionMatrix, false));
            GL.modelview = cam.worldToCameraMatrix;
            GL.Begin(GL.LINES);
            GL.Color(LineColor);
            foreach (var c in colliders)
            {
                if (c != null)
                {
                    DrawBox(c);
                }
            }
            GL.End();
            GL.PopMatrix();
        }

        static void DrawBox(BoxCollider c)
        {
            var t = c.transform;
            var h = c.size * 0.5f;
            var p = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                var local = c.center + new Vector3((i & 1) == 0 ? -h.x : h.x, (i & 2) == 0 ? -h.y : h.y, (i & 4) == 0 ? -h.z : h.z);
                p[i] = t.TransformPoint(local);
            }
            // 12本の辺：1つの軸だけが違う角どうしを結ぶ
            for (int i = 0; i < 8; i++)
            {
                for (int bit = 1; bit < 8; bit <<= 1)
                {
                    var j = i | bit;
                    if (j != i)
                    {
                        GL.Vertex(p[i]);
                        GL.Vertex(p[j]);
                    }
                }
            }
        }
    }
}
