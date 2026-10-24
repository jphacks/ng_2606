using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace DeskStage.AR
{
    /// <summary>
    /// P0 の検証用の表示と記録（A_ar.md Step 3）。
    /// どの物の上面が、何秒で、どの高さで見つかるかを実機で見て、CSV に残す。
    /// この部品は検証専用で、M1 以降の本体では使わない。
    /// </summary>
    [RequireComponent(typeof(ARPlaneManager))]
    public class PlaneProbe : MonoBehaviour
    {
        [SerializeField] ScanSettings m_Settings;

        /// <summary>面ごとに覚えておくこと。</summary>
        class PlaneRecord
        {
            public ARPlane Plane;
            public float FoundAtSeconds;
            public GameObject Visual;
        }

        /// <summary>タップで調べた点。</summary>
        struct TapRecord
        {
            public float Seconds;
            public Vector2 ScreenPosition;
            public float HeightMeters;
            public string HitKind;
        }

        readonly Dictionary<TrackableId, PlaneRecord> m_Records = new Dictionary<TrackableId, PlaneRecord>();
        readonly List<TapRecord> m_Taps = new List<TapRecord>();
        readonly List<ARRaycastHit> m_Hits = new List<ARRaycastHit>();

        ARPlaneManager m_PlaneManager;
        ARRaycastManager m_RaycastManager;
        ARSession m_Session;
        PlaneWatcher m_Watcher;
        Material m_PlaneMaterial;

        float m_StartedAt;
        string m_LastMessage = "";

        /// <summary>スキャン開始からの秒数。</summary>
        float Elapsed => Time.realtimeSinceStartup - m_StartedAt;

        void Awake()
        {
            m_PlaneManager = GetComponent<ARPlaneManager>();
            m_RaycastManager = GetComponent<ARRaycastManager>();
            m_Session = FindFirstObjectByType<ARSession>();
            m_Watcher = GetComponent<PlaneWatcher>();
            m_PlaneMaterial = CreateTransparentMaterial();
            m_StartedAt = Time.realtimeSinceStartup;
        }

        void OnEnable()
        {
            m_PlaneManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            m_PlaneManager.trackablesChanged.AddListener(OnPlanesChanged);
        }

        void OnDisable()
        {
            m_PlaneManager.trackablesChanged.RemoveListener(OnPlanesChanged);
        }

        void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
        {
            foreach (var plane in args.added)
            {
                if (m_Records.ContainsKey(plane.trackableId))
                {
                    continue;
                }
                m_Records[plane.trackableId] = new PlaneRecord
                {
                    Plane = plane,
                    FoundAtSeconds = Elapsed,
                    Visual = CreateVisual(plane),
                };
            }

            foreach (var pair in args.removed)
            {
                if (!m_Records.TryGetValue(pair.Key, out var record))
                {
                    continue;
                }
                if (record.Visual != null)
                {
                    Destroy(record.Visual);
                }
                m_Records.Remove(pair.Key);
            }
        }

        void Update()
        {
            foreach (var record in m_Records.Values)
            {
                UpdateVisual(record);
            }
        }

        // ---- 表示 ----

        /// <summary>URP で半透明に描けるマテリアルを作る。</summary>
        static Material CreateTransparentMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            var material = new Material(shader);
            material.SetFloat("_Surface", 1f);           // Transparent
            material.SetFloat("_Blend", 0f);             // Alpha
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return material;
        }

        GameObject CreateVisual(ARPlane plane)
        {
            var go = new GameObject("PlaneVisual");
            go.transform.SetParent(plane.transform, false);
            go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_PlaneMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<ARPlaneMeshVisualizer>();
            return go;
        }

        void UpdateVisual(PlaneRecord record)
        {
            if (record.Visual == null)
            {
                return;
            }
            var renderer = record.Visual.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                return;
            }
            // 低い＝青、高い＝赤。机からの差を 0〜20cm で割り当てる
            var t = Mathf.Clamp01(HeightAboveBase(record.Plane) / 0.20f);
            var color = Color.Lerp(new Color(0.2f, 0.4f, 1f), new Color(1f, 0.2f, 0.2f), t);
            color.a = 0.45f;
            renderer.material.color = color;
        }

        // ---- 高さの基準 ----

        /// <summary>いちばん低い「大きな面」の高さ。ふつうは机の天面。</summary>
        float BaseHeight()
        {
            var minArea = m_Settings != null ? m_Settings.minSurfaceArea : 0.0016f;
            var found = false;
            var lowest = 0f;
            foreach (var record in m_Records.Values)
            {
                var plane = record.Plane;
                if (plane == null || Area(plane) < minArea * 10f)
                {
                    continue;
                }
                var y = plane.transform.position.y;
                if (!found || y < lowest)
                {
                    lowest = y;
                    found = true;
                }
            }
            return found ? lowest : 0f;
        }

        float HeightAboveBase(ARPlane plane)
            => plane == null ? 0f : plane.transform.position.y - BaseHeight();

        static float Area(ARPlane plane) => plane.size.x * plane.size.y;

        // ---- タップで高さを調べる ----

        void CheckTap(Vector2 screenPosition)
        {
            m_Hits.Clear();
            var kind = "none";
            var height = 0f;

            if (m_RaycastManager != null &&
                m_RaycastManager.Raycast(screenPosition, m_Hits,
                    TrackableType.PlaneWithinPolygon | TrackableType.FeaturePoint))
            {
                var hit = m_Hits[0];
                height = hit.pose.position.y - BaseHeight();
                kind = hit.hitType.ToString();
            }

            m_Taps.Add(new TapRecord
            {
                Seconds = Elapsed,
                ScreenPosition = screenPosition,
                HeightMeters = height,
                HitKind = kind,
            });

            m_LastMessage = kind == "none"
                ? "タップ：何にも当たらなかった"
                : $"タップ：{height * 100f:F1} cm（{kind}）";
        }

        // ---- 記録とリセット ----

        void WriteCsv()
        {
            var text = new StringBuilder();
            text.AppendLine("kind,id,height_cm,size_x_cm,size_z_cm,found_at_s");
            foreach (var record in m_Records.Values)
            {
                var plane = record.Plane;
                if (plane == null)
                {
                    continue;
                }
                text.AppendFormat(CultureInfo.InvariantCulture,
                    "plane,{0},{1:F1},{2:F1},{3:F1},{4:F1}\n",
                    plane.trackableId,
                    HeightAboveBase(plane) * 100f,
                    plane.size.x * 100f,
                    plane.size.y * 100f,
                    record.FoundAtSeconds);
            }
            foreach (var tap in m_Taps)
            {
                text.AppendFormat(CultureInfo.InvariantCulture,
                    "tap,,{0:F1},,,{1:F1}\n", tap.HeightMeters * 100f, tap.Seconds);
            }

            var path = Path.Combine(Application.persistentDataPath,
                $"planecheck-{System.DateTime.Now:yyyyMMdd-HHmmss}.csv");
            File.WriteAllText(path, text.ToString(), Encoding.UTF8);
            m_LastMessage = "記録した：" + Path.GetFileName(path);
            Debug.Log("[PlaneProbe] " + path);
        }

        /// <summary>AR の記録を消してやり直す（Step 12 もういちどスキャンの試し）。</summary>
        void ResetScan()
        {
            foreach (var record in m_Records.Values)
            {
                if (record.Visual != null)
                {
                    Destroy(record.Visual);
                }
            }
            m_Records.Clear();
            m_Taps.Clear();

            if (m_Session != null)
            {
                m_Session.Reset();
            }

            m_StartedAt = Time.realtimeSinceStartup;
            m_LastMessage = "リセットした";
        }

        // ---- 画面（検証用なので IMGUI で完結させる）----

        void OnGUI()
        {
            var scale = Screen.dpi > 0 ? Screen.dpi / 160f : 2f;
            GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), Vector2.zero);

            var w = Screen.width / scale;

            // 面ごとの高さと大きさを、面の位置に重ねて出す
            var camera = Camera.main;
            if (camera != null)
            {
                foreach (var record in m_Records.Values)
                {
                    var plane = record.Plane;
                    if (plane == null)
                    {
                        continue;
                    }
                    var screen = camera.WorldToScreenPoint(plane.transform.position);
                    if (screen.z <= 0f)
                    {
                        continue;
                    }
                    var rect = new Rect(screen.x / scale - 60f,
                                        (Screen.height - screen.y) / scale - 12f, 120f, 34f);
                    GUI.Label(rect,
                        $"{HeightAboveBase(plane) * 100f:F1} cm\n" +
                        $"{plane.size.x * 100f:F0}×{plane.size.y * 100f:F0} cm");
                }
            }

            GUI.Box(new Rect(8f, 8f, 230f, 76f), "");
            GUI.Label(new Rect(16f, 12f, 220f, 20f), $"面：{m_Records.Count} 枚");
            GUI.Label(new Rect(16f, 32f, 220f, 20f),
                m_Watcher != null
                    ? $"経過：{Elapsed:F0} 秒　見張り：{m_Watcher.Planes.Count} 枚"
                    : $"経過：{Elapsed:F0} 秒");
            GUI.Label(new Rect(16f, 52f, 220f, 20f), m_LastMessage);

            if (GUI.Button(new Rect(w - 180f, 8f, 80f, 44f), "記録"))
            {
                WriteCsv();
            }
            if (GUI.Button(new Rect(w - 92f, 8f, 80f, 44f), "リセット"))
            {
                ResetScan();
            }

            // ボタン以外をタップしたら、その先の高さを調べる
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.mousePosition.y * scale > 60f)
            {
                CheckTap(new Vector2(e.mousePosition.x * scale,
                                     Screen.height - e.mousePosition.y * scale));
            }
        }
    }
}
