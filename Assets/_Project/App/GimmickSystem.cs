using System;
using System.Collections.Generic;
using DeskStage.Gameplay;
using DeskStage.Merge;
using DeskStage.Shared;
using DeskStage.Stage;
using TMPro;
using UnityEngine;

namespace DeskStage.App
{
    /// <summary>
    /// 箱のしかけ（折り紙の色）。上面が一面 青・赤・緑・黄・紫・ピンク の箱に、見た目と動きをつける。
    ///  ・水（青）：水面に流れる矢印。乗ると、決まった向きに、少し流される
    ///  ・炎（赤）：燃える炎。乗るとハートが 1 つ減る（減ったあとの 1.5 秒は、続けて減らない）
    ///  ・回復（緑）：光る＋。乗っている間、1 秒ごとにハートが 1 つ回復する
    ///  ・ジャンプ台（黄）：上向きの山形。乗ると自動で、ふつうのジャンプの 2 倍の高さまで跳ね上がる
    ///  ・敵（紫）：うずまき。7 秒ごとに敵が 1 体出る（最大 5 体）。敵は出るたびに、追いかけるか歩き回るかをくじで決める。当たるとハートが 1 つ減る
    ///  ・ワープ（ピンク）：輪の模様。踏むと、机の上か箱の上の、どこかランダムな所へ飛ぶ（飛んだあとの 3 秒は、続けてワープしない）
    /// どの箱がどのしかけかは、BoxData.gimmick（撮影の写真から決まる。保存したステージにも残る）。
    /// ハートは 3 つ。0 になると GameOver を出す（画面は AppFlow が切り替える）。ゲームを始める・やり直すたびに、3 つに戻る。
    /// C のゲームの中身は変えず、キャラの位置を読み、CharacterController に流される分だけを足す。
    /// </summary>
    public class GimmickSystem : MonoBehaviour
    {
        [SerializeField] Material unlit;

        /// <summary>しかけの板に使う素材（URP の Unlit、半透明）。AppRoot が渡す。</summary>
        public void SetMaterial(Material material) => unlit = material;

        /// <summary>ハート。</summary>
        public PlayerHealth Health { get; } = new PlayerHealth();

        /// <summary>撮ったばかりのステージ（たしかめ・遊ぶ）のときは true。箱の一覧は、直したあとの物（WorkingBoxes）を見る。保存したステージは false（読み込んだ物を見る）。</summary>
        public bool UseWorkingBoxes { get; set; }

        /// <summary>ハートが減った（残りの数）。</summary>
        public event Action<int> Damaged;

        /// <summary>ハートが回復した（残りの数）。</summary>
        public event Action<int> Healed;

        /// <summary>ハートが 0 になった。</summary>
        public event Action GameOver;

        /// <summary>ワープした。</summary>
        public event Action Warped;

        /// <summary>今、机の上に出ている敵の数。</summary>
        public int EnemyCount => enemies.Count;

        /// <summary>最後にハートが減ったのが、敵に当たったからか（炎なら false）。画面の言葉を変えるのに使う。</summary>
        public bool LastDamageWasEnemy { get; private set; }

        class Visual
        {
            public string Id;
            public string Kind;
            public Transform Box;
            public Material Material;
            public Vector2 Direction;
            public int Flow;
            public bool Ramp;
            public List<Billboard> Billboards = new List<Billboard>();
        }

        class Billboard
        {
            public Transform Transform;
            public Vector2 Local;     // 箱の上面の中での位置（-0.5〜0.5）
            public float Phase;
        }

        class Enemy
        {
            public Transform Transform;
            public Vector3 Position;   // 足元（世界の座標）
            public GimmickRules.EnemyMode Mode;
            public Vector3 Direction;  // うろうろするときの向き
            public float WanderTimer;
            public Vector3 Spawn;      // 出てきた所（抜け出せないときに戻す）
            public Vector3 LastProgress;
            public float StuckTimer;   // ほとんど動けていない時間
            public float DetourTimer;  // 追いかける敵が、回り道をしている残りの時間
            public Vector3 Facing = Vector3.forward; // 顔を向ける向き（進んでいる向き）
        }

        readonly List<Visual> visuals = new List<Visual>();

        // ワープの箱の上に浮かべる番号（1, 2, 3…。たしかめの画面の「1 ⇔ 2」と同じ番号。2026-11-07）
        class WarpNumber
        {
            public Transform Label;
            public Transform Box;
            public bool Ramp;
        }

        readonly List<WarpNumber> warpLabels = new List<WarpNumber>();

        // 最後に立っていた水の坂（跳んでいる間に流すのは、その坂から跳んだときだけ。2026-11-07：
        // 下り向きの水の坂に、下の端から跳んで近づくと、乗る前から強く押し戻されて「吹っ飛ぶ」ようだったため）
        string lastWaterRampId;

        /// <summary>番号の文字の大きさ（TextMeshPro の fontSize）と、上面からの高さ（m）。</summary>
        const float WarpLabelSize = 0.8f;
        const float WarpLabelLift = 0.045f;
        readonly List<Enemy> enemies = new List<Enemy>();
        // 紫の箱ごとの、次の敵が出るまでの時間
        readonly Dictionary<string, float> spawnTimers = new Dictionary<string, float>();
        float jumpPadReadyAt;
        float warpReadyAt;
        readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();
        readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        GameFlow flow;
        float healTimer;
        bool dirty;

