using System;
using System.Collections;
using System.IO;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.AR
{
    /// <summary>
    /// カメラを開き、机の写真を2枚撮る（A の Step 22、00_overview.md 9.2）。
    /// ・StartScan：カメラを開き、映像を ScanContext.CameraPreview に入れる。状態は FindingDesk
    /// ・TakePhoto：1枚目は真上、2枚目は斜め。1枚目のあとは Scanning。2枚そろったら PhotosReady を出す
    /// ・Rescan：撮った写真を捨てて、カメラを開き直す
    /// 写真は長い辺を ScanSettings の上限まで縮め、JPEG にする。
    /// エディタで useTestPhotos が on のときは、カメラの代わりに testPhotoFolder の top.jpg と side.jpg を「撮った」ことにする。
    /// </summary>
    public class PhotoCapture : MonoBehaviour
    {
        [SerializeField] ScanSettings settings;

        [Tooltip("エディタだけ：カメラの代わりに、保存した写真（testPhotoFolder の top.jpg と side.jpg）を使う")]
        [SerializeField] bool useTestPhotos = true;

        /// <summary>2枚そろった（真上、斜め）。A の写真の確認（Step 23）と送信（Step 25）が受ける。</summary>
        public event Action<byte[], byte[]> PhotosReady;

        /// <summary>撮った写真（JPEG）。まだなら null。</summary>
        public byte[] TopJpeg { get; private set; }
        public byte[] SideJpeg { get; private set; }

        /// <summary>開発中だけ：最後に保存した写真の場所（ログと確認用）。</summary>
        public string LastSavedFolder { get; private set; } = "";

        WebCamTexture webcam;
        Texture2D testTop;
        Texture2D testSide;

        bool UseTestPhotos => useTestPhotos && Application.isEditor;
        int MaxLongSide => settings != null ? settings.photoMaxLongSide : 1568;
        int JpegQuality => settings != null ? settings.photoJpegQuality : 85;

        void OnEnable()
        {
            UiRequests.StartScan += Begin;
            UiRequests.TakePhoto += OnTakePhoto;
            UiRequests.Rescan += Begin;
        }

        void OnDisable()
        {
            UiRequests.StartScan -= Begin;
            UiRequests.TakePhoto -= OnTakePhoto;
            UiRequests.Rescan -= Begin;
            StopCamera();
        }

        void Begin()
        {
            TopJpeg = null;
            SideJpeg = null;
            StopAllCoroutines();
            StartCoroutine(OpenCamera());
        }

        IEnumerator OpenCamera()
        {
            StageContext.SetScanStatus(ScanStatus.Starting);

            if (UseTestPhotos)
            {
                if (!LoadTestPhotos())
                {
                    StageContext.SetScanStatus(ScanStatus.Lost);
                    yield break;
                }
                ScanContext.CameraPreview = testTop;
                StageContext.SetScanStatus(ScanStatus.FindingDesk);
                yield break;
            }

            // iPhone ではカメラの許可を先に聞く（Camera Usage Description は全体 Step 1 で入れてある）
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
            if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
            {
                Debug.LogWarning("[Photo] カメラが許可されていません");
                StageContext.SetScanStatus(ScanStatus.Lost);
                yield break;
            }

            var device = FindBackCamera();
            if (device == null)
            {
                Debug.LogWarning("[Photo] カメラが見つかりません");
                StageContext.SetScanStatus(ScanStatus.Lost);
                yield break;
            }

            StopCamera();
            webcam = new WebCamTexture(device.Value.name, 1920, 1440);
            webcam.Play();
            ScanContext.CameraPreview = webcam;
            StageContext.SetScanStatus(ScanStatus.FindingDesk);
        }

        void OnTakePhoto()
        {
            if (ScanContext.CameraPreview == null || SideJpeg != null)
            {
                return;
            }

            var source = UseTestPhotos ? (TopJpeg == null ? testTop : testSide) : (Texture)webcam;
            var jpeg = Encode(source);

            if (TopJpeg == null)
            {
                TopJpeg = jpeg;
                if (UseTestPhotos)
                {
                    ScanContext.CameraPreview = testSide;
                }
                StageContext.SetScanStatus(ScanStatus.Scanning);
                return;
            }

            SideJpeg = jpeg;
            StopCamera();
            SaveForDebug();
            PhotosReady?.Invoke(TopJpeg, SideJpeg);
        }

        /// <summary>今の映像を、長い辺を上限まで縮めて JPEG にする。</summary>
        byte[] Encode(Texture source)
        {
            var size = PhotoMath.FitLongSide(source.width, source.height, MaxLongSide);
            var rt = RenderTexture.GetTemporary(size.x, size.y, 0);
            var before = RenderTexture.active;
            try
            {
                Graphics.Blit(source, rt);
                RenderTexture.active = rt;
                var tex = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
                tex.Apply();
                var jpeg = tex.EncodeToJPG(JpegQuality);
                Destroy(tex);
                return jpeg;
            }
            finally
            {
                RenderTexture.active = before;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        bool LoadTestPhotos()
        {
            var folder = Path.Combine(ProjectRoot(), settings != null ? settings.testPhotoFolder : "UserSettings/TestPhotos");
            var topPath = Path.Combine(folder, "top.jpg");
            var sidePath = Path.Combine(folder, "side.jpg");
            if (!File.Exists(topPath) || !File.Exists(sidePath))
            {
                Debug.LogWarning($"[Photo] 試しの写真がありません。{folder} に top.jpg と side.jpg を置いてください");
                return false;
            }
            testTop = LoadTexture(topPath, testTop);
            testSide = LoadTexture(sidePath, testSide);
            return true;
        }

        static Texture2D LoadTexture(string path, Texture2D reuse)
        {
            var tex = reuse != null ? reuse : new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(path));
            return tex;
        }

        /// <summary>開発中だけ、撮った写真を一時フォルダに保存して、場所をログに出す（00_overview.md 9.5：本番では残さない）。</summary>
        void SaveForDebug()
        {
            if (!Debug.isDebugBuild)
            {
                return;
            }
            var folder = Path.Combine(Application.temporaryCachePath, "ScanPhotos");
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, "top.jpg"), TopJpeg);
            File.WriteAllBytes(Path.Combine(folder, "side.jpg"), SideJpeg);
            LastSavedFolder = folder;
            Debug.Log($"[Photo] 2枚撮りました（真上 {TopJpeg.Length / 1024} KB、斜め {SideJpeg.Length / 1024} KB）。保存した場所：{folder}");
        }

        void StopCamera()
        {
            if (webcam != null)
            {
                webcam.Stop();
                Destroy(webcam);
                webcam = null;
            }
            ScanContext.CameraPreview = null;
        }

        static WebCamDevice? FindBackCamera()
        {
            var devices = WebCamTexture.devices;
            foreach (var d in devices)
            {
                if (!d.isFrontFacing)
                {
                    return d;
                }
            }
            return devices.Length > 0 ? devices[0] : (WebCamDevice?)null;
        }

        static string ProjectRoot() => Path.GetDirectoryName(Application.dataPath);
    }
}
