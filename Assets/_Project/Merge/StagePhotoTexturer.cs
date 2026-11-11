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
    /// ・上面と側面は、光の計算をする素材（Lit）にする（2026-11-11）：光の当たる向きで明るさが変わり、影も受ける。
    ///   前は、光の計算をしない素材で、側面を作り物で暗くしていたので、立体に見えにくく、影も出なかった。
    ///   写真と背景を貼ったあとに、光を整える（StageLighting：光源を 1 つに、環境の光を背景の色に）
    /// 貼る物は、机の板と箱の子（PhotoTop、PhotoSides）。親の素材や当たり判定は変えない。
    /// 位置が分からない箱（補助で足した箱など）は、そのまま（D の見た目）。選んでいる箱は、D の強調が見えるよう、写真を外す。
    ///
    /// 写真を貼るのは、箱だけ。机の板は、写真ではなく、きれいな色にする：
    /// ・地面：机の外側には、地面を敷かない（2026-11-10：背景・机の土台・ステージだけを見せる。
    ///   地面と机が二重の土台に見えて、見栄えが悪かったため）。机のふちから落ちると、背景の中へ落ちる
    ///   （1枚目の写真を机の外へのばす方法は、AddGround に残してあるが、使っていない）
    /// ・背景（PhotoBackdrop）：背景専用に撮った写真（BackdropStore。一覧の「はいけいを とる」で撮る）を、ぼかして、
    ///   ステージの周りの壁（円筒）の内側に貼る。壁の底は、写真の下のふちの色でふさぐ（下をのぞいても背景が続く）。
    ///   背景の写真が無ければ、背景は出さない
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

        // 写真の面（上面と側面）の、つやの少なさ（紙や段ボールのような、つやの無い感じ）。光の計算をする素材（Lit）に使う
        const float SurfaceSmoothness = 0.12f;

        // 机の絵から、物の写真を消すときの、場所の広げ方（写真の割合）：左右に SideMargin、下へ（上面の高さ × SideExtendRatio）
        const float SideMargin = 0.02f;
        const float SideExtendRatio = 0.8f;

        // 地面：机の何倍の広さにするか（机の中心から、各方向に）。写真は、机の四隅から外へのばして切り出す。
        // 小さな絵（GroundTextureSide 画素）を、なめらかに引き伸ばして貼る：のばした部分の筋が、ぼやけて目立たなくなる
        const float GroundExtent = 3f;
        const int GroundTextureSide = 64;
        const float GroundDarken = 0.85f;
        // 地面の色（きれいなクリーム色。背景のプリセットと合う、やわらかい色）

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
            StageContext.StageLoaded += OnStageLoaded;
            UiRequests.Rescan += OnRescan;
        }

        void OnDisable()
        {
            StageContext.StageBuilt -= Apply;
            StageContext.StageLoaded -= OnStageLoaded;
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

        // 保存したステージ（や、みんなのステージ）を読み込んだ：そのステージに保存した、箱の上面の写真を貼る（Docs/specs/saved-photos）
        StageData loadedStage;
        bool useSaved;

        void OnStageLoaded(StageData stage)
        {
            loadedStage = stage;
            useSaved = true;
        }

        void OnPhotos(byte[] top, byte[] side)
        {
            useSaved = false; // 撮り直した：撮った写真から貼る
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

            // 机の板は、写真を貼らない（きれいな色）。写真を貼るのは、箱だけ。机の外に地面は敷かない

            var changed = false;

            // 箱：保存したステージなら、保存した上面の写真。撮ったばかりなら、撮影の 1 枚目の写真から切り出す
            if (useSaved)
            {
                ApplySaved(root);
            }
            else if (photoPixels != null)
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
            AddContactShadows(root);
            ApplyLighting();

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
            pixels = VisiblePixels(id, pixels, PhotoTextureMath.CropPoints(ordered, Side, Side, Inner0, Inner1, Inner0, Inner1));
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

        /// <summary>見えている所が、これより少ないときは、隠れた所も数える（隠れすぎて、見える所だけでは決められない）。</summary>
        const float MinVisibleRatio = 0.3f;

        // 上に乗った（より高い）箱の上面で隠れている所の画素を、しかけの判定から外す（2026-11-07。紫の箱にピンクの箱が乗ると、
        // 紫の上面の 85% に届かなかったため）。見えている所が少なすぎるときは、外さない
        static Color32[] VisiblePixels(string id, Color32[] pixels, Vector2[] points)
        {
            var myHeight = HeightOf(id);
            var covers = new List<Vector2[]>();
            foreach (var pair in StagePhotoRegions.TopFaces)
            {
                if (pair.Key != id && HeightOf(pair.Key) > myHeight + 0.005f)
                {
                    covers.Add(pair.Value);
                }
            }
            if (covers.Count == 0)
            {
                return pixels;
            }
            var visible = new List<Color32>(pixels.Length);
            for (var i = 0; i < pixels.Length; i++)
            {
                var hidden = false;
                foreach (var q in covers)
                {
                    if (PhotoTextureMath.InsideQuad(q, points[i]))
                    {
                        hidden = true;
                        break;
                    }
                }
                if (!hidden) visible.Add(pixels[i]);
            }
            return visible.Count >= pixels.Length * MinVisibleRatio ? visible.ToArray() : pixels;
        }

        // 上面の写真のうち、写真で、より高い箱に隠れていた所を、見えている所の平均の色で埋める（pixels を書き換える）。
        // 2026-11-10：隠した箱（ピンクの紙など）が、低い箱の上面に写り込んでいたため。色の判定（VisiblePixels）と同じ考え方
        static void FillHidden(string id, Color32[] pixels, Vector2[] points)
        {
            var myHeight = HeightOf(id);
            var covers = new List<Vector2[]>();
            foreach (var pair in StagePhotoRegions.TopFaces)
            {
                if (pair.Key != id && HeightOf(pair.Key) > myHeight + 0.005f)
                {
                    covers.Add(pair.Value);
                }
            }
            if (covers.Count == 0)
            {
                return;
            }
            var hidden = new bool[pixels.Length];
            long r = 0, g = 0, b = 0;
            var visibleCount = 0;
            for (var i = 0; i < pixels.Length; i++)
            {
                foreach (var q in covers)
                {
                    if (PhotoTextureMath.InsideQuad(q, points[i]))
                    {
                        hidden[i] = true;
                        break;
                    }
                }
                if (hidden[i]) continue;
                r += pixels[i].r;
                g += pixels[i].g;
                b += pixels[i].b;
                visibleCount++;
            }
            if (visibleCount == 0 || visibleCount == pixels.Length)
            {
                return;
            }
            var fill = new Color32((byte)(r / visibleCount), (byte)(g / visibleCount), (byte)(b / visibleCount), 255);
            for (var i = 0; i < pixels.Length; i++)
            {
                if (hidden[i]) pixels[i] = fill;
            }
        }

        // 箱の高さ（作業中の箱。無ければ 0）
        static float HeightOf(string id)
        {
            foreach (var b in ScanContext.WorkingBoxes)
            {
                if (b.id == id && b.size != null && b.size.Length > 1) return b.size[1];
            }
            return 0f;
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
            FillHidden(boxId, pixels, PhotoTextureMath.CropPoints(ordered, outWidth, outHeight, 0f, 1f, 0f, 1f));
            var texture = new Texture2D(outWidth, outHeight, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply();
            created.Add(texture);
            AddOverlay(target, boxId, texture, paintSides, PhotoTextureMath.Average(pixels));
        }

        // 保存した、箱の上面の写真を読んで貼る。名前が決まった形でない・ファイルが無い・読めない箱は、写真なし
        void ApplySaved(Transform root)
        {
            if (loadedStage == null || loadedStage.boxes == null)
            {
                return;
            }
            foreach (var b in loadedStage.boxes)
            {
                var path = StageStorage.SavedFacePath(loadedStage.id, b.texture);
                var target = path != null ? root.Find(b.id) : null;
                if (target == null || !System.IO.File.Exists(path))
                {
                    continue;
                }
                var texture = new Texture2D(2, 2, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Clamp };
                byte[] bytes;
                try
                {
                    bytes = System.IO.File.ReadAllBytes(path);
                }
                catch (System.IO.IOException)
                {
                    Destroy(texture);
                    continue;
                }
                if (!texture.LoadImage(bytes))
                {
                    Destroy(texture);
                    continue;
                }
                texture.wrapMode = TextureWrapMode.Clamp;
                created.Add(texture);
                AddOverlay(target, b.id, texture, true, PhotoTextureMath.Average(texture.GetPixels32()));
            }
        }

        // 箱の上面に写真を貼り、横を写真の平均の色（少し暗く）で塗る
        void AddOverlay(Transform target, string boxId, Texture2D texture, bool paintSides, Color32 averagePixel)
        {
            var scale = target.localScale;
            var overlay = new Overlay { BoxId = boxId };
            var lift = Lift / Mathf.Max(scale.y, 1e-3f);
            var topY = 0.5f;

            // 坂（くさび形）は、斜めの面に貼る。横の面を塗る立方体は、坂の形を隠してしまうので作らない
            var ramp = target.GetComponent<MeshCollider>() != null;
            if (ramp)
            {
                paintSides = false;
            }

            if (paintSides)
            {
                // 側面の暗さは、光の計算に任せる（光の当たらない面は、影の色になる）
                var average = (Color)averagePixel;
                average.a = 1f;
                var sides = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sides.name = SidesName;
                RemoveCollider(sides);
                sides.transform.SetParent(target, false);
                sides.transform.localPosition = Vector3.zero;
                sides.transform.localScale = new Vector3(1.004f, 1.002f, 1.004f);
                sides.layer = target.gameObject.layer;
                var sideMaterial = NewSurfaceMaterial(null, average);
                sides.GetComponent<Renderer>().sharedMaterial = sideMaterial;
                overlay.Sides = sides;
                topY = 0.5f * 1.002f;
            }

            GameObject top;
            if (ramp)
            {
                var surface = RampMesh.SurfaceLifted(lift);
                created.Add(surface);
                top = new GameObject(TopName);
                top.AddComponent<MeshFilter>().sharedMesh = surface;
                top.AddComponent<MeshRenderer>();
                top.transform.SetParent(target, false);
                top.transform.localPosition = Vector3.zero;
                top.transform.localRotation = Quaternion.identity;
                top.transform.localScale = Vector3.one;
            }
            else
            {
                top = GameObject.CreatePrimitive(PrimitiveType.Quad);
                top.name = TopName;
                RemoveCollider(top);
                top.transform.SetParent(target, false);
                top.transform.localPosition = new Vector3(0f, topY + lift, 0f);
                top.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                top.transform.localScale = Vector3.one;
            }
            top.layer = target.gameObject.layer;
            var topRenderer = top.GetComponent<Renderer>();
            topRenderer.sharedMaterial = NewSurfaceMaterial(texture, Color.white);
            // 上面の板は、すぐ下の箱が影を落とすので、自分は影を落とさない（自分の影で、まだらにならないように）
            topRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            overlay.Top = top;

            overlays.Add(overlay);
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

            // 壁の底：Unity の円筒の底のふたは、写真が放射状に伸びて見えるので、写真の下のふちの色の板でふさぐ
            var floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
            floor.name = BackdropName + "Floor";
            RemoveCollider(floor);
            floor.transform.SetParent(root, false);
            floor.transform.localPosition = new Vector3(0f, BackdropBottom + 0.01f, 0f);
            floor.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            floor.transform.localScale = new Vector3(radius * 2.2f, radius * 2.2f, 1f);
            floor.GetComponent<Renderer>().sharedMaterial = NewMaterial(null, BottomEdgeColor() * BackdropDarken);
            extras.Add(floor);
        }

        // 背景の写真の、下のふち（いちばん下の 1 行）の色の平均
        Color BottomEdgeColor()
        {
            var sum = Vector3.zero;
            for (var x = 0; x < backdropWidth; x++)
            {
                Color c = backdropPixels[x];
                sum += new Vector3(c.r, c.g, c.b);
            }
            sum /= Mathf.Max(1, backdropWidth);
            return new Color(sum.x, sum.y, sum.z, 1f);
        }

        // 写真の面（箱の上面と側面）の素材：光の計算をする素材（URP の Lit）。見つからなければ、今までの光の計算をしない素材
        Material NewSurfaceMaterial(Texture2D texture, Color color)
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
            {
                return NewMaterial(texture, color);
            }
            var material = new Material(lit);
            if (texture != null)
            {
                material.SetTexture("_BaseMap", texture);
                material.mainTexture = texture;
            }
            material.SetColor("_BaseColor", color);
            material.color = color;
            material.SetFloat("_Smoothness", SurfaceSmoothness);
            created.Add(material);
            return material;
        }

        /// <summary>物が机に接するところの、やわらかい暗がり（StageContactShadows）。StagePreview（開発用）からも呼ぶ。</summary>
        void AddContactShadows(Transform root)
        {
            StageContactShadows.Add(root, StageContext.CurrentStage, photoMaterial, created, extras);
        }

        /// <summary>光を整える（StageLighting）。背景があれば、その色を使う。StagePreview（開発用）からも呼ぶ。</summary>
        void ApplyLighting()
        {
            StageLighting.Apply(backdropPixels, backdropWidth, backdropHeight);
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
