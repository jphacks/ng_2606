using DeskStage.Shared;
using TMPro;
using UnityEngine;

namespace DeskStage.UI
{
    /// <summary>
    /// 開発用：OrbitView と ZoomView の合計を表示する（C の視点のカメラがなくても Step 4 を確かめるため）。
    /// 合流前（Step 15）に DevMarker と一緒に消す。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class DevViewReadout : MonoBehaviour
    {
        Vector2 orbit;
        float zoom;
        TMP_Text text;

        void OnEnable()
        {
            text = GetComponent<TMP_Text>();
            orbit = Vector2.zero;
            zoom = 0f;
            UiRequests.OrbitView += OnOrbit;
            UiRequests.ZoomView += OnZoom;
            Refresh();
        }

        void OnDisable()
        {
            UiRequests.OrbitView -= OnOrbit;
            UiRequests.ZoomView -= OnZoom;
        }

        void OnOrbit(Vector2 delta)
        {
            orbit += delta;
            Refresh();
        }

        void OnZoom(float delta)
        {
            zoom += delta;
            Refresh();
        }

        void Refresh()
        {
            text.text = string.Format("DEV  Orbit 横 {0:0}° 縦 {1:0}°  Zoom {2:0.00}m", orbit.x, orbit.y, zoom);
        }
    }
}
