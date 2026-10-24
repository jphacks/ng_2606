using DeskStage.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeskStage.Gameplay
{
    /// <summary>
    /// 練習用シーンで視点を試すための仮の操作。D の画面がないシーンで、マウスから OrbitView と ZoomView を出す。
    /// 右ボタンを押しながら動かすと回り、ホイールで近づく・離れる。
    /// </summary>
    public class ViewDebugInput : MonoBehaviour
    {
        [Tooltip("マウスを1ピクセル動かしたときに回る角度（度）")]
        public float degreesPerPixel = 0.3f;

        [Tooltip("ホイール1目盛りで近づく距離（m）")]
        public float zoomPerStep = 0.03f;

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }
            if (mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue() * degreesPerPixel;
                if (delta != Vector2.zero)
                {
                    UiRequests.RequestOrbitView(delta);
                }
            }
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll != 0f)
            {
                UiRequests.RequestZoomView(Mathf.Sign(scroll) * zoomPerStep);
            }
        }
    }
}
