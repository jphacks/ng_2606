using System.Collections.Generic;
using DeskStage.AR;
using DeskStage.Shared;
using DeskStage.Stage;
using UnityEngine;

namespace DeskStage.Merge
{
    /// <summary>
    /// 3D のステージ（机の板と箱）に、撮った本物の写真を貼る（合流のつなぎ）。
    /// ・1枚目（真上）の写真は、A の PhotoCapture が撮ったときに受け取る
    /// ・写真の中の机と箱の上面の位置は、B の StagePhotoRegions（答えの deskInPhoto1、topFaceInPhoto1）
    /// ・StageBuilt で、机の板に机の絵、箱の上面にその部分の写真を貼る。側面は、上面の平均の色で塗る
    /// 貼る物は、机の板と箱の子（PhotoTop、PhotoSides）。親の素材や当たり判定は変えない。
    /// 位置が分からない箱（補助で足した箱など）は、そのまま（D の見た目）。選んでいる箱は、D の強調が見えるよう、写真を外す。
    /// </summary>
    public class StagePhotoTexturer : MonoBehaviour
    {
        const string TopName = "PhotoTop";
        const string SidesName = "PhotoSides";
        const int MaxTextureSide = 512;

        // 同じ高さで重なって、ちらつかないように、ほんの少し浮かせる（m）
        const float Lift = 0.0005f;

        // 側面は、上面の平均の色を、少し暗くして、上面との違いが分かるようにする
        const float SideDarken = 0.8f;

        class Overlay
        {
            public string BoxId;
            public GameObject Top;
            public GameObject Sides;
        }

        [SerializeField] Material photoMaterial;

        readonly List<Overlay> overlays = new List<Overlay>();
        readonly List<Object> created = new List<Object>();
        PhotoCapture capture;
        Color32[] photoPixels;
        int photoWidth;
        int photoHeight;

        void OnEnable()
        {
            StageContext.StageBuilt += Apply;
            UiRequests.Rescan += OnRescan;
        }

        void OnDisable()
        {
            StageContext.StageBuilt -= Apply;
            UiRequests.Rescan -= OnRescan;
            if (capture != null)
            {
                capture.PhotosReady -= OnPhotos;
            }
            Release();
        }

        void Update()
        {
            if (capture == null)
            {
                capture = FindFirstObjectByType<PhotoCapture>();
                if (capture != null)
                {
                    capture.PhotosReady += OnPhotos;
                }
            }

            // 選んでいる箱は、D の強調（黄色のふち）が見えるよう、写真を外す
            var selected = ScanContext.SelectedBoxId;
            foreach (var o in overlays)
            {
                var show = o.BoxId != selected;
                if (o.Top != null && o.Top.activeSelf != show) o.Top.SetActive(show);
                if (o.Sides != null && o.Sides.activeSelf != show) o.Sides.SetActive(show);
            }
        }

        void OnPhotos(byte[] top, byte[] side)
        {
            StagePhotoRegions.TopJpeg = top;
            var tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (tex.LoadImage(top))
            {
                photoWidth = tex.width;
                photoHeight = tex.height;
                photoPixels = tex.GetPixels32();
            }
            else
            {
                photoPixels = null;
                Debug.LogWarning("[Merge] 1枚目の写真を読めませんでした。ステージに写真を貼りません");
            }
            Destroy(tex);
        }

        void OnRescan()
        {
            photoPixels = null;
            Release();
        }

        void Apply()
        {
            Release();
            var root = StageContext.StageRoot;
            if (root == null || photoPixels == null || photoMaterial == null)
            {
                return;
            }

            var desk = root.Find("Desk");
            if (desk != null && StagePhotoRegions.DeskQuad != null)
            {
                AddPhoto(desk, "", StagePhotoRegions.DeskQuad, false);
            }
            foreach (var pair in StagePhotoRegions.TopFaces)
            {
                var box = root.Find(pair.Key);
                if (box != null)
                {
                    AddPhoto(box, pair.Key, pair.Value, true);
                }
            }
        }

        void AddPhoto(Transform target, string boxId, Vector2[] quad, bool paintSides)
        {
            var scale = target.localScale;
            var ordered = PhotoTextureMath.OrderForFace(quad, scale.x, scale.z, target.localEulerAngles.y);

            // 面の縦横の比に合わせた大きさで切り出す
            var wide = scale.x >= scale.z;
            var ratio = Mathf.Clamp(wide ? scale.z / Mathf.Max(scale.x, 1e-4f) : scale.x / Mathf.Max(scale.z, 1e-4f), 0.05f, 1f);
            var outWidth = wide ? MaxTextureSide : Mathf.Max(16, Mathf.RoundToInt(MaxTextureSide * ratio));
            var outHeight = wide ? Mathf.Max(16, Mathf.RoundToInt(MaxTextureSide * ratio)) : MaxTextureSide;

            var pixels = PhotoTextureMath.Crop(photoPixels, photoWidth, photoHeight, ordered, outWidth, outHeight);
            var texture = new Texture2D(outWidth, outHeight, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply();
            created.Add(texture);

            var overlay = new Overlay { BoxId = boxId };
            var lift = Lift / Mathf.Max(scale.y, 1e-3f);
            var topY = 0.5f;

            if (paintSides)
            {
                var average = (Color)PhotoTextureMath.Average(pixels) * SideDarken;
                average.a = 1f;
                var sides = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sides.name = SidesName;
                RemoveCollider(sides);
                sides.transform.SetParent(target, false);
                sides.transform.localPosition = Vector3.zero;
                sides.transform.localScale = new Vector3(1.004f, 1.002f, 1.004f);
                sides.layer = target.gameObject.layer;
                var sideMaterial = NewMaterial(null, average);
                sides.GetComponent<Renderer>().sharedMaterial = sideMaterial;
                overlay.Sides = sides;
                topY = 0.5f * 1.002f;
            }

            var top = GameObject.CreatePrimitive(PrimitiveType.Quad);
            top.name = TopName;
            RemoveCollider(top);
            top.transform.SetParent(target, false);
            top.transform.localPosition = new Vector3(0f, topY + lift, 0f);
            top.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            top.transform.localScale = Vector3.one;
            top.layer = target.gameObject.layer;
            top.GetComponent<Renderer>().sharedMaterial = NewMaterial(texture, Color.white);
            overlay.Top = top;

            overlays.Add(overlay);
        }

        Material NewMaterial(Texture2D texture, Color color)
        {
            var material = new Material(photoMaterial);
            if (texture != null)
            {
                material.SetTexture("_BaseMap", texture);
                material.mainTexture = texture;
            }
            material.SetColor("_BaseColor", color);
            material.color = color;
            created.Add(material);
            return material;
        }

        static void RemoveCollider(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        void Release()
        {
            foreach (var o in overlays)
            {
                if (o.Top != null) Destroy(o.Top);
                if (o.Sides != null) Destroy(o.Sides);
            }
            overlays.Clear();
            foreach (var obj in created)
            {
                if (obj != null) Destroy(obj);
            }
            created.Clear();
        }
    }
}