        /// <summary>しかけのある箱の数（種類ごと）。たしかめ画面で、見つけた数を伝えるのに使う。</summary>
        public string Summary()
        {
            var kinds = new[] { BoxData.GimmickWater, BoxData.GimmickFire, BoxData.GimmickHeal, BoxData.GimmickJumpPad, BoxData.GimmickEnemy, BoxData.GimmickWarp };
            var counts = new int[kinds.Length];
            foreach (var b in CurrentBoxes())
            {
                var i = Array.IndexOf(kinds, b.gimmick);
                if (i >= 0) counts[i]++;
            }
            var parts = new List<string>();
            for (var i = 0; i < kinds.Length; i++)
            {
                if (counts[i] > 0) parts.Add($"{GimmickRules.Name(kinds[i])} {counts[i]}");
            }
            return string.Join("　", parts);
        }

        /// <summary>水の箱（水の坂も含む）の id。たしかめの画面で、それぞれの上に「ながれの むき」のボタンを出すのに使う。</summary>
        /// <summary>ワープ（ピンク）の箱の id（Docs/specs/warp-pairs）。たしかめの画面で、それぞれの上に「1 ⇔ 2」のボタンを出すのに使う。</summary>
        public void WarpBoxIds(List<string> ids)
        {
            ids.Clear();
            foreach (var b in CurrentBoxes())
            {
                if (b.gimmick == BoxData.GimmickWarp) ids.Add(b.id);
            }
        }

        /// <summary>ワープの箱の数。1 つだけならワープしない。</summary>
        public int WarpCount => WarpPairs.Count(CurrentList());

        /// <summary>ワープの箱のボタンの文字（「1 ⇔ 2」。相手がいなければ「1 ⇔ ？」）。</summary>
        public string WarpLabel(string boxId)
        {
            var list = CurrentList();
            var numbers = WarpPairs.Numbers(list);
            if (!numbers.TryGetValue(boxId, out var mine)) return "";
            var partner = WarpPairs.Partner(list, boxId);
            return numbers.TryGetValue(partner, out var theirs)
                ? string.Format(AppText.WarpLink, mine, theirs)
                : string.Format(AppText.WarpAlone, mine);
        }

        /// <summary>ワープの箱の相手を、次のピンクに変える（作業中と読み込んだ物の両方に入れる）。</summary>
        public void CycleWarp(string boxId)
        {
            WarpPairs.Cycle(CurrentList(), boxId);
            SyncWarps();
        }

        IList<BoxData> CurrentList()
        {
            if (UseWorkingBoxes && ScanContext.WorkingBoxes.Count > 0) return ScanContext.WorkingBoxes;
            var stage = StageContext.CurrentStage;
            return stage != null && stage.boxes != null ? stage.boxes : new BoxData[0];
        }

        // ワープのつながりを整え（壊れた物を直し、相手のいない物を近い順につなぐ）、作業中の箱と読み込んだ箱の両方に、同じつながりを入れる
        void SyncWarps()
        {
            var list = CurrentList();
            WarpPairs.AutoPair(list);
            var links = new Dictionary<string, string>();
            foreach (var b in list) links[b.id] = b.warpTo ?? "";
            var others = new List<IList<BoxData>> { ScanContext.WorkingBoxes };
            var stage = StageContext.CurrentStage;
            if (stage != null && stage.boxes != null) others.Add(stage.boxes);
            foreach (var other in others)
            {
                if (ReferenceEquals(other, list)) continue;
                foreach (var b in other)
                {
                    if (links.TryGetValue(b.id, out var to)) b.warpTo = to;
                }
            }
        }

        public void WaterBoxIds(List<string> ids)
        {
            ids.Clear();
            foreach (var b in CurrentBoxes())
            {
                if (b.gimmick == BoxData.GimmickWater) ids.Add(b.id);
            }
        }

        /// <summary>この箱が水の箱（水の坂も含む）か。水の箱なら、いまの流れの向き（flow）も返す。</summary>
        public bool IsWater(string boxId, out int flow)
        {
            flow = 0;
            if (string.IsNullOrEmpty(boxId))
            {
                return false;
            }
            foreach (var b in CurrentBoxes())
            {
                if (b.id == boxId && b.gimmick == BoxData.GimmickWater)
                {
                    flow = EffectiveFlow(b);
                    return true;
                }
            }
            return false;
        }

        // 向きを選んでいない坂は、下り（-x の向き。flow = 3）に流れる。選んだら、その向き（上り向きなら、流れに乗ってのぼれる）
        static int EffectiveFlow(BoxData b) => b.ramp && b.flow == 0 ? 3 : b.flow;

        /// <summary>水の箱の流れを、次の向きにする（90 度まわる）。保存するデータ（作業中と読み込んだ物の両方）に入れ、見た目を作り直す。</summary>
        public void CycleFlow(string boxId)
        {
            if (!IsWater(boxId, out var current))
            {
                return;
            }
            var next = GimmickRules.NextFlow(boxId, current);
            foreach (var data in ScanContext.WorkingBoxes)
            {
                if (data.id == boxId) data.flow = next;
            }
            var stage = StageContext.CurrentStage;
            if (stage != null && stage.boxes != null)
            {
                foreach (var data in stage.boxes)
                {
                    if (data.id == boxId) data.flow = next;
                }
            }
            MarkDirty();
        }

