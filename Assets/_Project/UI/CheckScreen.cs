using DeskStage.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.UI
{
    /// <summary>
    /// たしかめ画面（Step 10）。箱の見た目は StageLook（BoxScan の素材）、段の印は StepMarksView が出す。
    /// 左上の「もういちどスキャン」で確かめてから Rescan を出し、スキャン画面へ戻る。
    /// </summary>
    public class CheckScreen : MonoBehaviour
    {
        [SerializeField] ScreenRouter router;
        [SerializeField] ConfirmDialog dialog;
        [SerializeField] Button rescan;

        public void Set(ScreenRouter screenRouter, ConfirmDialog confirm, Button rescanButton)
        {
            router = screenRouter;
            dialog = confirm;
            rescan = rescanButton;
        }

        void Awake() => rescan.onClick.AddListener(AskRescan);

        void OnDisable()
        {
            if (dialog != null)
            {
                dialog.Close();
            }
        }

        void AskRescan()
        {
            dialog.Show("check.rescanConfirm", () =>
            {
                UiRequests.RequestRescan();
                router.Show(ScreenId.Scan);
            });
        }
    }
}
