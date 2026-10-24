using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Gameplay
{
    /// <summary>
    /// 3D画面の視点（C_gameplay.md の Step 6）。子どもがキャラを見失わないように、キャラのまわりを回って後を追う。
    /// 遊び始めは机全体が入る距離から、1秒かけてキャラの近くへ寄る。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ViewCamera : MonoBehaviour
    {
        public CharacterSettings settings;

        /// <summary>追うキャラ</summary>
        public Transform target;

        [Tooltip("キャラの足元からどれだけ上を見るか（m）")]
        public float lookHeight = 0.02f;

        [Tooltip("後を追うなめらかさ（秒）。小さいほどぴったり追う")]
        public float followSmoothTime = 0.1f;

        [Tooltip("遊び始めにキャラの近くへ寄るまでの時間（秒）")]
        public float introDuration = 1f;

        [Tooltip("箱に近づけるときに、箱の表面から離す距離（m）")]
        public float wallMargin = 0.005f;

        [Tooltip("ステージデータがないとき（練習用シーン）の机の大きさ（m）")]
        public Vector2 fallbackDeskSize = new Vector2(0.6f, 0.6f);

        float yaw;
        float pitch;
        float distance;
        Vector3 focus;
        Vector3 focusVelocity;

        // 遊び始めの寄り
        float introElapsed;
        Vector3 introCenter;
        float introDistance;

        Camera cam;

        void Awake()
        {
            cam = GetComponent<Camera>();
            // 小さな世界なので、近くの物が切れないようにする
            cam.nearClipPlane = 0.01f;
        }

        void OnEnable()
        {
            UiRequests.OrbitView += OnOrbitView;
            UiRequests.ZoomView += OnZoomView;
        }

        void OnDisable()
        {
            UiRequests.OrbitView -= OnOrbitView;
            UiRequests.ZoomView -= OnZoomView;
        }

        void Start()
        {
            BeginIntro();
        }

        /// <summary>
        /// 机全体が入る距離から始めて、キャラの近くへ寄り直す。遊び始め（GameFlow）でも呼ぶ。
        /// </summary>
        public void BeginIntro()
        {
            yaw = target != null ? target.eulerAngles.y : 0f;
            pitch = settings.cameraPitch;
            distance = settings.cameraDistance;
            focus = FocusPoint();
            focusVelocity = Vector3.zero;

            var desk = StageContext.CurrentStage != null
                ? new Vector2(StageContext.CurrentStage.desk.width, StageContext.CurrentStage.desk.depth)
                : fallbackDeskSize;
            var root = StageContext.StageRoot;
            introCenter = root != null ? root.position : Vector3.zero;
            introDistance = OrbitMath.OverviewDistance(desk.x, desk.y, cam.fieldOfView);
            introElapsed = 0f;
        }

        void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            // キャラが動くと、なめらかに後を追う
            focus = Vector3.SmoothDamp(focus, FocusPoint(), ref focusVelocity, followSmoothTime);

            // 遊び始めは、机全体の見える位置からキャラの近くへ寄る
            Vector3 lookAt = focus;
            float wantDistance = distance;
            if (introElapsed < introDuration)
            {
                introElapsed += Time.deltaTime;
                float t = OrbitMath.Ease(introElapsed / introDuration);
                lookAt = Vector3.Lerp(introCenter, focus, t);
                wantDistance = Mathf.Lerp(introDistance, distance, t);
            }

            // カメラとキャラの間に箱があれば、キャラ側に近づける（箱の中に入らない）
            Vector3 wanted = OrbitMath.CameraPosition(lookAt, yaw, pitch, wantDistance);
            Vector3 toCamera = wanted - lookAt;
            float actualDistance = wantDistance;
            if (Physics.SphereCast(lookAt, cam.nearClipPlane, toCamera.normalized, out var hit, wantDistance, Layers.TerrainMask, QueryTriggerInteraction.Ignore))
            {
                actualDistance = Mathf.Max(hit.distance - wallMargin, cam.nearClipPlane);
            }

            transform.position = lookAt + toCamera.normalized * actualDistance;
            transform.rotation = Quaternion.LookRotation(lookAt - transform.position, Vector3.up);
        }

        Vector3 FocusPoint()
        {
            return target != null ? target.position + Vector3.up * lookHeight : Vector3.zero;
        }

        // 横の量はキャラのまわりを水平に回り、縦の量は見下ろす角度を変える（度）
        void OnOrbitView(Vector2 delta)
        {
            yaw += delta.x;
            pitch = Mathf.Clamp(pitch - delta.y, settings.cameraPitchMin, settings.cameraPitchMax);
        }

        // プラスで近づく（m）
        void OnZoomView(float amount)
        {
            distance = Mathf.Clamp(distance - amount, settings.cameraDistanceMin, settings.cameraDistanceMax);
        }
    }
}
