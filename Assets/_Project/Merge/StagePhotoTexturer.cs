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
    ///
    /// 地面と背景も、本物の写真にする：
    /// ・地面（PhotoGround）：机の板の外側。1枚目（真上）の写真を、机の四隅から外へのばして貼った大きな板を、
    ///   「落ちた」と判定する高さ（fallDepth）に敷く。机のふちから落ちると、この地面に着く
    /// ・背景（PhotoBackdrop）：背景専用に撮った写真（BackdropStore。一覧の「はいけいを とる」で撮る）を、ぼかして、
    ///   ステージの周りの大きなドームの内側に貼る。背景の写真が無ければ、背景は出さない
    ///   背景は、撮影の写真が無くても（保存したステージを読み込んだときも）貼る
    /// </summary>
    public class StagePhotoTexturer : MonoBehaviour
    {
        const string TopName = "PhotoTop";
        const string SidesName = "PhotoSides";
        const string GroundName = "PhotoGround";
        const string BackdropName = "PhotoBackdrop";
        const int MaxTextureSide = 512;

        // 同じ高さで重なって、ちらつかないように、ほんの少し浮かせる（m）
        const float Lift = 0.0005f;

        // 側面は、上面の平均の色を、少し暗くして、上面との違いが分かるようにする
        const float SideDarken = 0.8f;

        // 地面：机の何倍の広さにするか（机の中心から、各方向に）。写真は、机の四隅から外へのばして切り出す
        const float GroundExtent = 3f;
        const int GroundTextureSide = 512;
        const float GroundDarken = 0.85f;

        // 背景：ドームの半径（m）。カメラの遠いほうの面（20m）より内側。ぼかすため、写真は小さくして使う
        const float BackdropRadius = 8f;
        const int BackdropTargetWidth = 48;
        const float BackdropDarken = 0.9f;

        class Overlay
        {
            public string BoxId;
            public GameObject Top;
            public GameObject Sides;
        }

        [SerializeField] Material photoMaterial;

        readonly List<Overlay> overlays = new List<Overlay>();
        readonly List<Object> created = new List<Object>();
        readonly List<GameObject> extras = new List<GameObject>();
        PhotoCapture capture;
        Color32[] photoPixels;
        int photoWidth;
        int photoHeight;

        // 背景専用の写真を、ぼかすために小さくした物（背景に使う）
        Color32[] backdropPixels;
        int backdropWidth;
        int backdropHeight;

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

        /// <summary>背景専用の写真を読み、ミップマップの小さい段（箱型のぼかしがかかった物）から、背景用の小さな画像を取る。</summary>
        void MakeBackdropPixels(byte[] jpeg)
        {
            backdropPixels = null;
            if (jpeg == null)
            {
                return;
            }
            var tex = new Texture2D(2, 2, TextureFormat.RGB24, true);
            if (tex.LoadImage(jpeg))
            {
                var level = 0;
                while ((tex.width >> (level + 1)) >= BackdropTargetWidth && level + 1 < tex.mipmapCount)
                {
                    level++;
                }
                backdropWidth = Mathf.Max(1, tex.width >> level);
                backdropHeight = Mathf.Max(1, tex.height >> level);
                backdropPixels = tex.GetPixels32(level);
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
            if (root == null || photoMaterial == null)
            {
                return;
            }

            // 机・地面・箱：撮影の 1 枚目の写真があるときだけ（保存したステージを読み込んだときは、写真が無い）
            if (photoPixels != null)
            {
                var desk = root.Find("Desk");
                if (desk != null && StagePhotoRegions.DeskQuad != null)
                {
                    AddPhoto(desk, "", StagePhotoRegions.DeskQuad, false);
                    AddGround(root, desk, StagePhotoRegions.DeskQuad);
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

            // 背景：背景専用の写真があれば、いつでも貼る
            MakeBackdropPixels(BackdropStore.AtDefaultPlace().Load());
            AddBackdrop(root);
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

        /// <summary>机の外側の地面：1枚目の写真を、机の四隅から外へのばして貼った大きな板を、落ちる高さに敷く。</summary>
        void AddGround(Transform root, Transform desk, Vector2[] deskQuad)
        {
            var scale = desk.localScale;
            var ordered = PhotoTextureMath.OrderForFace(deskQuad, scale.x, scale.z, 0f);

            var wide = scale.x >= scale.z;
            var ratio = Mathf.Clamp(wide ? scale.z / Mathf.Max(scale.x, 1e-4f) : scale.x / Mathf.Max(scale.z, 1e-4f), 0.1f, 1f);
            var outWidth = wide ? GroundTextureSide : Mathf.Max(64, Mathf.RoundToInt(GroundTextureSide * ratio));
            var outHeight = wide ? Mathf.Max(64, Mathf.RoundToInt(GroundTextureSide * ratio)) : GroundTextureSide;

            // 机の中心から各方向に GroundExtent 倍：机の左端が u = 0、右端が u = 1 なので、0.5 ± GroundExtent の範囲
            var min = 0.5f - GroundExtent;
            var max = 0.5f + GroundExtent;
            var pixels = PhotoTextureMath.CropRange(photoPixels, photoWidth, photoHeight, ordered, outWidth, outHeight, min, max, min, max);
            var texture = new Texture2D(outWidth, outHeight, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply();
            created.Add(texture);

            var fallDepth = StageContext.CurrentStage != null ? StageContext.CurrentStage.fallDepth : 0.10f;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ground.name = GroundName;
            RemoveCollider(ground);
            ground.transform.SetParent(root, false);
            ground.transform.localPosition = new Vector3(0f, -fallDepth, 0f);
            ground.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ground.transform.localScale = new Vector3(scale.x * GroundExtent * 2f, scale.z * GroundExtent * 2f, 1f);
            ground.GetComponent<Renderer>().sharedMaterial = NewMaterial(texture, new Color(GroundDarken, GroundDarken, GroundDarken, 1f));
            extras.Add(ground);
        }

        /// <summary>背景：2枚目の写真を、ぼかして、ステージの周りの大きなドームの内側に貼る。</summary>
        void AddBackdrop(Transform root)
        {
            if (backdropPixels == null)
            {
                return;
            }
            var texture = new Texture2D(backdropWidth, backdropHeight, TextureFormat.RGB24, false)
            {
                wrapMode = TextureWrapMode.Mirror,
                filterMode = FilterMode.Bilinear,
            };
            texture.SetPixels32(backdropPixels);
            texture.Apply();
            created.Add(texture);

            var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.name = BackdropName;
            RemoveCollider(dome);
            dome.transform.SetParent(root, false);
            dome.transform.localPosition = Vector3.zero;
            dome.transform.localScale = Vector3.one * (BackdropRadius * 2f);

            var material = NewMaterial(texture, new Color(BackdropDarken, BackdropDarken, BackdropDarken, 1f));
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Front); // 内側を見せる
            material.mainTextureScale = new Vector2(2f, 1f);                           // 写真を横に 2 回、鏡写しで並べて 1 周にする
            dome.GetComponent<Renderer>().sharedMaterial = material;
            extras.Add(dome);
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
            foreach (var extra in extras)
            {
                if (extra != null) Destroy(extra);
            }
            extras.Clear();
            foreach (var obj in created)
            {
                if (obj != null) Destroy(obj);
            }
            created.Clear();
        }
    }
}