        void OnEnable()
        {
            StageContext.StageBuilt += MarkDirty;
            StagePhotoTexturer.GimmicksChanged += MarkDirty;
            ScanContext.BoxesChanged += OnBoxesChanged;
            GameEvents.GameStarted += OnGameStarted;
        }

        void OnDisable()
        {
            StageContext.StageBuilt -= MarkDirty;
            StagePhotoTexturer.GimmicksChanged -= MarkDirty;
            ScanContext.BoxesChanged -= OnBoxesChanged;
            GameEvents.GameStarted -= OnGameStarted;
            Clear();
        }

        void OnBoxesChanged(string id, BoxChange change, string source) => MarkDirty();

        // 箱の見た目は、イベントのたびに作り直すと重いので、次のフレームに 1 回だけ作り直す
        void MarkDirty() => dirty = true;

        void OnGameStarted()
        {
            Health.Reset();
            healTimer = 0f;
            LastDamageWasEnemy = false;
            // はじめから（やり直し）：敵は全部消し、出るまでの時間と、ジャンプ台・ワープの待ち時間も戻す
            ClearEnemies();
            spawnTimers.Clear();
            jumpPadReadyAt = 0f;
            warpReadyAt = 0f;
        }

        IEnumerable<BoxData> CurrentBoxes()
        {
            if (UseWorkingBoxes && ScanContext.WorkingBoxes.Count > 0)
            {
                return ScanContext.WorkingBoxes;
            }
            var stage = StageContext.CurrentStage;
            return stage != null && stage.boxes != null ? stage.boxes : new BoxData[0];
        }

        // ---- 見た目 ----

        void Rebuild()
        {
            Clear();
            var root = StageContext.StageRoot;
            if (root == null)
            {
                return;
            }
            SyncWarps();
            foreach (var b in CurrentBoxes())
            {
                if (string.IsNullOrEmpty(b.gimmick))
                {
                    continue;
                }
                var box = root.Find(b.id);
                if (box == null)
                {
                    continue;
                }
                var visual = new Visual { Id = b.id, Kind = b.gimmick, Box = box, Flow = EffectiveFlow(b), Ramp = b.ramp };
                BuildPad(visual);
                BuildBillboards(visual);
                visuals.Add(visual);
            }
            BuildWarpLabels();
        }

        // ワープの箱が 2 つ以上あるときだけ、番号を浮かべる（1 つだけならワープしないので、出さない）
        void BuildWarpLabels()
        {
            var list = CurrentList();
            if (WarpPairs.Count(list) < 2)
            {
                return;
            }
            var numbers = WarpPairs.Numbers(list);
            foreach (var v in visuals)
            {
                if (v.Kind != BoxData.GimmickWarp || !numbers.TryGetValue(v.Id, out var n) || v.Box == null) continue;
                var go = new GameObject("WarpNumber");
                go.transform.SetParent(transform, false);
                go.layer = v.Box.gameObject.layer;
                var text = go.AddComponent<TextMeshPro>();
                if (Theme.Font != null) text.font = Theme.Font;
                text.text = n.ToString();
                text.fontSize = WarpLabelSize;
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Center;
                text.color = new Color(0.55f, 0.02f, 0.38f); // ピンクの模様の上でも読めるよう、濃い赤紫
                text.outlineWidth = 0.25f;
                text.outlineColor = Color.white;
                text.rectTransform.sizeDelta = new Vector2(0.12f, 0.1f);
                warpLabels.Add(new WarpNumber { Label = go.transform, Box = v.Box, Ramp = v.Ramp });
            }
        }

        void Clear()
        {
            foreach (var w in warpLabels)
            {
                if (w.Label != null) UiKit.Kill(w.Label.gameObject);
            }
            warpLabels.Clear();
            ClearEnemies();
            spawnTimers.Clear();
            foreach (var v in visuals)
            {
                foreach (var bb in v.Billboards)
                {
                    if (bb.Transform != null) UiKit.Kill(bb.Transform.gameObject);
                }
            }
            visuals.Clear();
            foreach (var o in created)
            {
                if (o != null) UiKit.Kill(o);
            }
            created.Clear();
            textures.Clear();
        }

        Material NewMaterial(Texture2D texture, Color tint)
        {
            var material = unlit != null ? new Material(unlit) : new Material(Shader.Find("Sprites/Default"));
            material.mainTexture = texture;
            material.color = tint;
            created.Add(material);
            return material;
        }

        Texture2D GetTexture(string key, Func<Texture2D> make)
        {
            if (!textures.TryGetValue(key, out var t))
            {
                t = make();
                textures[key] = t;
                created.Add(t);
            }
            return t;
        }

