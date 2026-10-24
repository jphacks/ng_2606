using System.Collections;
using DeskStage.AR;
using UnityEngine;

namespace DeskStage.Merge
{
    /// <summary>枠の場所を撮った写真。添字 0 が左下の行。</summary>
    public struct FrameResult
    {
        public bool Ok;
        public string Reason;
        public Color32[] Pixels;
        public int Width;
        public int Height;
    }

    /// <summary>
    /// 人形を撮って、人形だけを取り出す（キャラクターを写真で作る）。
    /// カメラ（WebCamTexture）を開き、端末の向きに合わせて起こした映像を Preview に出す。
    /// 2 回撮る：先に「背景だけ」（CaptureFrame）、次に、同じ場所に人形を置いて撮る（TakeCutout）。
    /// 2 枚の差から人形を取り出す（計算は BackgroundSubtraction）。撮る場所は、人型の枠（HumanMask.CropRegion）の中。
    /// 背景の色で自動で切り抜く方法（CutoutMath）は、うまく切れなかったので、使わない。
    /// 机の写真を撮る PhotoCapture と、同じカメラを使うので、同時には開かない（撮影の画面では、これを閉じておく）。
    /// </summary>
    public class CharacterCapture : MonoBehaviour
    {
        // 撮る絵の高さ（画素）。横は、枠の縦横の比（HumanMask.Aspect）で決まる
        const int OutHeight = 512;

        WebCamTexture webcam;
        RenderTexture upright;

        public bool IsOpen => webcam != null;

        /// <summary>いまの映像（端末の向きに合わせた物）。まだなら null。</summary>
        public Texture Preview => upright != null ? upright : null;

        /// <summary>カメラを開けなかった理由。開けたら空。</summary>
        public string Problem { get; private set; } = "";

        public void Open()
        {
            Close();
            StartCoroutine(OpenRoutine());
        }

        IEnumerator OpenRoutine()
        {
            Problem = "";
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                Problem = "カメラが ゆるされていません";
                yield break;
            }

            string deviceName = null;
            foreach (var d in WebCamTexture.devices)
            {
                if (!d.isFrontFacing)
                {
                    deviceName = d.name;
                    break;
                }
            }
            if (deviceName == null && WebCamTexture.devices.Length > 0)
            {
                deviceName = WebCamTexture.devices[0].name;
            }
            if (deviceName == null)
            {
                Problem = "カメラが みつかりません";
                yield break;
            }

            webcam = new WebCamTexture(deviceName, 1280, 960);
            webcam.Play();
        }

        public void Close()
        {
            StopAllCoroutines();
            if (webcam != null)
            {
                webcam.Stop();
                Destroy(webcam);
                webcam = null;
            }
            if (upright != null)
            {
                upright.Release();
                Destroy(upright);
                upright = null;
            }
        }

        void OnDisable() => Close();

        void Update()
        {
            // 最初の映像が届くまで、WebCamTexture は 16x16 のまま
            if (webcam == null || webcam.width <= 16 || !webcam.didUpdateThisFrame)
            {
                return;
            }
            if (!PhotoMath.TryUprightTransform(webcam.videoRotationAngle, webcam.videoVerticallyMirrored, out var scale, out var offset))
            {
                scale = Vector2.one;
                offset = Vector2.zero;
            }
            if (upright == null || upright.width != webcam.width || upright.height != webcam.height)
            {
                if (upright != null)
                {
                    upright.Release();
                    Destroy(upright);
                }
                upright = new RenderTexture(webcam.width, webcam.height, 0);
            }
            Graphics.Blit(webcam, upright, scale, offset);
        }

        /// <summary>いまの映像の、枠の場所を、写真として撮る（切り抜きはしない）。映像がまだなら、失敗の結果を返す。</summary>
        public FrameResult CaptureFrame()
        {
            var source = Preview;
            if (source == null || source.width <= 16)
            {
                return new FrameResult { Ok = false, Reason = "カメラの えいぞうが まだ きていません" };
            }

            var region = HumanMask.CropRegion((float)source.width / source.height);
            var outWidth = Mathf.RoundToInt(OutHeight * HumanMask.Aspect);
            var rt = RenderTexture.GetTemporary(outWidth, OutHeight, 0);
            var before = RenderTexture.active;
            try
            {
                // 映像の、枠の場所だけを取り出す
                Graphics.Blit(source, rt, new Vector2(region.width, region.height), new Vector2(region.xMin, region.yMin));
                RenderTexture.active = rt;
                var tex = new Texture2D(outWidth, OutHeight, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, outWidth, OutHeight), 0, 0);
                tex.Apply();
                var pixels = tex.GetPixels32();
                Destroy(tex);
                return new FrameResult { Ok = true, Reason = "", Pixels = pixels, Width = outWidth, Height = OutHeight };
            }
            finally
            {
                RenderTexture.active = before;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        /// <summary>
        /// 人形を置いた写真（いまの映像の枠の場所）を撮り、先に撮った背景との差から、人形を取り出す。
        /// 背景と大きさが合わないとき、映像がまだのときは、失敗の結果を返す。
        /// </summary>
        public CutoutResult TakeCutout(FrameResult background)
        {
            var photo = CaptureFrame();
            if (!photo.Ok)
            {
                return new CutoutResult { Ok = false, Reason = photo.Reason };
            }
            if (!background.Ok || background.Width != photo.Width || background.Height != photo.Height)
            {
                return new CutoutResult { Ok = false, Reason = "はいけいの しゃしんが ありません。はいけいから とりなおしてね" };
            }
            return BackgroundSubtraction.Cut(background.Pixels, photo.Pixels, photo.Width, photo.Height);
        }
    }
}
