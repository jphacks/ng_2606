using DeskStage.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace DeskStage.UI
{
    /// <summary>
    /// プレイ画面の視点の操作（Step 4）。画面いっぱいの透明な板に付け、スティックとボタンより奥に置く。
    /// スティックやボタンの上で始まった指は、そちらに渡るのでここには来ない（スティックの指は視点に使わない）。
    /// </summary>
    public class ViewGesture : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IScrollHandler
    {
        [SerializeField] ViewGestureSettings settings;

        readonly ViewGestureTracker tracker = new ViewGestureTracker();

        public void SetSettings(ViewGestureSettings value) => settings = value;

        void OnEnable() => ApplySettings();

        void OnDisable() => tracker.Clear();

        void ApplySettings()
        {
            if (settings == null)
            {
                return;
            }
            tracker.DegreesPerScreenHeight = settings.DegreesPerScreenHeight;
            tracker.ZoomMetersPerScreenHeight = settings.ZoomMetersPerScreenHeight;
            tracker.InvertVertical = settings.InvertVertical;
        }

        public void OnPointerDown(PointerEventData e)
        {
            // マウスは左ボタンだけ（PC で試すとき用）
            if (e is ExtendedPointerEventData x && x.pointerType == UIPointerType.MouseOrPen
                && e.button != PointerEventData.InputButton.Left)
            {
                return;
            }
            tracker.Down(e.pointerId, e.position);
        }

        public void OnPointerUp(PointerEventData e) => tracker.Up(e.pointerId);

        // OnDrag を受けるために必要（中身はなし）
        public void OnBeginDrag(PointerEventData e)
        {
        }

        public void OnDrag(PointerEventData e)
        {
            var output = tracker.Move(e.pointerId, e.position, Screen.height);
            if (output.OrbitDegrees != Vector2.zero)
            {
                UiRequests.RequestOrbitView(output.OrbitDegrees);
            }
            if (output.ZoomMeters != 0f)
            {
                UiRequests.RequestZoomView(output.ZoomMeters);
            }
        }

        public void OnScroll(PointerEventData e)
        {
            // 開発用：PC ではマウスホイールで近づく・離れる
            if (settings != null && e.scrollDelta.y != 0f)
            {
                UiRequests.RequestZoomView(Mathf.Sign(e.scrollDelta.y) * settings.ZoomMetersPerScrollStep);
            }
        }
    }
}
