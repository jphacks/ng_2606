using System.Collections.Generic;
using DeskStage.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeskStage.UI
{
    /// <summary>
    /// たしかめ画面の読み取りの補助（Step 11）。「なおす」を押したときだけ操作を出す。
    /// 指の動きを読んで B への依頼（AddBox、SelectBox、MoveBox、ResizeBox、RotateBox、DeleteBox、UndoBox）を出す。
    /// 指で動かしている間は、選んだ箱の写し（BoxSelected の素材）が指にぴったりついてくる。
    /// 離したら変えた量をまとめて依頼し、写しを B がそろえた位置へなめらかに寄せてから消す。
    /// 画面いっぱいの透明な板に付ける（ボタンより奥）。
    /// </summary>
    public class AssistController : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        const float SettleSeconds = 0.15f;

        [SerializeField] ViewLook look;
        [SerializeField] Button fixButton;
        [SerializeField] GameObject tools;
        [SerializeField] Button addButton;
        [SerializeField] Button undoButton;
        [SerializeField] Button deleteButton;
        [SerializeField] RectTransform heightHandle;
        [SerializeField] TMP_Text hint;

        enum Edit
        {
            None,
            Move,
            Height,
            TwoFinger,
            Settle,
        }

        readonly Dictionary<int, Vector2> pointers = new Dictionary<int, Vector2>();
        readonly TwoFingerGesture twoFinger = new TwoFingerGesture();
        bool open;
        bool addMode;
        Edit edit;
        GameObject ghost;
        Renderer hiddenReal;
        Transform real;
        Vector3 startPosition;
        Vector3 startScale;
        float startYaw;
        Vector3 moveStartHit;
        float settleTime;
        Color addColor;
        readonly List<int> twoIds = new List<int>();

        public void Set(ViewLook viewLook, Button fix, GameObject toolGroup, Button add, Button undo, Button delete,
            RectTransform handle, TMP_Text hintText)
        {
            look = viewLook;
            fixButton = fix;
            tools = toolGroup;
            addButton = add;
            undoButton = undo;
            deleteButton = delete;
            heightHandle = handle;
            hint = hintText;
        }

        void Awake()
        {
            fixButton.onClick.AddListener(() => SetOpen(!open));
            addButton.onClick.AddListener(() => SetAddMode(!addMode));
            undoButton.onClick.AddListener(UiRequests.RequestUndoBox);
            deleteButton.onClick.AddListener(UiRequests.RequestDeleteBox);
            addColor = addButton.GetComponent<Image>().color;
        }

        void OnEnable() => SetOpen(false);

        void OnDisable()
        {
            CancelEdit();
            pointers.Clear();
        }

        void SetOpen(bool value)
        {
            open = value;
            tools.SetActive(open);
            SetAddMode(false);
            if (!open)
            {
                CancelEdit();
            }
        }

        // 「＋はこ」を押した後は、ボタンの色を変えて「おきたい ところを タップしてね」を出す
        void SetAddMode(bool value)
        {
            addMode = value && open;
            addButton.GetComponent<Image>().color = addMode ? new Color(1f, 0.85f, 0.3f) : addColor;
            hint.gameObject.SetActive(addMode);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!open)
            {
                return;
            }
            pointers[e.pointerId] = e.position;
            if (pointers.Count == 2 && (edit == Edit.None || edit == Edit.Settle) && BeginGhost())
            {
                twoIds.Clear();
                twoIds.AddRange(pointers.Keys);
                twoFinger.Begin(pointers[twoIds[0]], pointers[twoIds[1]]);
                edit = Edit.TwoFinger;
            }
        }

        public void OnPointerUp(PointerEventData e)
        {
            pointers.Remove(e.pointerId);
            if (edit == Edit.TwoFinger && pointers.Count < 2)
            {
                EndEdit();
            }
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (!open || e.dragging || (edit != Edit.None && edit != Edit.Settle))
            {
                return;
            }
            if (addMode)
            {
                // 見つからなかった物の上をタップして、箱を足す
                UiRequests.RequestAddBox(e.position);
                SetAddMode(false);
            }
            else
            {
                UiRequests.RequestSelectBox(e.position);
            }
        }

        public void OnBeginDrag(PointerEventData e)
        {
            // 1本指で、選んでいる箱の上から動かし始めたときだけ動かす
            if (!open || (edit != Edit.None && edit != Edit.Settle) || pointers.Count != 1 || !IsOverSelected(e.pressPosition))
            {
                return;
            }
            if (BeginGhost() && TryDeskPoint(e.pressPosition, out moveStartHit))
            {
                edit = Edit.Move;
            }
            else
            {
                CancelEdit();
            }
        }

        public void OnDrag(PointerEventData e)
        {
            pointers[e.pointerId] = e.position;
            if (edit == Edit.Move && TryDeskPoint(e.position, out var hit))
            {
                var delta = hit - moveStartHit;
                ghost.transform.localPosition = startPosition + new Vector3(delta.x, 0f, delta.z);
            }
            else if (edit == Edit.TwoFinger && twoIds.Count == 2
                && pointers.TryGetValue(twoIds[0], out var a) && pointers.TryGetValue(twoIds[1], out var b))
            {
                twoFinger.Update(a, b);
                ghost.transform.localScale = new Vector3(startScale.x * twoFinger.Scale, startScale.y, startScale.z * twoFinger.Scale);
                ghost.transform.localRotation = Quaternion.Euler(0f, startYaw + twoFinger.Angle, 0f);
            }
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (edit == Edit.Move)
            {
                EndEdit();
            }
        }

        // 上下の取っ手（AssistHandle から呼ぶ）
        public void BeginHeight()
        {
            if (open && (edit == Edit.None || edit == Edit.Settle) && BeginGhost())
            {
                edit = Edit.Height;
            }
        }

        public void DragHeight(float pixels)
        {
            if (edit != Edit.Height)
            {
                return;
            }
            // 箱の上面の位置で、1m が画面の何ピクセルかを求め、なぞった量を高さに直す
            var h = Mathf.Max(0.005f, startScale.y + pixels / PixelsPerMeter());
            ghost.transform.localScale = new Vector3(startScale.x, h, startScale.z);
            var p = startPosition;
            ghost.transform.localPosition = new Vector3(p.x, h * 0.5f, p.z);
        }

        /// <summary>指を離した：変えた量をまとめて依頼し、写しを B の箱へ寄せる。</summary>
        public void EndEdit()
        {
            if (ghost == null || edit == Edit.None || edit == Edit.Settle)
            {
                return;
            }
            var t = ghost.transform;
            var move = t.localPosition - startPosition;
            var size = t.localScale - startScale;
            var yaw = Mathf.DeltaAngle(startYaw, t.localEulerAngles.y);
            if (edit == Edit.Move && (move.x != 0f || move.z != 0f))
            {
                UiRequests.RequestMoveBox(new Vector2(move.x, move.z));
            }
            if (edit == Edit.Height && size.y != 0f)
            {
                UiRequests.RequestResizeBox(new Vector3(0f, size.y, 0f));
            }
            if (edit == Edit.TwoFinger)
            {
                if (twoFinger.Current == TwoFingerGesture.Mode.Pinch)
                {
                    UiRequests.RequestResizeBox(new Vector3(size.x, 0f, size.z));
                }
                else if (twoFinger.Current == TwoFingerGesture.Mode.Twist && yaw != 0f)
                {
                    UiRequests.RequestRotateBox(yaw);
                }
            }
            edit = Edit.Settle;
            settleTime = 0f;
        }

        void CancelEdit()
        {
            if (ghost != null)
            {
                Destroy(ghost);
            }
            ghost = null;
            if (hiddenReal != null)
            {
                hiddenReal.enabled = true;
            }
            hiddenReal = null;
            real = null;
            edit = Edit.None;
        }

        // 選んでいる箱の写しを作り、本物は隠す
        bool BeginGhost()
        {
            // 前の写しが寄せている途中なら、打ち切って次の操作を受け付ける
            if (edit == Edit.Settle)
            {
                CancelEdit();
            }
            real = FindSelected();
            if (real == null || look == null)
            {
                return false;
            }
            ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ghost.name = "AssistGhost";
            ghost.layer = Layers.ScanView;
            Destroy(ghost.GetComponent<Collider>());
            ghost.GetComponent<Renderer>().sharedMaterial = look.BoxSelected;
            ghost.transform.SetParent(real.parent, false);
            ghost.transform.localPosition = startPosition = real.localPosition;
            ghost.transform.localRotation = real.localRotation;
            ghost.transform.localScale = startScale = real.localScale;
            startYaw = real.localEulerAngles.y;
            hiddenReal = real.GetComponent<Renderer>();
            if (hiddenReal != null)
            {
                hiddenReal.enabled = false;
            }
            return true;
        }

        static Transform FindSelected()
        {
            var id = ScanContext.SelectedBoxId;
            var root = StageContext.StageRoot;
            return string.IsNullOrEmpty(id) || root == null ? null : root.Find(id);
        }

        static bool IsOverSelected(Vector2 screen)
        {
            var cam = Camera.main;
            if (cam == null || !Physics.Raycast(cam.ScreenPointToRay(screen), out var hit, 10f, Layers.TerrainMask))
            {
                return false;
            }
            return hit.collider.name == ScanContext.SelectedBoxId;
        }

        // 画面の点から、選んだ箱の上面の高さの水平な面に当たる点（StageRoot の中の座標）
        bool TryDeskPoint(Vector2 screen, out Vector3 local)
        {
            local = Vector3.zero;
            var cam = Camera.main;
            var root = StageContext.StageRoot;
            if (cam == null || root == null)
            {
                return false;
            }
            var plane = new Plane(root.up, root.TransformPoint(new Vector3(0f, startScale.y, 0f)));
            var ray = cam.ScreenPointToRay(screen);
            if (!plane.Raycast(ray, out var enter))
            {
                return false;
            }
            local = root.InverseTransformPoint(ray.GetPoint(enter));
            return true;
        }

        float PixelsPerMeter()
        {
            var cam = Camera.main;
            var root = StageContext.StageRoot;
            if (cam == null || root == null)
            {
                return 1000f;
            }
            var top = root.TransformPoint(startPosition + Vector3.up * (startScale.y * 0.5f));
            var above = root.TransformPoint(startPosition + Vector3.up * (startScale.y * 0.5f + 0.01f));
            var px = Vector2.Distance(cam.WorldToScreenPoint(top), cam.WorldToScreenPoint(above));
            return Mathf.Max(1f, px * 100f);
        }

        void LateUpdate()
        {
            if (edit == Edit.Settle)
            {
                Settle();
            }
            PlaceFloatingButtons();
        }

        // 写しを B の箱（そろえた後の位置と大きさ）へなめらかに寄せてから消す
        void Settle()
        {
            settleTime += Time.unscaledDeltaTime;
            if (ghost == null || real == null || settleTime >= SettleSeconds)
            {
                CancelEdit();
                return;
            }
            var k = settleTime / SettleSeconds;
            var t = ghost.transform;
            t.localPosition = Vector3.Lerp(t.localPosition, real.localPosition, k);
            t.localScale = Vector3.Lerp(t.localScale, real.localScale, k);
            t.localRotation = Quaternion.Slerp(t.localRotation, real.localRotation, k);
        }

        // 選んだ箱の横に「けす」、上に「上下の取っ手」を出す
        void PlaceFloatingButtons()
        {
            var selected = open ? FindSelected() : null;
            var cam = Camera.main;
            var show = selected != null && cam != null;
            deleteButton.gameObject.SetActive(show);
            heightHandle.gameObject.SetActive(show);
            if (!show)
            {
                return;
            }
            var target = ghost != null ? ghost.transform : selected;
            var top = target.TransformPoint(new Vector3(0f, 0.5f, 0f));
            var screen = cam.WorldToScreenPoint(top);
            var container = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screen, null, out var local);
            heightHandle.anchoredPosition = local + new Vector2(0f, 120f);
            ((RectTransform)deleteButton.transform).anchoredPosition = local + new Vector2(230f, 20f);
        }
    }
}
