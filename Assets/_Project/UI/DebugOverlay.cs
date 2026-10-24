using System.Collections.Generic;
using System.Text;
using DeskStage.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace DeskStage.UI
{
    /// <summary>
    /// 開発用のデバッグ表示（Step 6）。画面の左上に小さく重ねる。
    /// 3本指で2回たたくと表示・非表示（PC では F1 キー）。Development Build でないビルドでは何もしない。
    /// 他の担当の項目は DebugRegistry に登録してもらって出す。
    /// 子どもの画面とは別物なので、作りやすい IMGUI で描く。
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        const string ColliderToggleName = "当たり判定の枠";

        [SerializeField] bool visible;
        [SerializeField] ColliderOutline colliderOutline;
        [Tooltip("日本語の出るフォント（IMGUI 用。なければ既定のフォント）")]
        [SerializeField] Font font;

        readonly MultiTapDetector tapDetector = new MultiTapDetector();
        readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        float smoothedDelta = 1f / 60f;
        float scanStartTime = -1f;
        float scanEndTime = -1f;
        string gameState = "-";
        string tappedBox = "";
        GUIStyle labelStyle;

        public bool Visible
        {
            get => visible;
            set => visible = value;
        }

        public void SetColliderOutline(ColliderOutline value) => colliderOutline = value;

        public void SetFont(Font value) => font = value;

        void Awake()
        {
            if (!Debug.isDebugBuild)
            {
                // 完成版では出さない
                if (colliderOutline != null)
                {
                    colliderOutline.enabled = false;
                }
                Destroy(gameObject);
            }
        }

        void OnEnable()
        {
            StageContext.ScanStatusChanged += OnScanStatus;
            GameEvents.GameStarted += OnGameStarted;
            GameEvents.Goal += OnGoal;
            GameEvents.Fell += OnFell;
            if (colliderOutline != null)
            {
                DebugRegistry.AddToggle(ColliderToggleName, () => colliderOutline.enabled, v => colliderOutline.enabled = v);
            }
        }

        void OnDisable()
        {
            StageContext.ScanStatusChanged -= OnScanStatus;
            GameEvents.GameStarted -= OnGameStarted;
            GameEvents.Goal -= OnGoal;
            GameEvents.Fell -= OnFell;
            DebugRegistry.Remove(ColliderToggleName);
        }

        void OnScanStatus(ScanStatus status, ScanReason reason)
        {
            if (status == ScanStatus.Starting)
            {
                scanStartTime = Time.unscaledTime;
                scanEndTime = -1f;
            }
            else if ((status == ScanStatus.Finished || status == ScanStatus.NotStarted) && scanStartTime >= 0f && scanEndTime < 0f)
            {
                scanEndTime = Time.unscaledTime;
            }
        }

        void OnGameStarted() => gameState = "プレイ中";

        void OnGoal(float seconds, bool isBest) => gameState = string.Format("ゴール {0:0.00}秒{1}", seconds, isBest ? "（最速）" : "");

        void OnFell() => gameState = "おちた";

        void Update()
        {
            smoothedDelta = Mathf.Lerp(smoothedDelta, Time.unscaledDeltaTime, 0.05f);

            if (tapDetector.Update(CountTouches(), Time.unscaledTime)
                || (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame))
            {
                visible = !visible;
            }

            if (visible)
            {
                CheckBoxTap();
            }
        }

        static int CountTouches()
        {
            var screen = Touchscreen.current;
            if (screen == null)
            {
                return 0;
            }
            var count = 0;
            foreach (var touch in screen.touches)
            {
                if (touch.isInProgress)
                {
                    count++;
                }
            }
            return count;
        }

        // Step 6 の4：箱をタップすると、その箱の id、大きさ、高さ（cm）を出す
        void CheckBoxTap()
        {
            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame || CountTouches() > 1)
            {
                return;
            }
            var position = pointer.position.ReadValue();
            if (IsOverButton(position) || Camera.main == null)
            {
                return;
            }
            var ray = Camera.main.ScreenPointToRay(position);
            if (!Physics.Raycast(ray, out var hit, 10f, Layers.TerrainMask))
            {
                return;
            }
            tappedBox = DescribeBox(hit.collider);
        }

        // 視点の操作の板（画面いっぱいの透明な板）は、ボタン扱いにしない
        bool IsOverButton(Vector2 position)
        {
            if (EventSystem.current == null)
            {
                return false;
            }
            uiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, uiHits);
            foreach (var hit in uiHits)
            {
                if (hit.gameObject.GetComponent<ViewGesture>() == null)
                {
                    return true;
                }
            }
            return false;
        }

        static string DescribeBox(Collider collider)
        {
            var bounds = collider.bounds;
            var baseY = StageContext.StageRoot != null ? StageContext.StageRoot.position.y : 0f;
            var size = collider is BoxCollider box ? Vector3.Scale(box.size, box.transform.lossyScale) : bounds.size;
            var id = collider.gameObject.name;
            var source = FindSource(id);
            return string.Format("{0}{1}\n大きさ {2:0.0}×{3:0.0}×{4:0.0} cm\n高さ {5:0.0} cm",
                id, source != null ? "（" + source + "）" : "",
                size.x * 100f, size.y * 100f, size.z * 100f, (bounds.max.y - baseY) * 100f);
        }

        static string FindSource(string id)
        {
            foreach (var b in ScanContext.WorkingBoxes)
            {
                if (b.id == id)
                {
                    return b.source;
                }
            }
            var stage = StageContext.CurrentStage;
            if (stage != null)
            {
                foreach (var b in stage.boxes)
                {
                    if (b.id == id)
                    {
                        return b.source;
                    }
                }
            }
            return null;
        }

        string BuildText()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("FPS {0:0}\n", 1f / Mathf.Max(smoothedDelta, 0.0001f));
            sb.AppendFormat("スキャン {0}", StageContext.ScanStatus);
            if (StageContext.ScanReason != ScanReason.None)
            {
                sb.AppendFormat("（{0}）", StageContext.ScanReason);
            }
            sb.Append('\n');
            if (scanStartTime >= 0f)
            {
                var end = scanEndTime >= 0f ? scanEndTime : Time.unscaledTime;
                sb.AppendFormat("スキャン開始から {0:0.0} 秒\n", end - scanStartTime);
            }
            sb.AppendFormat("上面 {0}  ", ScanContext.Surfaces.Count);
            CountBoxes(out var auto, out var manual);
            sb.AppendFormat("箱 {0}（auto {1}・manual {2}）\n", auto + manual, auto, manual);
            sb.AppendFormat("場面 {0}  ゲーム {1}\n", StageContext.Mode, gameState);
            foreach (var item in DebugRegistry.Items)
            {
                if (item.Kind == DebugRegistry.Kind.Value)
                {
                    sb.AppendFormat("{0} {1}\n", item.Name, item.GetText());
                }
            }
            if (tappedBox != "")
            {
                sb.Append("― タップした箱 ―\n").Append(tappedBox).Append('\n');
            }
            return sb.ToString();
        }

        static void CountBoxes(out int auto, out int manual)
        {
            auto = 0;
            manual = 0;
            IEnumerable<BoxData> boxes = ScanContext.WorkingBoxes;
            if (ScanContext.WorkingBoxes.Count == 0 && StageContext.CurrentStage != null)
            {
                boxes = StageContext.CurrentStage.boxes;
            }
            foreach (var b in boxes)
            {
                if (b.source == BoxData.SourceManual)
                {
                    manual++;
                }
                else
                {
                    auto++;
                }
            }
        }

        void OnGUI()
        {
            if (!visible)
            {
                return;
            }
            // 端末の解像度にかかわらず、同じくらいの大きさに見えるようにする
            var scale = Mathf.Max(1f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
                if (font != null)
                {
                    labelStyle.font = font;
                }
                labelStyle.normal.textColor = Color.white;
            }

            var safe = Screen.safeArea;
            GUILayout.BeginArea(new Rect(safe.x / scale + 8f, (Screen.height - safe.yMax) / scale + 8f, 360f, Screen.height / scale - 16f));
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(BuildText(), labelStyle);
            foreach (var item in DebugRegistry.Items)
            {
                if (item.Kind == DebugRegistry.Kind.Toggle)
                {
                    var current = item.GetBool();
                    var next = GUILayout.Toggle(current, " " + item.Name, ToggleStyle());
                    if (next != current)
                    {
                        item.SetBool(next);
                    }
                }
                else if (item.Kind == DebugRegistry.Kind.Slider)
                {
                    var current = item.GetFloat();
                    GUILayout.Label(string.Format("{0} {1:0.00}", item.Name, current), labelStyle);
                    var next = GUILayout.HorizontalSlider(current, item.Min, item.Max, GUILayout.Height(24f));
                    if (!Mathf.Approximately(next, current))
                    {
                        item.SetFloat(next);
                    }
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        GUIStyle toggleStyle;

        GUIStyle ToggleStyle()
        {
            if (toggleStyle == null)
            {
                toggleStyle = new GUIStyle(GUI.skin.toggle) { fontSize = 14 };
                if (font != null)
                {
                    toggleStyle.font = font;
                }
            }
            return toggleStyle;
        }
    }
}
