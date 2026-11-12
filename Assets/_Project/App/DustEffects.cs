using DeskStage.Gameplay;
using DeskStage.Merge;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.App
{
    /// <summary>
    /// 足もとの土煙（2026-11-12）。キャラの動きが分かるように：
    ///  ・歩く・走る：足が地面に着くたびに（CharacterLimbs.Stepped）、足もとの少し後ろに煙。歩くときは小さく少なく、速いほど大きく多く
    ///  ・跳ぶ：跳んだ瞬間に（CharacterMotor.Jumped。地面からのジャンプだけ）、足もとから輪のように広がる煙
    ///  ・着地：地面に着いた瞬間に（CharacterLimbs.Landed）、跳ぶときより大きな輪の煙。速く落ちてきたほど大きい
    /// 煙は、やわらかい丸い絵の板（いつもカメラの方を向く）で、広がりながら少し上がって、薄くなって消える。
    /// 板は使い回す（Count 個）。遊ぶ画面のときだけ出す。煙の数や大きさの決め方は、EditMode テストで確かめる（StepPuffs など）。
    /// </summary>
    public class DustEffects : MonoBehaviour
    {
        const int Count = 64;
        static readonly Color DustColor = new Color(0.86f, 0.82f, 0.76f);

        class Puff
        {
            public Transform Transform;
            public Material Material;
            public Vector3 Velocity;
            public float Age;
            public float Life;
            public float StartSize;
            public float EndSize;
            public float Alpha;
            public float GroundY;     // 煙が出た地面の高さ（板の下のふちを、これより下にしない）
            public bool Alive;
        }

        AppRouter router;
        Material template;
        Texture2D texture;
        readonly Puff[] puffs = new Puff[Count];
        int next;
        GameFlow flow;
        CharacterMotor hooked;

        public void Set(AppRouter appRouter, Material material)
        {
            router = appRouter;
            template = material;
        }

        void OnEnable()
        {
            CharacterLimbs.Stepped += OnStepped;
            CharacterLimbs.Landed += OnLanded;
            GameEvents.GameStarted += OnGameStarted;
        }

        void OnDisable()
        {
            CharacterLimbs.Stepped -= OnStepped;
            CharacterLimbs.Landed -= OnLanded;
            GameEvents.GameStarted -= OnGameStarted;
            Hook(null);
        }

        void OnDestroy()
        {
            foreach (var p in puffs)
            {
                if (p == null) continue;
                if (p.Material != null) Destroy(p.Material);
                if (p.Transform != null) Destroy(p.Transform.gameObject);
            }
            if (texture != null) Destroy(texture);
        }

        bool Playing => router != null && router.Current == AppScreenId.Play;

        void OnGameStarted()
        {
            if (flow == null) flow = FindFirstObjectByType<GameFlow>();
            Hook(flow != null ? flow.Motor : null);
        }

        void Hook(CharacterMotor motor)
        {
            if (hooked == motor) return;
            if (hooked != null) hooked.Jumped -= OnJumped;
            hooked = motor;
            if (hooked != null) hooked.Jumped += OnJumped;
        }

        // ---- 煙の出し方（数と大きさ）。テストで確かめる ----

        /// <summary>足が着いたときの煙の数（歩くと 2、速いほど増えて、走ると 4。2026-11-12：1〜3 から増やした）。</summary>
        public static int StepPuffs(float speed01) => speed01 < 0.45f ? 2 : speed01 < 0.8f ? 3 : 4;

        /// <summary>足が着いたときの煙の大きさ（m。消えるときの大きさ）。歩くと小さく、走ると大きめ。</summary>
        public static float StepSize(float speed01) => Mathf.Lerp(0.008f, 0.018f, Mathf.Clamp01(speed01));

        /// <summary>着地の煙の大きさ（m）。速く落ちてきたほど大きい（落ちる速さ 0.3〜1.5m/秒の間で変わる）。</summary>
        public static float LandSize(float fallSpeed) => Mathf.Lerp(0.012f, 0.024f, Mathf.InverseLerp(0.3f, 1.5f, fallSpeed));

        /// <summary>着地の煙の数（輪に並べる）。</summary>
        public static int LandPuffs(float fallSpeed) => fallSpeed < 0.8f ? 8 : 12;

        void OnStepped(Vector3 feet, Vector3 forward, float speed01)
        {
            if (!Playing) return;
            var count = StepPuffs(speed01);
            var size = StepSize(speed01);
            var right = Vector3.Cross(Vector3.up, forward);
            for (var i = 0; i < count; i++)
            {
                // 足もとから、後ろと左右へ散らす（後ろだけだと、キャラを正面から見たとき、体の後ろに隠れて見えなかった）
                var spread = count == 1 ? 0f : Mathf.Lerp(-1.3f, 1.3f, i / (float)(count - 1));
                var direction = (-forward * 0.7f + right * (spread + Random.Range(-0.2f, 0.2f))).normalized;
                Spawn(feet - forward * 0.004f + Vector3.up * 0.002f, direction * Random.Range(0.02f, 0.035f) * (0.6f + speed01) + Vector3.up * 0.012f,
                    size * 0.4f, size * Random.Range(0.85f, 1.15f), Random.Range(0.35f, 0.5f), 0.55f + 0.2f * speed01);
            }
        }

        void OnJumped(bool air)
        {
            if (!Playing || air || hooked == null) return;
            Ring(hooked.transform.position, 8, 0.04f, 0.011f, 0.45f, 0.6f);
        }

        void OnLanded(Vector3 feet, float fallSpeed)
        {
            if (!Playing) return;
            var size = LandSize(fallSpeed);
            Ring(feet, LandPuffs(fallSpeed), 0.05f + 0.03f * Mathf.Clamp01(fallSpeed), size, 0.55f, 0.75f);
        }

        // 足もとから、輪のように広がる煙
        void Ring(Vector3 feet, int count, float speed, float size, float life, float alpha)
        {
            var start = Random.Range(0f, 360f);
            for (var i = 0; i < count; i++)
            {
                var angle = (start + 360f * i / count) * Mathf.Deg2Rad;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Spawn(feet + direction * 0.004f + Vector3.up * 0.002f, direction * speed + Vector3.up * 0.01f,
                    size * 0.35f, size * Random.Range(0.85f, 1.15f), life * Random.Range(0.85f, 1.1f), alpha);
            }
        }

        void Spawn(Vector3 at, Vector3 velocity, float startSize, float endSize, float life, float alpha)
        {
            if (template == null) return;
            var p = puffs[next] ??= Make();
            next = (next + 1) % Count;
            p.Transform.gameObject.SetActive(true);
            p.Transform.position = at;
            p.Velocity = velocity;
            p.Age = 0f;
            p.Life = life;
            p.StartSize = startSize;
            p.EndSize = endSize;
            p.Alpha = alpha;
            p.GroundY = at.y - 0.002f;
            p.Alive = true;
        }

        Puff Make()
        {
            texture ??= SoftCircle();
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Dust";
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            go.transform.SetParent(transform, false);
            var material = new Material(template);
            material.SetTexture("_BaseMap", texture);
            material.mainTexture = texture;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return new Puff { Transform = go.transform, Material = material };
        }

        void LateUpdate()
        {
            var dt = Time.deltaTime;
            var cam = Camera.main;
            foreach (var p in puffs)
            {
                if (p == null || !p.Alive) continue;
                p.Age += dt;
                if (p.Age >= p.Life || !Playing)
                {
                    p.Alive = false;
                    p.Transform.gameObject.SetActive(false);
                    continue;
                }
                var k = p.Age / p.Life;
                // 広がりながら、だんだん遅くなる。少し上がる
                p.Transform.position += p.Velocity * dt * (1f - k);
                var size = Mathf.Lerp(p.StartSize, p.EndSize, 1f - (1f - k) * (1f - k));
                // 板の下のふちを、地面より上に保つ（低い所から横向きに見ると、下半分が机にめり込んで隠れ、見る向きで量が違って見えた）
                var position = p.Transform.position;
                position.y = Mathf.Max(position.y, p.GroundY + size * 0.5f);
                p.Transform.position = position;
                p.Transform.localScale = new Vector3(size, size, size);
                if (cam != null) p.Transform.rotation = Quaternion.LookRotation(p.Transform.position - cam.transform.position, cam.transform.up);
                var color = DustColor;
                color.a = p.Alpha * (1f - k) * Mathf.Clamp01(p.Age / 0.05f);
                p.Material.color = color;
                p.Material.SetColor("_BaseColor", color);
            }
        }

        // やわらかい丸（真ん中が濃く、ふちに向かって、なめらかに消える）
        static Texture2D SoftCircle()
        {
            const int side = 32;
            var pixels = new Color32[side * side];
            for (var y = 0; y < side; y++)
            {
                for (var x = 0; x < side; x++)
                {
                    var dx = (x + 0.5f) / side * 2f - 1f;
                    var dy = (y + 0.5f) / side * 2f - 1f;
                    var r = Mathf.Sqrt(dx * dx + dy * dy);
                    var a = Mathf.Clamp01(1f - r);
                    pixels[y * side + x] = new Color32(255, 255, 255, (byte)(a * a * (3f - 2f * a) * 255f));
                }
            }
            var t = new Texture2D(side, side, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Dust" };
            t.SetPixels32(pixels);
            t.Apply();
            return t;
        }
    }
}