        // 上面に敷く、模様つきの板（子として、箱について動く）
        void BuildPad(Visual v)
        {
            var scale = v.Box.localScale;
            var dir = GimmickRules.WaterDirection(v.Id, v.Flow);
            v.Direction = dir;
            Texture2D texture;
            Color tint = Color.white;
            switch (v.Kind)
            {
                case BoxData.GimmickWater:
                    texture = GetTexture("water" + dir, () => GimmickTextures.Water(dir));
                    break;
                case BoxData.GimmickFire:
                    texture = GetTexture("fire", GimmickTextures.Fire);
                    break;
                case BoxData.GimmickJumpPad:
                    texture = GetTexture("jumppad", GimmickTextures.JumpPad);
                    break;
                case BoxData.GimmickEnemy:
                    texture = GetTexture("enemy", GimmickTextures.EnemyPad);
                    break;
                case BoxData.GimmickWarp:
                    texture = GetTexture("warp", GimmickTextures.Warp);
                    break;
                default:
                    texture = GetTexture("heal", GimmickTextures.Heal);
                    break;
            }
            GameObject pad;
            if (v.Ramp)
            {
                // 坂は、斜めの面に沿った板（箱の座標で、面から少し浮かせる）
                var surface = RampMesh.SurfaceLifted(0.0025f / Mathf.Max(scale.y, 1e-3f));
                created.Add(surface);
                pad = new GameObject("Gimmick_" + v.Kind);
                pad.AddComponent<MeshFilter>().sharedMesh = surface;
                pad.AddComponent<MeshRenderer>();
                pad.transform.SetParent(v.Box, false);
                pad.transform.localPosition = Vector3.zero;
                pad.transform.localRotation = Quaternion.identity;
                pad.transform.localScale = Vector3.one;
            }
            else
            {
                pad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                pad.name = "Gimmick_" + v.Kind;
                var collider = pad.GetComponent<Collider>();
                if (collider != null) UiKit.Kill(collider);
                pad.transform.SetParent(v.Box, false);
                pad.transform.localPosition = new Vector3(0f, 0.5f * 1.002f + 0.0025f / Mathf.Max(scale.y, 1e-3f), 0f);
                pad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                pad.transform.localScale = Vector3.one;
            }
            pad.layer = v.Box.gameObject.layer;
            v.Material = NewMaterial(texture, tint);
            // 模様の 1 つ分を、約 4cm にして、箱の大きさだけ並べる
            v.Material.mainTextureScale = new Vector2(Mathf.Max(1f, Mathf.Round(scale.x / 0.04f)), Mathf.Max(1f, Mathf.Round(scale.z / 0.04f)));
            pad.GetComponent<Renderer>().sharedMaterial = v.Material;
            pad.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // 炎と回復の「立ち上がる」飾り（いつも カメラの方を向く）
        void BuildBillboards(Visual v)
        {
            // 立ち上がる飾りは、炎と回復だけ
            if (v.Kind != BoxData.GimmickFire && v.Kind != BoxData.GimmickHeal)
            {
                return;
            }
            var scale = v.Box.localScale;
            var area = scale.x * scale.z;
            var count = v.Kind == BoxData.GimmickFire
                ? Mathf.Clamp(Mathf.RoundToInt(area / 0.0012f), 2, 7)
                : Mathf.Clamp(Mathf.RoundToInt(area / 0.004f), 1, 3);
            var texture = v.Kind == BoxData.GimmickFire
                ? GetTexture("flame", GimmickTextures.Flame)
                : GetTexture("plus", GimmickTextures.Plus);
            for (var i = 0; i < count; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = "GimmickSprite";
                var collider = go.GetComponent<Collider>();
                if (collider != null) UiKit.Kill(collider);
                go.transform.SetParent(transform, false);
                go.GetComponent<Renderer>().sharedMaterial = NewMaterial(texture, Color.white);
                go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var h1 = Hash01(v.Id, i * 2);
                var h2 = Hash01(v.Id, i * 2 + 1);
                v.Billboards.Add(new Billboard
                {
                    Transform = go.transform,
                    Local = new Vector2((h1 - 0.5f) * 0.7f, (h2 - 0.5f) * 0.7f),
                    Phase = h1 * 6.28f,
                });
            }
        }

        static float Hash01(string id, int salt)
        {
            unchecked
            {
                var h = salt * 7919 + 17;
                foreach (var ch in id) h = h * 31 + ch;
                h ^= h >> 13;
                h *= 1274126177;
                h ^= h >> 16;
                return (h & 0xFFFF) / 65535f;
            }
        }

        // ---- 毎フレーム ----

        void Update()
        {
            if (dirty)
            {
                dirty = false;
                Rebuild();
            }
            AnimatePads();
            ApplyEffects();
        }

        void LateUpdate()
        {
            AnimateBillboards();
            AnimateEnemies();
        }

        void AnimatePads()
        {
            var t = Time.unscaledTime;
            foreach (var v in visuals)
            {
                if (v.Material == null) continue;
                switch (v.Kind)
                {
                    case BoxData.GimmickWater:
                        // 矢印が、流れる向きへ動く（模様は、u = x、v = z の向きに並ぶ）
                        v.Material.mainTextureOffset = -v.Direction * (t * 0.35f % 1f);
                        break;
                    case BoxData.GimmickFire:
                        v.Material.mainTextureOffset = new Vector2(0f, -(t * 0.6f % 1f));
                        v.Material.color = Color.Lerp(new Color(1f, 0.8f, 0.7f, 1f), Color.white, 0.5f + 0.5f * Mathf.Sin(t * 11f));
                        break;
                    case BoxData.GimmickJumpPad:
                        // 山形が、上（跳ぶ向き）へ流れる
                        v.Material.mainTextureOffset = new Vector2(0f, -(t * 0.8f % 1f));
                        break;
                    case BoxData.GimmickEnemy:
                        v.Material.color = new Color(1f, 1f, 1f, 0.75f + 0.25f * Mathf.Sin(t * 4f));
                        break;
                    case BoxData.GimmickWarp:
                        // 輪が、ちかちか光る
                        v.Material.color = new Color(1f, 1f, 1f, 0.8f + 0.2f * Mathf.Sin(t * 6f));
                        break;
                    default:
                        v.Material.color = new Color(1f, 1f, 1f, 0.8f + 0.2f * Mathf.Sin(t * 3f));
                        break;
                }
            }
        }

        void AnimateBillboards()
        {
            var cam = Camera.main;
            var t = Time.unscaledTime;
            // ワープの番号：箱の上面の真ん中の、少し上。いつもカメラの方を向く
            foreach (var w in warpLabels)
            {
                if (w.Label == null || w.Box == null) continue;
                w.Label.position = w.Box.TransformPoint(new Vector3(0f, w.Ramp ? 0f : 0.5f, 0f)) + Vector3.up * WarpLabelLift;
                if (cam != null)
                {
                    w.Label.rotation = Quaternion.LookRotation(w.Label.position - cam.transform.position, cam.transform.up);
                }
            }
            foreach (var v in visuals)
            {
                if (v.Box == null) continue;
                var scale = v.Box.lossyScale;
                foreach (var b in v.Billboards)
                {
                    if (b.Transform == null) continue;
                    var surfaceY = v.Ramp ? RampMesh.SurfaceLocalY(b.Local.x) * 1.002f : 0.5f * 1.002f;
                    var local = new Vector3(b.Local.x, surfaceY, b.Local.y);
                    var basePoint = v.Box.TransformPoint(local);
                    float size;
                    float lift;
                    var renderer = b.Transform.GetComponent<Renderer>();
                    if (v.Kind == BoxData.GimmickFire)
                    {
                        // ゆらゆら大きさが変わる炎。足もとは、上面に付ける
                        var wobble = 0.5f + 0.5f * Mathf.Sin(t * 9f + b.Phase);
                        size = 0.03f + 0.014f * wobble;
                        lift = size * 0.5f + 0.003f;
                    }
                    else
                    {
                        // ふわっと上がって消える＋
                        var k = (t * 0.45f + b.Phase) % 1f;
                        size = 0.022f;
                        lift = 0.006f + k * 0.05f;
                        if (renderer != null) renderer.sharedMaterial.color = new Color(1f, 1f, 1f, Mathf.Sin(k * Mathf.PI));
                    }
                    b.Transform.position = basePoint + v.Box.up * lift;
                    b.Transform.localScale = new Vector3(size, size * (v.Kind == BoxData.GimmickFire ? 1.4f : 1f), 1f);
                    if (cam != null)
                    {
                        b.Transform.rotation = Quaternion.LookRotation(b.Transform.position - cam.transform.position, cam.transform.up);
                    }
                }
                _ = scale;
            }
        }

        // キャラが箱の上にいるときの、しかけの効き目
        void ApplyEffects()
        {
            if (flow == null)
            {
                flow = FindFirstObjectByType<GameFlow>();
            }
            if (flow == null || flow.Motor == null)
            {
                healTimer = 0f;
                ClearEnemies();
                return;
            }
            if (flow.State == GameState.WaitingStage)
            {
                // ステージ一覧へ戻った：敵は全部消す
                ClearEnemies();
            }
            // 流される分は、キャラの動きに足す（別に Move を呼ぶと、接地の判定が狂って、ジャンプできなくなる）。乗っていなければ、毎フレーム ゼロに戻す
            var push = Vector3.zero;
            if (visuals.Count == 0 || flow.State != GameState.Playing)
            {
                flow.Motor.ExternalVelocity = Vector3.zero;
                healTimer = 0f;
                return;
            }
            var motor = flow.Motor;
            var feet = motor.transform.position;
            var healing = false;
            string warpFrom = null;
            string groundedWaterRamp = null;
            UpdateEnemies(motor);
            foreach (var v in visuals)
            {
                if (v.Box == null) continue;
                var local = v.Box.InverseTransformPoint(feet);
                // 水の坂は、選んだ向きへ強く流す（下り向きなら、のぼれず下りることだけできる。上り向きなら、流れに乗ってのぼれる）。
                // 立っているときは面の上にいるときだけ。跳んでいる間は、その坂から跳んだときだけ流す（外から跳んで近づいたときは、乗るまで流さない）
                if (v.Ramp && v.Kind == BoxData.GimmickWater)
                {
                    var onRamp = GimmickRules.IsOverRamp(local, v.Box.lossyScale.y, true);
                    if (motor.IsGrounded && onRamp)
                    {
                        groundedWaterRamp = v.Id;
                    }
                    if (motor.IsGrounded && onRamp)
                    {
                        push += PushOf(v, GimmickRules.WaterRampSpeed);
                    }
                    else if (!motor.IsGrounded && lastWaterRampId == v.Id && GimmickRules.IsOverRamp(local, v.Box.lossyScale.y, false))
                    {
                        push += PushOf(v, GimmickRules.WaterRampAirSpeed); // 浮いている間は、歩く速さと同じだけ（強く運ばない）
                    }
                    continue;
                }
                if (!GimmickRules.IsOnTop(local, v.Box.lossyScale.y, v.Ramp)) continue;
                switch (v.Kind)
                {
                    case BoxData.GimmickWater:
                        push += PushOf(v, GimmickRules.WaterSpeed);
                        break;
                    case BoxData.GimmickFire:
                        TryDamage(false);
                        break;
                    case BoxData.GimmickHeal:
                        healing = true;
                        break;
                    case BoxData.GimmickJumpPad:
                        if (Time.time >= jumpPadReadyAt)
                        {
                            jumpPadReadyAt = Time.time + GimmickRules.JumpPadCooldownSeconds;
                            motor.Launch(motor.settings.jumpHeight * GimmickRules.JumpPadHeightScale);
                        }
                        break;
                    case BoxData.GimmickWarp:
                        warpFrom = v.Id;
                        break;
                }
            }
            if (motor.IsGrounded)
            {
                lastWaterRampId = groundedWaterRamp; // 立っている所が水の坂でなければ、空にする
            }
            // 水の箱が重なっていると、流れが足し合わさって速くなりすぎる。上限で抑える
            flow.Motor.ExternalVelocity = Vector3.ClampMagnitude(push, GimmickRules.MaxWaterPush);
            if (warpFrom != null && Time.time >= warpReadyAt && TryWarp(motor, warpFrom))
            {
                warpReadyAt = Time.time + GimmickRules.WarpCooldownSeconds;
                Warped?.Invoke();
            }
            if (healing)
            {
                healTimer += Time.deltaTime;
                if (healTimer >= GimmickRules.HealSeconds)
                {
                    healTimer = 0f;
                    if (Health.Heal())
                    {
                        Healed?.Invoke(Health.Hearts);
                    }
                }
            }
            else
            {
                healTimer = 0f;
            }
        }

        // ハートを 1 つ減らす（減ったあとの 1.5 秒は、続けて減らない）。0 ならゲームオーバー
        void TryDamage(bool byEnemy)
        {
            if (!Health.TryDamage(Time.time))
            {
                return;
            }
            LastDamageWasEnemy = byEnemy;
            Damaged?.Invoke(Health.Hearts);
            if (Health.IsDead) GameOver?.Invoke();
        }

        // ---- ワープ（ピンク） ----

        // つながった相手のピンクの箱の上へ飛ぶ（Docs/specs/warp-pairs。行き来できる）。相手がいなければ飛ばない（ピンクが 1 つだけのときなど）。
        // 上面の真ん中から、少しずつ外へずらして、キャラが入れる所を探す。見つからなければ飛ばない
        bool TryWarp(CharacterMotor motor, string fromBoxId)
        {
            var root = StageContext.StageRoot;
            var partnerId = WarpPairs.Partner(CurrentList(), fromBoxId);
            var target = root != null && partnerId.Length > 0 ? root.Find(partnerId) : null;
            if (target == null)
            {
                return false;
            }
            var ramp = target.GetComponent<MeshCollider>() != null;
            var settings = motor.settings;
            var offsets = new[] { Vector2.zero, new Vector2(0.2f, 0f), new Vector2(-0.2f, 0f), new Vector2(0f, 0.2f), new Vector2(0f, -0.2f),
                new Vector2(0.35f, 0.35f), new Vector2(-0.35f, 0.35f), new Vector2(0.35f, -0.35f), new Vector2(-0.35f, -0.35f) };
            foreach (var o in offsets)
            {
                // 坂は、斜めの面（箱の座標で y = x）の上
                var top = target.TransformPoint(new Vector3(o.x, ramp ? o.x : 0.5f, o.y));
                var feet = top + Vector3.up * 0.002f;
                var r = settings.radius * 0.9f;
                if (Physics.CheckCapsule(feet + Vector3.up * (settings.radius + 0.002f), feet + Vector3.up * (settings.height - settings.radius), r,
                        Layers.TerrainMask, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }
                motor.Teleport(feet, motor.transform.rotation);
                return true;
            }
            return false;
        }

        // ---- 敵（紫） ----

        void UpdateEnemies(CharacterMotor motor)
        {
            var dt = Time.deltaTime;
            // 紫の箱ごとに、7 秒ごとに 1 体出す（全部で最大 5 体）。箱の上が空いていなければ、空くまで待つ
            foreach (var v in visuals)
            {
                if (v.Kind != BoxData.GimmickEnemy || v.Box == null) continue;
                spawnTimers.TryGetValue(v.Id, out var timer);
                timer += dt;
                if (timer >= GimmickRules.EnemySpawnSeconds && enemies.Count < GimmickRules.MaxEnemies && TrySpawnEnemy(v.Box))
                {
                    timer = 0f;
                }
                spawnTimers[v.Id] = Mathf.Min(timer, GimmickRules.EnemySpawnSeconds);
            }

            var feet = motor.transform.position;
            var hit = false;
            foreach (var e in enemies)
            {
                MoveEnemy(e, feet, dt);
                hit |= GimmickRules.EnemyTouches(e.Position, feet, motor.settings.radius, motor.settings.height);
            }
            // ハートを減らすのは、敵の一覧を見終わってから（0 になってゲームオーバーの画面に変わると、敵の一覧が消されることがあるため）
            if (hit)
            {
                TryDamage(true);
            }
        }

        // 敵を 1 フレーム分動かす。進めなければ向きを変え、はまって抜け出せなければ、出てきた所に戻す
        void MoveEnemy(Enemy e, Vector3 target, float dt)
        {
            e.DetourTimer -= dt;
            var chasing = e.Mode == GimmickRules.EnemyMode.Chase && e.DetourTimer <= 0f;
            if (!chasing)
            {
                e.WanderTimer -= dt;
                if (e.WanderTimer <= 0f)
                {
                    e.Direction = GimmickRules.WanderDirection(UnityEngine.Random.value);
                    e.WanderTimer = GimmickRules.EnemyWanderSeconds;
                }
            }
            var dir = chasing ? GimmickRules.ChaseDirection(e.Position, target) : e.Direction;
            var flat = new Vector3(dir.x, 0f, dir.z);
            if (flat.sqrMagnitude > 1e-6f)
            {
                e.Facing = flat.normalized; // 進む向きに顔を向ける
            }
            if (!TryStepEnemy(e, dir * (GimmickRules.EnemySpeed * dt)))
            {
                // 進めなかった：進める向きを探して、そちらへ向きを変える。追いかける敵は、しばらく回り道をする
                TurnToOpenDirection(e);
            }

            // ほとんど動けていなければ、向きを変える。それでも抜け出せなければ、出てきた所に戻す
            if ((e.Position - e.LastProgress).sqrMagnitude > 0.003f * 0.003f)
            {
                e.LastProgress = e.Position;
                e.StuckTimer = 0f;
                return;
            }
            e.StuckTimer += dt;
            if (e.StuckTimer >= GimmickRules.EnemyStuckRespawnSeconds)
            {
                if (!OverlapsOtherEnemy(e, e.Spawn))
                {
                    e.Position = e.Spawn;
                }
                e.LastProgress = e.Position;
                e.StuckTimer = 0f;
                TurnToOpenDirection(e);
            }
            else if (e.StuckTimer >= GimmickRules.EnemyStuckTurnSeconds && e.StuckTimer - dt < GimmickRules.EnemyStuckTurnSeconds)
            {
                TurnToOpenDirection(e);
            }
        }

        // 8 方向をためして、少しでも進める向きに変える（見つからなければ、くじで決めた向き）
        void TurnToOpenDirection(Enemy e)
        {
            var start = UnityEngine.Random.value;
            for (var i = 0; i < 8; i++)
            {
                var dir = GimmickRules.WanderDirection(start + i / 8f);
                if (CanMoveEnemyTo(e, e.Position + dir * 0.006f, out _))
                {
                    e.Direction = dir;
                    break;
                }
                e.Direction = GimmickRules.WanderDirection(UnityEngine.Random.value);
            }
            e.WanderTimer = GimmickRules.EnemyWanderSeconds;
            if (e.Mode == GimmickRules.EnemyMode.Chase)
            {
                e.DetourTimer = GimmickRules.EnemyDetourSeconds;
            }
        }

        // 紫の箱の上に敵を出す。まん中に敵がいれば、上面の空いている所をさがす。どこも空いていなければ出さない
        bool TrySpawnEnemy(Transform box)
        {
            var offsets = new[] { Vector2.zero, new Vector2(0.25f, 0f), new Vector2(-0.25f, 0f), new Vector2(0f, 0.25f), new Vector2(0f, -0.25f),
                new Vector2(0.25f, 0.25f), new Vector2(-0.25f, 0.25f), new Vector2(0.25f, -0.25f), new Vector2(-0.25f, -0.25f) };
            foreach (var o in offsets)
            {
                var feet = box.TransformPoint(new Vector3(o.x, 0.5f, o.y));
                if (OverlapsOtherEnemy(null, feet))
                {
                    continue;
                }
                SpawnEnemy(feet);
                return true;
            }
            return false;
        }

        // 立体の敵（2026-11-07）：赤い玉の体に、白い目と黒いひとみ。進む向き（+z）に顔を向ける。当たりの大きさは EnemyRadius のまま
        GameObject BuildEnemyBody()
        {
            var size = GimmickRules.EnemyVisualSize;
            var root = new GameObject("GimmickEnemy");
            root.transform.SetParent(transform, false);
            Part(root.transform, "Body", new Vector3(0f, 0f, 0f), Vector3.one * size, new Color(0.9f, 0.1f, 0.1f));
            foreach (var side in new[] { -1f, 1f })
            {
                Part(root.transform, "Eye", new Vector3(side * 0.2f, 0.14f, 0.36f) * size, Vector3.one * (0.3f * size), Color.white);
                Part(root.transform, "Pupil", new Vector3(side * 0.2f, 0.14f, 0.5f) * size, Vector3.one * (0.14f * size), new Color(0.08f, 0.05f, 0.05f));
            }
            return root;
        }

        void Part(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            part.name = name;
            var collider = part.GetComponent<Collider>();
            if (collider != null) UiKit.Kill(collider);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = LitMaterial(color);
        }

        // 陰影の付く素材（立体に見せるため）。ビルドで見つからなければ、しかけの板と同じ素材
        Material LitMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                return NewMaterial(null, color);
            }
            var material = new Material(shader) { color = color };
            created.Add(material);
            return material;
        }

