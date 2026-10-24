using DeskStage.Shared;
using DeskStage.UI;
using UnityEngine;

namespace DeskStage.Merge
{
    /// <summary>
    /// D の画面の切り替え（ScreenRouter）と、A・B・C の動きの受け渡し（合流のつなぎ）。
    /// D のボタンは、画面を切り替えるだけで、次の依頼を出さない所がある。それをここで補う。
    ///  ・サーバーの答えが届いたら（ScanStatus が Finished）、撮影画面からたしかめ画面へ
    ///  ・たしかめ画面から遊ぶ画面に入ったら UiRequests.Play（C の GameFlow が受ける）
    ///  ・遊ぶ・クリアの画面から出たら UiRequests.BackToList（C がキャラを片付ける）
    ///  ・ゴールしたらクリア画面へ
    /// D が、同じことを UI_System の中で行うようになったら、これは外す。
    /// </summary>
    public class MergeFlow : MonoBehaviour
    {
        [SerializeField] ScreenRouter router;

        ScreenId previous = ScreenId.Title;

        static bool InGame(ScreenId id) => id == ScreenId.Play || id == ScreenId.Clear;

        void Awake()
        {
            if (router == null)
            {
                router = FindFirstObjectByType<ScreenRouter>();
            }
        }

        void OnEnable()
        {
            if (router != null)
            {
                router.ScreenChanged += OnScreenChanged;
            }
            StageContext.ScanStatusChanged += OnScanStatusChanged;
            GameEvents.Goal += OnGoal;
        }

        void OnDisable()
        {
            if (router != null)
            {
                router.ScreenChanged -= OnScreenChanged;
            }
            StageContext.ScanStatusChanged -= OnScanStatusChanged;
            GameEvents.Goal -= OnGoal;
        }

        void OnScreenChanged(ScreenId id)
        {
            if (id == ScreenId.Play && !InGame(previous))
            {
                UiRequests.RequestPlay();
            }
            else if (InGame(previous) && !InGame(id))
            {
                UiRequests.RequestBackToList();
            }
            previous = id;
        }

        void OnScanStatusChanged(ScanStatus status, ScanReason reason)
        {
            if (status == ScanStatus.Finished && router != null && router.Current == ScreenId.Scan)
            {
                UiRequests.RequestFinishScan();
                router.Show(ScreenId.Check);
            }
        }

        void OnGoal(float seconds, bool isBest)
        {
            if (router != null && router.Current == ScreenId.Play)
            {
                router.Show(ScreenId.Clear);
            }
        }
    }
}
