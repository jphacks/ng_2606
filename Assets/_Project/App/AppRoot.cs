using DeskStage.Merge;
using DeskStage.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DeskStage.App
{
    /// <summary>
    /// 新しい UI の入口。画面の入れ物（Canvas、安全な範囲、EventSystem）と、各画面、全体の流れ（AppFlow）、重なる部品（AppOverlays）を作る。
    /// 見た目の部品は、すべて、起動したときに、コードで作る（シーンやプレハブに、細かい配線を持たない）。
    /// 設定が要るのは、日本語のフォントと、3D の見た目の設定、ならべかたの絵だけ。
    /// </summary>
    public class AppRoot : MonoBehaviour
    {
        // 基準の画面の大きさ（CanvasScaler）。横長
        public static readonly Vector2 ReferenceResolution = new Vector2(2532f, 1170f);

        [SerializeField] TMP_FontAsset font;
        [SerializeField] ViewLook viewLook;
        [SerializeField] ViewGestureSettings viewGestureSettings;
        [SerializeField] Wording wording;
        [SerializeField] Sprite[] tipSprites;
        [SerializeField] CharacterCapture cameraCapture;
        [SerializeField] Texture2D titleImage;
        [SerializeField] Material gimmickMaterial;
        [SerializeField] AppScreenId firstScreen = AppScreenId.Title;

        public AppContext Context { get; private set; }

        public Canvas Canvas { get; private set; }

        void Awake()
        {
            Theme.Font = font;
            Build(null);
        }

        void Start()
        {
            Context.Router.Show(firstScreen, remember: false, animate: false);
        }

        /// <summary>
        /// 画面の入れ物と各画面を作る。previewCamera を渡すと、その camera に描く Canvas にする（画像に書き出して確かめるとき）。
        /// </summary>
        public AppContext Build(Camera previewCamera)
        {
            if (Context != null)
            {
                return Context;
            }
            if (font != null) Theme.Font = font;

            Context = new AppContext
            {
                ViewLook = viewLook,
                ViewGestureSettings = viewGestureSettings,
                Wording = wording,
                TipSprites = tipSprites ?? new Sprite[0],
                Camera = cameraCapture,
                TitleImage = titleImage,
            };

            // Canvas
            var canvasGo = new GameObject("AppCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            Canvas = canvasGo.AddComponent<Canvas>();
            if (previewCamera != null)
            {
                Canvas.renderMode = RenderMode.ScreenSpaceCamera;
                Canvas.worldCamera = previewCamera;
                Canvas.planeDistance = 1f;
            }
            else
            {
                Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            Context.Canvas = Canvas;

            // 画面いっぱい / 安全な範囲
            Context.Full = UiKit.Fill(UiKit.NewRect("Full", Canvas.transform));
            var safe = UiKit.Fill(UiKit.NewRect("SafeArea", Context.Full));
            safe.gameObject.AddComponent<SafeAreaFitter>();
            Context.SafeArea = safe;

            // 切り替え役、全体の流れ、重なる部品
            Context.Router = gameObject.AddComponent<AppRouter>();
            Context.Flow = gameObject.AddComponent<AppFlow>();
            Context.Gimmicks = gameObject.AddComponent<GimmickSystem>();
            Context.Gimmicks.SetMaterial(gimmickMaterial);
            Context.Overlays = gameObject.AddComponent<AppOverlays>();

            Context.Flow.Init(Context);

            AddScreen<TitleScreen>(AppScreenId.Title);
            AddScreen<HomeScreen>(AppScreenId.Home);
            AddScreen<HowToScreen>(AppScreenId.HowTo);
            AddScreen<ReachScreen>(AppScreenId.Reach);
            AddScreen<CaptureScreen>(AppScreenId.Capture);
            AddScreen<GeneratingScreen>(AppScreenId.Generating);
            AddScreen<CheckStageScreen>(AppScreenId.Check);
            AddScreen<PlayScreen>(AppScreenId.Play);
            AddScreen<ClearScreen>(AppScreenId.Clear);
            AddScreen<GameOverScreen>(AppScreenId.GameOver);
            AddScreen<CharactersScreen>(AppScreenId.Characters);
            AddScreen<BackdropScreen>(AppScreenId.Backdrop);

            // 重なる部品は、画面よりあとに作って、いちばん手前に置く
            Context.Overlays.Build(Context.Full);

            if (previewCamera == null)
            {
                EnsureEventSystem();
            }
            return Context;
        }

        T AddScreen<T>(AppScreenId id) where T : AppScreen
        {
            var rect = UiKit.Fill(UiKit.NewRect(typeof(T).Name, Context.Full));
            rect.gameObject.AddComponent<CanvasGroup>();
            var screen = rect.gameObject.AddComponent<T>();
            screen.Init(Context);
            Context.Router.Register(id, screen);
            return screen;
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }
    }
}