        void SpawnEnemy(Vector3 feet)
        {
            var go = BuildEnemyBody();
            // 追いかける敵と歩き回る敵が、どちらも出るように決める
            var chase = 0;
            var wander = 0;
            foreach (var other in enemies)
            {
                if (other.Mode == GimmickRules.EnemyMode.Chase) chase++; else wander++;
            }
            enemies.Add(new Enemy
            {
                Transform = go.transform,
                Position = feet,
                Spawn = feet,
                LastProgress = feet,
                Mode = GimmickRules.ChooseEnemyMode(UnityEngine.Random.value, chase, wander),
                Direction = GimmickRules.WanderDirection(UnityEngine.Random.value),
                WanderTimer = GimmickRules.EnemyWanderSeconds,
            });
        }

        // 敵を少し動かす。進めなければ、壁に沿って横にすべれるか（x だけ、z だけ）も試す
        bool TryStepEnemy(Enemy e, Vector3 step)
        {
            if (step.sqrMagnitude < 1e-12f)
            {
                return true;
            }
            return TryMoveEnemyTo(e, e.Position + step)
                || TryMoveEnemyTo(e, e.Position + new Vector3(step.x, 0f, 0f))
                || TryMoveEnemyTo(e, e.Position + new Vector3(0f, 0f, step.z));
        }

