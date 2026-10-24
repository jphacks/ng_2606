using DeskStage.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeskStage.Gameplay
{
    /// <summary>
    /// 練習用シーンで視点を試すための仮の操作。D の画面がないシーンで、マウスとキーから OrbitView と ZoomView を出す。
    /// 右ボタンを押しながら動かすと回り、Q で近づき、E で離れる。
    /// ホイールは Game ビューの表示倍率（Scale）をマウスの位置へ変えてしまうので使わない。
    /// </summary>
    public class ViewDebugInput : MonoBehaviour
    {
        [Tooltip("マウスを1ピクセル動かしたときに回る角度（度）")]
        public float degreesPerPixel = 0.3f;

        [Tooltip("Q・E を押している間に近づく・離れる速さ（m/秒）")]
        public float zoomSpeed = 0.3f;

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue() * degreesPerPixel;
                if (delta != Vector2.zero)
                {
                    UiRequests.RequestOrbitView(delta);
                }
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }
            float zoom = 0f;
            if (keyboard.qKey.isPressed) zoom += 1f;
            if (keyboard.eKey.isPressed) zoom -= 1f;
            if (zoom != 0f)
            {
                UiRequests.RequestZoomView(zoom * zoomSpeed * Time.deltaTime);
            }
        }
    }
}
