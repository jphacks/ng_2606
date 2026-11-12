using DeskStage.Gameplay;
using DeskStage.Merge;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.App
{
    /// <summary>
    /// 足もとの土煙（2026-11-12）。キャラの動きが分かるように：
    ///  ・歩く・走る：足が地面に着くたびに（CharacterLimbs.Stepped）、足もとから後ろと左右へ煙。歩くときは小さく少なく、速いほど大きく多い。
    ///    走ると、煙が進む向きに横長にのびて、後ろへ流れる
    ///  ・跳ぶ：跳んだ瞬間に（CharacterMotor.Jumped。地面からのジャンプだけ）、足もとから輪のように広がる煙
    ///  ・着地：地面に着いた瞬間に（CharacterLimbs.Landed）、跳ぶときより大きな輪の煙。速く落ちてきたほど大きい
    /// 煙は、うすい半透明の玉（立体）。上が明るく下が少し暗い陰を、絵として描き込んで、どの向きから見ても丸い立体に見せる
    /// （光の計算をする素材は、ビルドに半透明の形が入っていないため）。ふわっと大きくなってから、しぼみながら薄くなって消える。
    /// 玉は使い回す（Count 個）。遊ぶ画面のときだけ出す。数・大きさ・のび方の決め方は、EditMode テストで確かめる。
    /// </summary>
    public class DustEffects : MonoBehaviour
    {
        const int Count = 64;
        static readonly Color DustColor = new Color(0.9f, 0.87f, 0.82f);

        /// <summary>煙の濃さ（出たときの不透明さ）。2026-11-12：玉を不透明にしたら濃すぎたので、うすくした。</summary>
        public const float Opacity = 0.4f;

        class Puff
        {
            public Transform Transform;
            public Material Material;
            public Vector3 Velocity;
            public Vector3 Direction; // のびる向き（横の向き、長さ 1）
            public float Stretch;     // 進む向きへの、のび方（1：丸）
            public float Age;
            public float Life;
            public float EndSize;
            public float GroundY;     // 煙が出た地面の高さ（玉の下のふちを、これより下にしない）
            public bool Alive;
        }

        AppRouter router;
        Material template;
        Texture2D shading;
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
            if (shading != null) Destroy(shading);
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

        // ---- 煙の出し方（数・大きさ・のび方）。テストで確かめる ----

        /// <summary>足が着いたときの煙の数（歩くと 2、速いほど増えて、走ると 4）。</summary>
        public static int StepPuffs(float speed01) => speed01 < 0.45f ? 2 : speed01 < 0.8f ? 3 : 4;

        /// <summary>足が着いたときの煙の大きさ（m。いちばん大きいときの太さ）。歩くと小さく、走ると大きめ。</summary>
        public static float StepSize(float speed01) => Mathf.Lerp(0.006f, 0.012f, Mathf.Clamp01(speed01));

        /// <summary>足が着いたときの煙の、進む向きへの、のび方（1：丸）。走るほど横長にのびて、流れるように見える。</summary>
        public static float StepStretch(float speed01) => Mathf.Lerp(1.1f, 2.7f, Mathf.Clamp01(speed01));

        /// <summary>着地の煙の大きさ（m）。速く落ちてきたほど大きい（落ちる速さ 0.3〜1.5m/秒の間で変わる）。</summary>
        public static float LandSize(float fallSpeed) => Mathf.Lerp(0.008f, 0.016f, Mathf.InverseLerp(0.3f, 1.5f, fallSpeed));

        /// <summary>着地の煙の数（輪に並べる）。</summary>
        public static int LandPuffs(float fallSpeed) => fallSpeed < 0.8f ? 8 : 12;

        void OnStepped(Vector3 feet, Vector3 forward, float speed01)
        {
            if (!Playing) return;
            var count = StepPuffs(speed01);
            var size = StepSize(speed01);
            var stretch = StepStretch(speed01);
            var right = Vector3.Cross(Vector3.up, forward);
            for (var i = 0; i < count; i++)
            {
                // 足もとから、後ろと左右へ散らす（後ろだけだと、キャラを正面から見たとき、体の後ろに隠れて見えなかった）。
                // 走るほど、後ろ向きを強く、速くする（後ろへ流れる）
                var spread = count == 1 ? 0f : Mathf.Lerp(-1.3f, 1.3f, i / (float)(count - 1)) * (1f - 0.5f * speed01);
                var direction = (-forward * (0.7f + speed01) + right * (spread + Random.Range(-0.2f, 0.2f))).normalized;
                var speed = Random.Range(0.02f, 0.035f) * (0.6f + speed01 * 1.6f);
                Spawn(feet - forward * 0.004f + Vector3.up * 0.002f, direction * speed + Vector3.up * 0.01f, direction,
                    size * Random.Range(0.85f, 1.15f), stretch, Random.Range(0.4f, 0.55f));
            }
        }

        void OnJumped(bool air)
        {
            if (!Playing || air || hooked == null) return;
            Ring(hooked.transform.position, 8, 0.04f, 0.008f, 0.45f);
        }

        void OnLanded(Vector3 feet, float fallSpeed)
        {
            if (!Playing) return;
            Ring(feet, LandPuffs(fallSpeed), 0.05f + 0.03f * Mathf.Clamp01(fallSpeed), LandSize(fallSpeed), 0.55f);
        }

        // 足もとから、輪のように広がる煙（外向きに、少しのびる）
        void Ring(Vector3 feet, int count, float speed, float size, float life)
        {
            var start = Random.Range(0f, 360f);
            for (var i = 0; i < count; i++)
            {
                var angle = (start + 360f * i / count) * Mathf.Deg2Rad;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Spawn(feet + direction * 0.004f + Vector3.up * 0.002f, direction * speed + Vector3.up * 0.01f, direction,
                    size * Random.Range(0.85f, 1.15f), 1.4f, life * Random.Range(0.85f, 1.1f));
            }
        }

        void Spawn(Vector3 at, Vector3 velocity, Vector3 direction, float size, float stretch, float life)
        {
            if (template == null) return;
            var p = puffs[next] ??= Make();
            next = (next + 1) % Count;
            p.Transform.gameObject.SetActive(true);
            p.Transform.position = at;
            p.Velocity = velocity;
            p.Direction = direction.sqrMagnitude > 1e-6f ? direction : Vector3.forward;
            p.Stretch = stretch;
            p.Age = 0f;
            p.Life = life;
            p.EndSize = size;
            p.GroundY = at.y - 0.002f;
            p.Alive = true;
        }

        // 煙の玉：球の形に、うすい半透明の素材（しかけと同じ）と、陰を描き込んだ絵。うすくして消すので、玉ごとに素材を分ける
        Puff Make()
        {
            shading ??= Shading();
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Dust";
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            go.transform.SetParent(transform, false);
            var material = new Material(template);
            material.SetTexture("_BaseMap", shading);
            material.mainTexture = shading;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return new Puff { Transform = go.transform, Material = material };
        }

        /// <summary>煙の玉の大きさの変わり方（0〜1）：はじめの 2 割で、ふわっと大きくなり、そのあと、少ししぼみながら消える。</summary>
        public static float Swell(float k)
        {
            k = Mathf.Clamp01(k);
            if (k < 0.2f)
            {
                var g = k / 0.2f;
                return Mathf.Lerp(0.35f, 1f, 1f - (1f - g) * (1f - g));
            }
            var r = (k - 0.2f) / 0.8f;
            return 1f - 0.6f * r * r;
        }

        void LateUpdate()
        {
            var dt = Time.deltaTime;
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
                var size = p.EndSize * Swell(k);
                // 進む向きに、のびる（のびは、広がるにつれて少し戻る）。玉の下のふちを、地面より上に保つ（机にめり込んで、隠れないように）
                var stretch = Mathf.Lerp(p.Stretch, 1f + (p.Stretch - 1f) * 0.7f, k);
                p.Transform.rotation = Quaternion.LookRotation(p.Direction, Vector3.up);
                p.Transform.localScale = new Vector3(size, size * 0.8f, size * stretch);
                var position = p.Transform.position;
                position.y = Mathf.Max(position.y, p.GroundY + size * 0.4f);
                p.Transform.position = position;
                // 出始めから うすく、消えるにつれて、もっと うすく
                var color = DustColor;
                color.a = Opacity * (1f - k * k) * Mathf.Clamp01(p.Age / 0.04f);
                p.Material.color = color;
                p.Material.SetColor("_BaseColor", color);
            }
        }

        // 玉の陰の絵：球の絵の上下（v：0 が下、1 が上）で、上を明るく、下を少し暗く。光の計算をしない素材でも、立体に見える
        static Texture2D Shading()
        {
            const int height = 32;
            var pixels = new Color32[2 * height];
            for (var y = 0; y < height; y++)
            {
                var v = (y + 0.5f) / height;
                var light = Mathf.Min(1f, Mathf.Lerp(0.72f, 1.06f, Mathf.SmoothStep(0f, 1f, v)));
                var c = (Color32)new Color(light, light, light, 1f);
                pixels[y * 2] = c;
                pixels[y * 2 + 1] = c;
            }
            var t = new Texture2D(2, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "DustShading" };
            t.SetPixels32(pixels);
            t.Apply();
            return t;
        }
    }
}
