using System;
using System.Collections.Generic;
using DeskStage.Gameplay;
using DeskStage.Merge;
using DeskStage.Shared;
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
        }

        readonly List<Visual> visuals = new List<Visual>();
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
            var warp = false;
            UpdateEnemies(motor);
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
                        warp = true;
                        break;
                }
            }
            flow.Motor.ExternalVelocity = push;
            if (warp && Time.time >= warpReadyAt && TryWarp(motor))
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

        // 机の上か箱の上の、どこかランダムな所へ飛ぶ。机の長方形の中から点をくじで選び、真上から光線を落として、
        // 一番上の平らな面に置く（机から落ちる所には飛ばない）。キャラが入れない所（壁の中など）なら、選び直す
        bool TryWarp(CharacterMotor motor)
        {
            var root = StageContext.StageRoot;
            var stage = StageContext.CurrentStage;
            if (root == null || stage == null)
            {
                return false;
            }
            var settings = motor.settings;
            for (var i = 0; i < 30; i++)
            {
                var c = GimmickRules.WarpCandidate(UnityEngine.Random.value, UnityEngine.Random.value, stage.desk.width, stage.desk.depth, GimmickRules.WarpEdgeInset);
                var from = root.TransformPoint(new Vector3(c.x, 1f, c.y));
                if (!Physics.Raycast(from, Vector3.down, out var hit, 3f, Layers.TerrainMask, QueryTriggerInteraction.Ignore) || hit.normal.y < 0.9f)
                {
                    continue;
                }
                var feet = hit.point + Vector3.up * 0.002f;
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

        void SpawnEnemy(Vector3 feet)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "GimmickEnemy";
            var collider = go.GetComponent<Collider>();
            if (collider != null) UiKit.Kill(collider);
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * (GimmickRules.EnemyRadius * 2.4f);
            go.GetComponent<Renderer>().sharedMaterial = NewMaterial(GetTexture("enemyface", GimmickTextures.EnemyFace), Color.white);
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
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
        // そのうえで足元を光線で調べ、机の外には出ず、下へは降りられる。landed は、動いたあとの足元
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
                || hit.normal.y < 0.9f || hit.point.y > e.Position.y + step)
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

        // 敵の見た目を、足元の少し上に置き、いつもカメラの方を向ける
        void AnimateEnemies()
        {
            var cam = Camera.main;
            var t = Time.unscaledTime;
            foreach (var e in enemies)
            {
                if (e.Transform == null) continue;
                var bob = 0.002f * Mathf.Abs(Mathf.Sin(t * 8f + e.Position.x * 50f));
                e.Transform.position = e.Position + Vector3.up * (GimmickRules.EnemyRadius * 1.2f + bob);
                if (cam != null)
                {
                    e.Transform.rotation = Quaternion.LookRotation(e.Transform.position - cam.transform.position, cam.transform.up);
                }
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
        static Vector3 PushOf(Visual v)
        {
            var world = v.Box.TransformDirection(new Vector3(v.Direction.x, 0f, v.Direction.y)).normalized;
            return world * GimmickRules.WaterSpeed;
        }
    }
}