        bool TryMoveEnemyTo(Enemy e, Vector3 next)
        {
            if (!CanMoveEnemyTo(e, next, out var landed))
            {
                return false;
            }
            e.Position = landed;
            return true;
        }

        // 進む先へ動けるか。箱や壁を通り抜けないよう、進む先に敵の体の大きさの球を置いてみて、
        // 登れる段差（0.9cm）より上で箱に重なるなら進まない。ほかの敵とも重ならない（すでに重なっているときは、離れる向きなら進める）。
        // そのうえで足元を光線で調べ、紫の箱の上面から出ない（2026-11-09：紫の箱から落ちて、下でぐるぐる回っていたため）。landed は、動いたあとの足元
        bool CanMoveEnemyTo(Enemy e, Vector3 next, out Vector3 landed)
        {
            landed = next;
            var step = GimmickRules.EnemyMaxStep;
            var r = GimmickRules.EnemyRadius * 1.1f;
            var body = next + Vector3.up * (step + r + 0.001f);
            if (Physics.CheckSphere(body, r, Layers.TerrainMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
            // 光線は、登れる段差のすぐ上から落とす（それより高い箱は、上の球で止めてあるので、箱の中から落とすことはない）
            if (!Physics.Raycast(next + Vector3.up * (step + 0.002f), Vector3.down, out var hit, 0.5f, Layers.TerrainMask, QueryTriggerInteraction.Ignore)
                || hit.normal.y < 0.9f || hit.point.y > e.Position.y + step || !IsEnemyBox(hit.collider.transform))
            {
                return false;
            }
            landed = new Vector3(next.x, hit.point.y, next.z);
            foreach (var other in enemies)
            {
                if (other == e || !GimmickRules.EnemiesOverlap(landed, other.Position)) continue;
                // すでに重なっているなら、離れる向きにだけは動ける
                var before = Flat(e.Position - other.Position).sqrMagnitude;
                var after = Flat(landed - other.Position).sqrMagnitude;
                if (after <= before) return false;
            }
            return true;
        }

        // 紫の箱（敵の出る箱）か
        bool IsEnemyBox(Transform t)
        {
            foreach (var v in visuals)
            {
                if (v.Kind == BoxData.GimmickEnemy && v.Box == t) return true;
            }
            return false;
        }

        // ほかの敵と重なるか（self は除く。null なら全部の敵と比べる）
        bool OverlapsOtherEnemy(Enemy self, Vector3 feet)
        {
            foreach (var other in enemies)
            {
                if (other != self && GimmickRules.EnemiesOverlap(feet, other.Position)) return true;
            }
            return false;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        // 敵の見た目を、足元の少し上に置き、進む向きに顔を向ける（立体なので、カメラの方へは向けない）
        void AnimateEnemies()
        {
            var t = Time.unscaledTime;
            foreach (var e in enemies)
            {
                if (e.Transform == null) continue;
                var bob = 0.002f * Mathf.Abs(Mathf.Sin(t * 8f + e.Position.x * 50f));
                e.Transform.position = e.Position + Vector3.up * (GimmickRules.EnemyVisualSize * 0.5f + bob); // 足もとが面に付くよう、見た目の半分だけ上げる
                e.Transform.rotation = Quaternion.LookRotation(e.Facing, Vector3.up);
            }
        }

        void ClearEnemies()
        {
            foreach (var e in enemies)
            {
                if (e.Transform != null) UiKit.Kill(e.Transform.gameObject);
            }
            enemies.Clear();
        }

        // 水の箱が流す速さ（ステージの向きを、世界の向きに直したもの）
        static Vector3 PushOf(Visual v, float speed)
        {
            var world = v.Box.TransformDirection(new Vector3(v.Direction.x, 0f, v.Direction.y));
            world.y = 0f;
            return world.normalized * speed;
        }
    }
}
