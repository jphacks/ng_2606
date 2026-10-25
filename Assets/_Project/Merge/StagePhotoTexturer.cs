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
    /// 写真を貼るのは、箱だけ。机の板と地面は、写真ではなく、きれいな色にする：
    /// ・地面（PhotoGround）：机の板の外側。きれいな単色の大きな板を、
    ///   「落ちた」と判定する高さ（fallDepth）に敷く。机のふちから落ちると、この地面に着く
    ///   （1枚目の写真を机の外へのばす方法は、AddGround に残してあるが、使っていない）
    /// ・背景（PhotoBackdrop）：背景専用に撮った写真（BackdropStore。一覧の「はいけいを とる」で撮る）を、ぼかして、
    ///   ステージの周りの壁（円筒）の内側に貼る。背景の写真が無ければ、背景は出さない
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

        // 机の絵から、物の写真を消すときの、場所の広げ方（写真の割合）：左右に SideMargin、下へ（上面の高さ × SideExtendRatio）
        const float SideMargin = 0.02f;
        const float SideExtendRatio = 0.8f;

        // 地面：机の何倍の広さにするか（机の中心から、各方向に）。写真は、机の四隅から外へのばして切り出す。
        // 小さな絵（GroundTextureSide 画素）を、なめらかに引き伸ばして貼る：のばした部分の筋が、ぼやけて目立たなくなる
        const float GroundExtent = 3f;
        const int GroundTextureSide = 64;
        const float GroundDarken = 0.85f;
        // 地面の色（きれいなクリーム色。背景のプリセットと合う、やわらかい色）
        static readonly Color PlainGroundColor = new Color(0.94f, 0.89f, 0.78f, 1f);

        // 背景：ステージの周りの壁（円筒）の、半径（m）の下限と、高さ（m）。写真は、読み取れる程度の大きさ（BackdropTargetWidth 画素幅）にぼかして貼る。
        // 壁の周りの長さに合わせて、写真を横に何回か、鏡写しで並べる
        const float BackdropMinRadius = 2f;
        const float BackdropHeight = 3.2f;
        const float BackdropBottom = -1.0f;
        const int BackdropTargetWidth = 256;
        const float BackdropDarken = 0.95f;

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

            // 机の板と地面は、写真を貼らない（きれいな色）。写真を貼るのは、箱だけ
            var desk = root.Find("Desk");
            if (desk != null)
            {
                AddPlainGround(root, desk);
            }

            var changed = false;

            // 箱：撮影の 1 枚目の写真があるときだけ（保存したステージを読み込んだときは、写真が無い）
            if (photoPixels != null)
            {
                foreach (var pair in StagePhotoRegions.TopFaces)
                {
                    var box = root.Find(pair.Key);
                    if (box != null)
                    {
                        AddPhoto(box, pair.Key, pair.Value, true, photoPixels);
                        changed |= DetectGimmick(box, pair.Key, pair.Value);
                    }
                }
                if (changed)
                {
                    GimmicksChanged?.Invoke();
                }
            }

            // 背景：背景専用の写真があれば、いつでも貼る
            MakeBackdropPixels(BackdropStore.AtDefaultPlace().Load());
            AddBackdrop(root);
        }

        /// <summary>
        /// 1枚目の写真の中の、物のある場所（写真の割合）。各箱の上面の四隅を囲む長方形を、少し広げ、下へのばしたもの
        /// （斜めに撮った写真では、上面の下に、物の側面が写っているため）。
        /// </summary>
        static List<Rect> ObjectRegions()
        {
            var regions = new List<Rect>();
            foreach (var quad in StagePhotoRegions.TopFaces.Values)
            {
                float xMin = 1f, xMax = 0f, yMin = 1f, yMax = 0f;
                foreach (var p in quad)
                {
                    xMin = Mathf.Min(xMin, p.x);
                    xMax = Mathf.Max(xMax, p.x);
                    yMin = Mathf.Min(yMin, p.y);
                    yMax = Mathf.Max(yMax, p.y);
                }
                var height = yMax - yMin;
                regions.Add(Rect.MinMaxRect(
                    Mathf.Clamp01(xMin - SideMargin), Mathf.Clamp01(yMin - 0.01f),
                    Mathf.Clamp01(xMax + SideMargin), Mathf.Clamp01(yMax + height * SideExtendRatio + 0.02f)));
            }
            return regions;
        }

        /// <summary>箱のしかけ（水・炎・回復）が、写真から決まった（変わった）。GimmickSystem が受けて、見た目を作り直す。</summary>
        public static event System.Action GimmicksChanged;

        // 上面の写真の、ふち（影やテープ）を除いた内側を小さく切り出して、一面、青・赤・緑のどれかなら、その箱のしかけにする
        bool DetectGimmick(Transform box, string id, Vector2[] quad)
        {
            const int Side = 24;
            const float Inner0 = 0.15f;
            const float Inner1 = 0.85f;
            var scale = box.localScale;
            var ordered = PhotoTextureMath.OrderForFace(quad, scale.x, scale.z, box.localEulerAngles.y);
            var pixels = PhotoTextureMath.CropRange(photoPixels, photoWidth, photoHeight, ordered, Side, Side, Inner0, Inner1, Inner0, Inner1);
            var gimmick = GimmickDetector.Detect(pixels);
            var changed = false;
            foreach (var data in ScanContext.WorkingBoxes)
            {
                if (data.id == id && (data.gimmick ?? "") != gimmick)
                {
                    data.gimmick = gimmick;
                    changed = true;
                }
            }
            var stage = StageContext.CurrentStage;
            if (stage != null && stage.boxes != null)
            {
                foreach (var data in stage.boxes)
                {
                    if (data.id == id && (data.gimmick ?? "") != gimmick)
                    {
                        data.gimmick = gimmick;
                        changed = true;
                    }
                }
            }
            if (gimmick.Length > 0)
            {
                Debug.Log($"[Merge] {id} の上面は一面 {gimmick} の色：しかけにします");
            }
            return changed;
        }

        void AddPhoto(Transform target, string boxId, Vector2[] quad, bool paintSides, Color32[] source)
        {
            var scale = target.localScale;
            var ordered = PhotoTextureMath.OrderForFace(quad, scale.x, scale.z, target.localEulerAngles.y);

            // 面の縦横の比に合わせた大きさで切り出す
            var wide = scale.x >= scale.z;
            var ratio = Mathf.Clamp(wide ? scale.z / Mathf.Max(scale.x, 1e-4f) : scale.x / Mathf.Max(scale.z, 1e-4f), 0.05f, 1f);
            var outWidth = wide ? MaxTextureSide : Mathf.Max(16, Mathf.RoundToInt(MaxTextureSide * ratio));
            var outHeight = wide ? Mathf.Max(16, Mathf.RoundToInt(MaxTextureSide * ratio)) : MaxTextureSide;

            var pixels = PhotoTextureMath.Crop(source, photoWidth, photoHeight, ordered, outWidth, outHeight);
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

        /// <summary>机の外側の地面：きれいな単色の大きな板を、落ちる高さに敷く（机のふちから落ちると、ここに着く）。</summary>
        void AddPlainGround(Transform root, Transform desk)
        {
            var scale = desk.localScale;
            var texture = new Texture2D(2, 2, TextureFormat.RGB24, false) { wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(new Color32[] { new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255) });
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
            ground.GetComponent<Renderer>().sharedMaterial = NewMaterial(texture, PlainGroundColor);
            extras.Add(ground);
        }

        /// <summary>机の外側の地面：1枚目の写真を、机の四隅から外へのばして貼った大きな板を、落ちる高さに敷く。</summary>
        void AddGround(Transform root, Transform desk, Vector2[] deskQuad, Color32[] source)
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
            var pixels = PhotoTextureMath.CropRange(source, photoWidth, photoHeight, ordered, outWidth, outHeight, min, max, min, max);
            var texture = new Texture2D(outWidth, outHeight, TextureFormat.RGB24, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear, // 小さな絵を、なめらかに引き伸ばす
            };
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

        /// <summary>背景：背景専用の写真を、ぼかして、ステージの周りの壁（円筒）の内側に貼る。</summary>
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

            // 壁の半径：机が大きいときは、広げる
            var stage = StageContext.CurrentStage;
            var deskSize = stage != null && stage.desk != null ? Mathf.Max(stage.desk.width, stage.desk.depth) : 0.6f;
            var radius = Mathf.Max(BackdropMinRadius, deskSize * 1.2f + 1f);

            // Unity の円筒は、直径 1、高さ 2。半径 radius、高さ BackdropHeight にする
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wall.name = BackdropName;
            RemoveCollider(wall);
            wall.transform.SetParent(root, false);
            wall.transform.localPosition = new Vector3(0f, BackdropBottom + BackdropHeight / 2f, 0f);
            wall.transform.localScale = new Vector3(radius * 2f, BackdropHeight / 2f, radius * 2f);

            var material = NewMaterial(texture, new Color(BackdropDarken, BackdropDarken, BackdropDarken, 1f));
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Front); // 内側を見せる
            // 写真の縦横の比が、壁の上で保たれるように、周りの長さ ÷ 高さ ÷ 写真の縦横の比 だけ、横に並べる（鏡写し）
            var photoAspect = backdropHeight > 0 ? (float)backdropWidth / backdropHeight : 1.33f;
            var repeats = Mathf.Max(1f, Mathf.Round(2f * Mathf.PI * radius / BackdropHeight / photoAspect));
            material.mainTextureScale = new Vector2(repeats, 1f);
            wall.GetComponent<Renderer>().sharedMaterial = material;
            extras.Add(wall);
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
