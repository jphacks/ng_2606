using System.Collections;
using DeskStage.AR;
using UnityEngine;

namespace DeskStage.Merge
{
    /// <summary>
    /// 人形を、人型の枠（HumanMask）に合わせて撮り、枠の形に切り抜く（キャラクターを写真で作る）。
    /// カメラ（WebCamTexture）を開き、端末の向きに合わせて起こした映像を Preview に出す。
    /// TakeFramed で、今の映像の、枠を置く場所（HumanMask.CropRegion）を撮り、枠の外を透明にする。
    /// 背景を自動で切り抜く方法（CutoutMath）は、うまく切れなかったので、使わない。
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

        /// <summary>いまの映像の、枠の場所を撮って、枠の外を透明にする。映像がまだなら、失敗の結果を返す。</summary>
        public CutoutResult TakeFramed()
        {
            var source = Preview;
            if (source == null || source.width <= 16)
            {
                return new CutoutResult { Ok = false, Reason = "カメラの えいぞうが まだ きていません" };
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
                var pixels = HumanMask.Apply(tex.GetPixels32(), outWidth, OutHeight);
                Destroy(tex);
                return new CutoutResult { Ok = true, Reason = "", Pixels = pixels, Width = outWidth, Height = OutHeight };
            }
            finally
            {
                RenderTexture.active = before;
                RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}
