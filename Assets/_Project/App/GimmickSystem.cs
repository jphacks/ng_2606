using System;
using System.Collections.Generic;
using DeskStage.Gameplay;
using DeskStage.Merge;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.App
{
    /// <summary>
    /// 箱のしかけ（折り紙の色）。上面が一面 青・赤・緑 の箱に、見た目と動きをつける。
    ///  ・水（青）：水面に流れる矢印。乗ると、決まった向きに、少し流される
    ///  ・炎（赤）：燃える炎。乗るとハートが 1 つ減る（減ったあとの 1.5 秒は、続けて減らない）
    ///  ・回復（緑）：光る＋。乗っている間、1 秒ごとにハートが 1 つ回復する
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

        class Visual
        {
            public string Id;
            public string Kind;
            public Transform Box;
            public Material Material;
            public Vector2 Direction;
            public List<Billboard> Billboards = new List<Billboard>();
        }

        class Billboard
        {
            public Transform Transform;
            public Vector2 Local;     // 箱の上面の中での位置（-0.5〜0.5）
            public float Phase;
        }

        readonly List<Visual> visuals = new List<Visual>();
        readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();
        readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        GameFlow flow;
        float healTimer;
        bool dirty;

        /// <summary>しかけのある箱の数（種類ごと）。たしかめ画面で、見つけた数を伝えるのに使う。</summary>
        public string Summary()
        {
            var water = 0;
            var fire = 0;
            var heal = 0;
            foreach (var b in CurrentBoxes())
            {
                switch (b.gimmick)
                {
                    case BoxData.GimmickWater: water++; break;
                    case BoxData.GimmickFire: fire++; break;
                    case BoxData.GimmickHeal: heal++; break;
                }
            }
            var parts = new List<string>();
            if (water > 0) parts.Add($"{AppText.GimmickWater} {water}");
            if (fire > 0) parts.Add($"{AppText.GimmickFire} {fire}");
            if (heal > 0) parts.Add($"{AppText.GimmickHeal} {heal}");
            return string.Join("　", parts);
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
                var visual = new Visual { Id = b.id, Kind = b.gimmick, Box = box };
                BuildPad(visual);
                BuildBillboards(visual);
                visuals.Add(visual);
            }
        }

        void Clear()
        {
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
            var dir = GimmickRules.WaterDirection(v.Id);
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
                default:
                    texture = GetTexture("heal", GimmickTextures.Heal);
                    break;
            }
            var pad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pad.name = "Gimmick_" + v.Kind;
            var collider = pad.GetComponent<Collider>();
            if (collider != null) UiKit.Kill(collider);
            pad.transform.SetParent(v.Box, false);
            pad.transform.localPosition = new Vector3(0f, 0.5f * 1.002f + 0.0025f / Mathf.Max(scale.y, 1e-3f), 0f);
            pad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            pad.transform.localScale = Vector3.one;
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
            if (v.Kind == BoxData.GimmickWater)
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

        void LateUpdate() => AnimateBillboards();

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
            foreach (var v in visuals)
            {
                if (v.Box == null) continue;
                var scale = v.Box.lossyScale;
                foreach (var b in v.Billboards)
                {
                    if (b.Transform == null) continue;
                    var local = new Vector3(b.Local.x, 0.5f * 1.002f, b.Local.y);
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
                return;
            }
            // 流される分は、キャラの動きに足す（別に Move を呼ぶと、接地の判定が狂って、ジャンプできなくなる）。乗っていなければ、毎フレーム ゼロに戻す
            var push = Vector3.zero;
            if (visuals.Count == 0 || flow.State != GameState.Playing)
            {
                flow.Motor.ExternalVelocity = Vector3.zero;
                healTimer = 0f;
                return;
            }
            var feet = flow.Motor.transform.position;
            var healing = false;
            foreach (var v in visuals)
            {
                if (v.Box == null) continue;
                var local = v.Box.InverseTransformPoint(feet);
                if (!GimmickRules.IsOnTop(local, v.Box.lossyScale.y)) continue;
                switch (v.Kind)
                {
                    case BoxData.GimmickWater:
                        push += PushOf(v);
                        break;
                    case BoxData.GimmickFire:
                        if (Health.TryDamage(Time.time))
                        {
                            Damaged?.Invoke(Health.Hearts);
                            if (Health.IsDead) GameOver?.Invoke();
                        }
                        break;
                    case BoxData.GimmickHeal:
                        healing = true;
                        break;
                }
            }
            flow.Motor.ExternalVelocity = push;
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

        // 水の箱が流す速さ（ステージの向きを、世界の向きに直したもの）
        static Vector3 PushOf(Visual v)
        {
            var world = v.Box.TransformDirection(new Vector3(v.Direction.x, 0f, v.Direction.y)).normalized;
            return world * GimmickRules.WaterSpeed;
        }
    }
}
