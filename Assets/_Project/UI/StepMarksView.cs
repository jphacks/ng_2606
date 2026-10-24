using System.Collections.Generic;
using DeskStage.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.UI
{
    /// <summary>
    /// たしかめ画面で、各箱の上面に段の印を出す（Step 10）。
    /// Jump は緑の丸と「とぶ」、Wall は赤の四角と「ばつ」（色だけで伝えないよう、形と文字も変える）。
    /// Wall の箱のそばには、低い物を隣に置いてもういちどスキャンするよう小さく出す。
    /// 印は画面の上の部品で、箱の上面の位置に毎フレーム合わせる。
    /// </summary>
    public class StepMarksView : MonoBehaviour
    {
        const float MarkSize = 110f;

        [SerializeField] Wording wording;
        [SerializeField] TMP_FontAsset font;
        [SerializeField] Sprite circle;

        class MarkUi
        {
            public RectTransform Root;
            public Image Back;
            public TMP_Text Label;
            public TMP_Text Hint;
        }

        readonly List<MarkUi> pool = new List<MarkUi>();
        readonly List<StepMarkLogic.Box> boxes = new List<StepMarkLogic.Box>();
        readonly List<Vector3> tops = new List<Vector3>();
        StepMarkLogic.Mark[] marks = new StepMarkLogic.Mark[0];
        float nextCompute;

        public void Set(Wording table, TMP_FontAsset fontAsset, Sprite circleSprite)
        {
            wording = table;
            font = fontAsset;
            circle = circleSprite;
        }

        void OnEnable()
        {
            ScanContext.BoxesChanged += OnBoxesChanged;
            StageContext.StageBuilt += Compute;
            Compute();
        }

        void OnDisable()
        {
            ScanContext.BoxesChanged -= OnBoxesChanged;
            StageContext.StageBuilt -= Compute;
        }

        void OnBoxesChanged(string id, BoxChange change, string source) => Compute();

        // 箱のデータ（ステージ座標）から、上から見た範囲・上面の高さ・上面の中心を集めて印を決める
        void Compute()
        {
            boxes.Clear();
            tops.Clear();
            IEnumerable<BoxData> source = ScanContext.WorkingBoxes;
            if (ScanContext.WorkingBoxes.Count == 0 && StageContext.CurrentStage != null)
            {
                source = StageContext.CurrentStage.boxes;
            }
            foreach (var b in source)
            {
                var c = new Vector2(b.center[0], b.center[2]);
                var half = new Vector2(b.size[0], b.size[2]) * 0.5f;
                // 回っている箱は、回した四隅を囲む範囲で隣を調べる
                var rad = b.yawDegrees * Mathf.Deg2Rad;
                var ex = Mathf.Abs(Mathf.Cos(rad)) * half.x + Mathf.Abs(Mathf.Sin(rad)) * half.y;
                var ez = Mathf.Abs(Mathf.Sin(rad)) * half.x + Mathf.Abs(Mathf.Cos(rad)) * half.y;
                boxes.Add(new StepMarkLogic.Box
                {
                    Footprint = Rect.MinMaxRect(c.x - ex, c.y - ez, c.x + ex, c.y + ez),
                    Height = b.size[1],
                });
                tops.Add(new Vector3(c.x, b.size[1], c.y));
            }
            marks = StepMarkLogic.Marks(boxes);
            nextCompute = Time.unscaledTime + 0.25f;
        }

        void LateUpdate()
        {
            if (Time.unscaledTime >= nextCompute)
            {
                Compute();
            }
            var cam = Camera.main;
            var root = StageContext.StageRoot;
            var container = (RectTransform)transform;
            var used = 0;
            for (int i = 0; i < marks.Length; i++)
            {
                if (marks[i] == StepMarkLogic.Mark.Walk || cam == null)
                {
                    continue;
                }
                var world = root != null ? root.TransformPoint(tops[i]) : tops[i];
                var screen = cam.WorldToScreenPoint(world);
                if (screen.z <= 0f)
                {
                    continue;
                }
                var ui = Get(used++);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screen, null, out var local);
                ui.Root.anchoredPosition = local + new Vector2(0f, MarkSize * 0.6f);
                var wall = marks[i] == StepMarkLogic.Mark.Wall;
                ui.Back.color = wall ? new Color(0.92f, 0.3f, 0.28f) : new Color(0.3f, 0.75f, 0.35f);
                ui.Back.sprite = wall ? null : circle;
                ui.Label.text = wording != null ? wording.Get(wall ? "check.mark.wall" : "check.mark.jump") : "";
                ui.Hint.gameObject.SetActive(wall);
            }
            for (int i = used; i < pool.Count; i++)
            {
                pool[i].Root.gameObject.SetActive(false);
            }
        }

        MarkUi Get(int index)
        {
            while (pool.Count <= index)
            {
                pool.Add(Create());
            }
            var ui = pool[index];
            ui.Root.gameObject.SetActive(true);
            return ui;
        }

        MarkUi Create()
        {
            var go = new GameObject("Mark", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(MarkSize, MarkSize);
            var back = go.AddComponent<Image>();
            back.raycastTarget = false;

            var label = Text("Label", go.transform, 40f);
            Stretch((RectTransform)label.transform);
            label.color = Color.white;

            var hint = Text("Hint", go.transform, 26f);
            var hr = (RectTransform)hint.transform;
            hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 0f);
            hr.anchoredPosition = new Vector2(0f, -70f);
            hr.sizeDelta = new Vector2(520f, 90f);
            hint.color = new Color(0.6f, 0.1f, 0.1f);
            hint.text = wording != null ? wording.Get("check.wallHint") : "";
            return new MarkUi { Root = rect, Back = back, Label = label, Hint = hint };
        }

        TMP_Text Text(string name, Transform parent, float size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                tmp.font = font;
            }
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }
    }
}
