using DeskStage.AR;
using DeskStage.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.Merge
{
    /// <summary>
    /// 撮影画面（合流のつなぎ）：カメラの映像、「とる」ボタン、案内の文。
    /// 写真方式（00_overview.md 9.2）：1枚目は真上、2枚目は斜め。2枚そろうとサーバーに送られ、答えが届くまで待つ。
    /// D の正式な撮影画面ができるまでの代わり。
    /// </summary>
    public class PhotoScanView : MonoBehaviour
    {
        [SerializeField] RawImage preview;
        [SerializeField] AspectRatioFitter fitter;
        [SerializeField] Button take;
        [SerializeField] TMP_Text takeLabel;
        [SerializeField] TMP_Text hint;

        PhotoCapture capture;

        void Awake()
        {
            if (take != null)
            {
                take.onClick.AddListener(OnTake);
            }
        }

        void Update()
        {
            if (capture == null)
            {
                capture = FindFirstObjectByType<PhotoCapture>();
            }

            var texture = ScanContext.CameraPreview;
            if (preview != null)
            {
                preview.texture = texture;
                preview.enabled = texture != null;
            }
            if (fitter != null && texture != null && texture.height > 0)
            {
                fitter.aspectRatio = (float)texture.width / texture.height;
            }

            var status = StageContext.ScanStatus;
            var hasTop = capture != null && capture.TopJpeg != null;
            var hasSide = capture != null && capture.SideJpeg != null;

            string hintText;
            string buttonText;
            bool canTake;
            if (status == ScanStatus.Lost)
            {
                hintText = "うまく いかなかったよ。もういちど やってみよう";
                buttonText = "もういちど";
                canTake = true;
            }
            else if (hasSide)
            {
                hintText = "しゃしんを おくっているよ。ちょっと まっててね";
                buttonText = "まっててね";
                canTake = false;
            }
            else
            {
                buttonText = hasTop ? "とる（ななめ）" : "とる（うえ）";
                canTake = texture != null;
                if (status == ScanStatus.Limited)
                {
                    hintText = StageContext.ScanReason == ScanReason.Dark
                        ? "くらいよ。あかるい ところで もういちど とってね"
                        : "ぶれたよ。じっと して もういちど とってね";
                }
                else
                {
                    hintText = hasTop ? "つぎは ななめから とってね（2まいめ）" : "つくえの まうえから とってね（1まいめ）";
                }
            }

            if (hint != null) hint.text = hintText;
            if (takeLabel != null) takeLabel.text = buttonText;
            if (take != null) take.interactable = canTake;
        }

        void OnTake()
        {
            if (StageContext.ScanStatus == ScanStatus.Lost)
            {
                UiRequests.RequestRescan();
            }
            else
            {
                UiRequests.RequestTakePhoto();
            }
        }
    }
}
