using UnityEngine;

namespace DeskStage.App
{
    /// <summary>
    /// 効果音（押した音、できた音、失敗の音、シャッターの音）。音の素材は無いので、コードで作る短い音。
    /// 遊ぶときの音（足音、ジャンプ、ゴール。2026-11-11）も、ここで作る。鳴らす時を決めるのは GameSounds。
    /// 音を出す物（AudioSource）は、最初に鳴らすときに作る。
    /// </summary>
    public static class UiSound
    {
        public enum Cue
        {
            Click,
            Success,
            Error,
            Shutter,
            StepLeft,
            StepRight,
            Jump,
            AirJump,
            JumpPad,
            Goal,
            Warp,
            Fall,
            FireHit,
            EnemyHit,
            GameOver,
        }

        const int SampleRate = 22050;

        static AudioSource source;
        static AudioClip click;
        static AudioClip success;
        static AudioClip error;
        static AudioClip shutter;
        static AudioClip stepLeft;
        static AudioClip stepRight;
        static AudioClip jump;
        static AudioClip airJump;
        static AudioClip jumpPad;
        static AudioClip goal;
        static AudioClip warp;
        static AudioClip fall;
        static AudioClip fireHit;
        static AudioClip enemyHit;
        static AudioClip gameOver;
        static AudioClip water;
        static AudioSource waterSource;

        /// <summary>false にすると、音を出さない（確認用の画像を作るときなど）。</summary>
        public static bool Enabled = true;

        public static void Play(Cue cue)
        {
            if (!Enabled || !Application.isPlaying)
            {
                return;
            }
            EnsureSource();
            if (source == null)
            {
                return;
            }
            AudioClip clip;
            var volume = 0.8f;
            switch (cue)
            {
                case Cue.Success: clip = success ??= Make(new[] { 660f, 880f, 1320f }, 0.11f, 0.5f); break;
                case Cue.Error: clip = error ??= Make(new[] { 330f, 247f }, 0.16f, 0.5f); break;
                case Cue.Shutter: clip = shutter ??= MakeNoiseClick(); break;
                // 足音は、何度も鳴るので小さめ。左右で少し高さを変える
                case Cue.StepLeft: clip = stepLeft ??= MakeStep(1.09f); volume = 0.5f; break;
                case Cue.StepRight: clip = stepRight ??= MakeStep(1f); volume = 0.5f; break;
                case Cue.Jump: clip = jump ??= MakeSweep(380f, 900f, 0.16f, 0.45f); break;
                case Cue.AirJump: clip = airJump ??= MakeSweep(620f, 1400f, 0.13f, 0.4f); break;
                case Cue.JumpPad: clip = jumpPad ??= MakeSpring(); break;
                case Cue.Goal: clip = goal ??= MakeFanfare(); break;
                // 遊んでいるときの音（2026-11-15。聞き比べて、えらんだ）
                case Cue.Warp: clip = warp ??= MakeWarp(); break;
                case Cue.Fall: clip = fall ??= MakeFall(); break;
                case Cue.FireHit: clip = fireHit ??= MakeCrackle(); break;
                case Cue.EnemyHit: clip = enemyHit ??= MakeBuzz(); break;
                case Cue.GameOver: clip = gameOver ??= MakeTrombone(); break;
                default: clip = click ??= Make(new[] { 900f }, 0.06f, 0.45f); break;
            }
            source.PlayOneShot(clip, volume);
        }

        /// <summary>
        /// 水に流されている間の「ちょろちょろ」を、鳴らす・止める（2026-11-15）。毎フレーム呼んでよい（同じ状態なら何もしない）。
        /// くり返して鳴らすので、効果音とは別の鳴らし口を使う。
        /// </summary>
        public static void SetWater(bool on)
        {
            if (!Enabled || !Application.isPlaying)
            {
                return;
            }
            if (!on)
            {
                if (waterSource != null && waterSource.isPlaying) waterSource.Stop();
                return;
            }
            if (waterSource == null)
            {
                EnsureSource();
                if (source == null) return;
                waterSource = source.gameObject.AddComponent<AudioSource>();
                waterSource.playOnAwake = false;
                waterSource.spatialBlend = 0f;
                waterSource.loop = true;
                waterSource.volume = 0.7f;
                waterSource.clip = water ??= MakeTrickle();
            }
            if (!waterSource.isPlaying) waterSource.Play();
        }

        static void EnsureSource()
        {
            if (source != null)
            {
                return;
            }
            var go = new GameObject("UiSound") { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(go);
            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        /// <summary>音の高さ（Hz）を、順に、1 つ noteSeconds ずつ鳴らす（だんだん小さくなる）。</summary>
        static AudioClip Make(float[] notes, float noteSeconds, float volume)
        {
            var perNote = Mathf.RoundToInt(SampleRate * noteSeconds);
            var data = new float[perNote * notes.Length];
            for (var n = 0; n < notes.Length; n++)
            {
                for (var i = 0; i < perNote; i++)
                {
                    var t = i / (float)SampleRate;
                    var envelope = Mathf.Exp(-6f * i / perNote) * Mathf.Min(1f, i / 80f);
                    data[n * perNote + i] = Mathf.Sin(2f * Mathf.PI * notes[n] * t) * envelope * volume;
                }
            }
            var clip = AudioClip.Create("ui", data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// 足音：やわらかい靴底で走るような、弾む軽い「たったっ」（2026-11-12）。pitch は高さの倍率（左右で少し変えて、リズムを出す）。
        /// 620Hz から 430Hz へ少し下げる短い音。立ち上がりをやわらかく（約 3 ミリ秒）し、高い音を削って、角のない音にする。
        /// 前の低い「ぽふ」は、太鼓のように聞こえた。その前は、雑音を混ぜたパンという音で、銃のように聞こえた。
        /// </summary>
        static AudioClip MakeStep(float pitch)
        {
            var length = Mathf.RoundToInt(SampleRate * 0.05f);
            var data = new float[length];
            var phase = 0f;
            var smooth = 0f;
            for (var i = 0; i < length; i++)
            {
                var t = i / (float)SampleRate;
                var frequency = Mathf.Lerp(620f, 430f, Mathf.Clamp01(t / 0.03f)) * pitch;
                phase += 2f * Mathf.PI * frequency / SampleRate;
                var attack = Mathf.Sin(Mathf.Clamp01(i / (SampleRate * 0.003f)) * Mathf.PI * 0.5f);
                var tone = Mathf.Sin(phase) + 0.2f * Mathf.Sin(2f * phase);
                // 高い音を削って、角のない音にする
                smooth = Mathf.Lerp(smooth, tone * attack * Mathf.Exp(-t * 75f), 0.5f);
                data[i] = smooth * 0.55f;
            }
            var clip = AudioClip.Create("step", length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>ジャンプ：下から上へ、すっと上がる「ぴょん」。from から to（Hz）へ、seconds 秒で上げる。</summary>
        /// <summary>
        /// ジャンプ台：ばねの「びよーん」（2026-11-11）。下から上へ大きく上がりながら、高さが ぶるぶる揺れ、だんだん揺れがおさまる。
        /// ふつうのジャンプ（すっと上がる「ぴょん」）より長く、揺れがあるので、ジャンプ台で跳んだと分かる。
        /// </summary>
        static AudioClip MakeSpring()
        {
            const float seconds = 0.42f;
            var length = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[length];
            var phase = 0f;
            for (var i = 0; i < length; i++)
            {
                var t = i / (float)SampleRate;
                var k = t / seconds;
                // 220Hz から 880Hz へ上がる。上に、ばねの揺れ（1 秒に 22 回）を重ね、揺れはだんだん小さくする
                var frequency = Mathf.Lerp(220f, 880f, Mathf.Sqrt(k)) * (1f + 0.12f * Mathf.Sin(2f * Mathf.PI * 22f * t) * Mathf.Exp(-3f * t));
                phase += 2f * Mathf.PI * frequency / SampleRate;
                var envelope = Mathf.Min(1f, i / 80f) * (1f - k) * (1f - k * 0.5f);
                var wave = Mathf.Sin(phase) * 0.8f + Mathf.Sin(2f * phase) * 0.15f;
                data[i] = wave * envelope * 0.42f;
            }
            var clip = AudioClip.Create("jumppad", length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip MakeSweep(float from, float to, float seconds, float volume)
        {
            var length = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[length];
            var phase = 0f;
            for (var i = 0; i < length; i++)
            {
                var k = i / (float)length;
                var frequency = Mathf.Lerp(from, to, Mathf.Sqrt(k));
                phase += 2f * Mathf.PI * frequency / SampleRate;
                var envelope = Mathf.Min(1f, i / 60f) * (1f - k) * (1f - k);
                // 少し四角い波を混ぜて、ゲームらしい音にする
                var wave = Mathf.Sin(phase) * 0.8f + Mathf.Sign(Mathf.Sin(phase)) * 0.2f;
                data[i] = wave * envelope * volume;
            }
            var clip = AudioClip.Create("jump", length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>ゴール：ド・ミ・ソ・ドと上がって、最後に、ドとミを重ねて長く鳴らすファンファーレ。</summary>
        static AudioClip MakeFanfare()
        {
            var notes = new[] { 523.25f, 659.25f, 783.99f, 1046.5f };
            const float noteSeconds = 0.11f;
            const float lastSeconds = 0.75f;
            var perNote = Mathf.RoundToInt(SampleRate * noteSeconds);
            var last = Mathf.RoundToInt(SampleRate * lastSeconds);
            var data = new float[perNote * notes.Length + last];
            for (var n = 0; n < notes.Length; n++)
            {
                for (var i = 0; i < perNote; i++)
                {
                    var t = i / (float)SampleRate;
                    var envelope = Mathf.Exp(-3f * i / perNote) * Mathf.Min(1f, i / 80f);
                    data[n * perNote + i] = Tone(notes[n], t) * envelope * 0.45f;
                }
            }
            var start = perNote * notes.Length;
            for (var i = 0; i < last; i++)
            {
                var t = i / (float)SampleRate;
                var envelope = Mathf.Exp(-2.5f * i / last) * Mathf.Min(1f, i / 80f);
                // 少しゆらして、明るく伸ばす
                var vibrato = 1f + 0.004f * Mathf.Sin(2f * Mathf.PI * 6f * t);
                data[start + i] = (Tone(1046.5f * vibrato, t) + Tone(1318.5f * vibrato, t) * 0.7f + Tone(783.99f, t) * 0.5f) * envelope * 0.28f;
            }
            var clip = AudioClip.Create("goal", data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ラッパのような音：元の音に、倍の高さの音を少し混ぜる
        static float Tone(float frequency, float t)
        {
            var w = 2f * Mathf.PI * frequency * t;
            return Mathf.Sin(w) * 0.75f + Mathf.Sin(2f * w) * 0.2f + Mathf.Sin(3f * w) * 0.08f;
        }

        static AudioClip Clip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // 立ち上がり attack 秒で大きくなり、1 秒に decay の割合で小さくなる
        static float Envelope(float t, float attack, float decay) => Mathf.Min(1f, t / attack) * Mathf.Exp(-t * decay);

        // from から to（Hz）へ、seconds 秒で変わる音（shape：変わり方。1 で一定、小さいほど はじめに大きく変わる）。k（0〜1）を返す
        static float[] Glide(float from, float to, float seconds, float shape, out float[] k)
        {
            var length = Mathf.RoundToInt(SampleRate * seconds);
            var wave = new float[length];
            k = new float[length];
            var phase = 0f;
            for (var i = 0; i < length; i++)
            {
                k[i] = i / (float)length;
                phase += 2f * Mathf.PI * Mathf.Lerp(from, to, Mathf.Pow(k[i], shape)) / SampleRate;
                wave[i] = Mathf.Sin(phase);
            }
            return wave;
        }

        /// <summary>ワープ：上がっていく「ひゅいーん」に、きらきらの粒（ド・ミ・ソ・ドの高い音）が重なる。</summary>
        static AudioClip MakeWarp()
        {
            var data = Glide(300f, 1600f, 0.55f, 0.6f, out var k);
            for (var i = 0; i < data.Length; i++)
            {
                var t = i / (float)SampleRate;
                data[i] *= Mathf.Min(1f, t / 0.02f) * Mathf.Pow(1f - k[i], 0.6f) * 0.5f;
            }
            var sparkles = new[] { 84, 88, 91, 96 };
            for (var n = 0; n < sparkles.Length; n++)
            {
                var start = Mathf.RoundToInt(SampleRate * (0.12f + n * 0.07f));
                var frequency = 440f * Mathf.Pow(2f, (sparkles[n] - 69) / 12f);
                for (var i = 0; i < SampleRate * 0.25f && start + i < data.Length; i++)
                {
                    var t = i / (float)SampleRate;
                    data[start + i] += Mathf.Sin(2f * Mathf.PI * frequency * t) * Envelope(t, 0.002f, 14f) * 0.3f;
                }
            }
            return Clip("warp", data);
        }

        /// <summary>落ちた：高い音から、ゆっくり下がる「ひゅ〜〜」。</summary>
        static AudioClip MakeFall()
        {
            var data = Glide(1300f, 180f, 1f, 0.8f, out var k);
            for (var i = 0; i < data.Length; i++)
            {
                data[i] *= Mathf.Min(1f, i / (SampleRate * 0.02f)) * (1f - k[i] * 0.7f) * 0.4f;
            }
            return Clip("fall", data);
        }

        /// <summary>炎に当たった：火の粉がはじける「パチパチッ」（小さなざらざらの粒に、少し下がる音を重ねる）。</summary>
        static AudioClip MakeCrackle()
        {
            var data = Glide(900f, 600f, 0.4f, 0.5f, out _);
            for (var i = 0; i < data.Length; i++)
            {
                data[i] *= Envelope(i / (float)SampleRate, 0.003f, 10f) * 0.2f;
            }
            var random = new System.Random(5);
            foreach (var at in new[] { 0f, 0.03f, 0.07f, 0.12f, 0.2f, 0.27f })
            {
                var start = Mathf.RoundToInt(SampleRate * at);
                var loudness = 0.4f + 0.4f * (float)random.NextDouble();
                for (var i = 0; i < SampleRate * 0.025f && start + i < data.Length; i++)
                {
                    var t = i / (float)SampleRate;
                    data[start + i] += ((float)random.NextDouble() * 2f - 1f) * Envelope(t, 0.0005f, 160f) * loudness;
                }
            }
            return Clip("fire-hit", data);
        }

        /// <summary>敵に当たった：ぶるぶるふるえる「びびっ」（高さが、1 秒に 28 回、2 つの高さを行き来する）。</summary>
        static AudioClip MakeBuzz()
        {
            const float seconds = 0.3f;
            var length = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[length];
            var phase = 0f;
            for (var i = 0; i < length; i++)
            {
                var t = i / (float)SampleRate;
                var frequency = 300f + 120f * Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 28f * t));
                phase += 2f * Mathf.PI * frequency / SampleRate;
                var wave = Mathf.Sin(phase) * 0.6f + Mathf.Sign(Mathf.Sin(phase)) * 0.3f;
                data[i] = wave * Mathf.Min(1f, t / 0.005f) * (1f - t / seconds) * 0.45f;
            }
            return Clip("enemy-hit", data);
        }

        /// <summary>ゲームオーバー：トロンボーンが、ゆれながら下がる「ぷわわ〜ん」（ソ・ファ#・ファ・ミ。最後はのばして、ゆらす）。</summary>
        static AudioClip MakeTrombone()
        {
            var notes = new[] { (67, 0.35f), (66, 0.35f), (65, 0.35f), (64, 1.2f) };
            var total = 0;
            foreach (var (_, d) in notes) total += Mathf.RoundToInt(SampleRate * d);
            var data = new float[total];
            var at = 0;
            foreach (var (note, d) in notes)
            {
                var length = Mathf.RoundToInt(SampleRate * d);
                var phase = 0f;
                var last = d > 1f;
                for (var i = 0; i < length; i++)
                {
                    var t = i / (float)SampleRate;
                    var frequency = 440f * Mathf.Pow(2f, (note - 69) / 12f) * (last ? 1f + 0.012f * Mathf.Sin(2f * Mathf.PI * 6f * t) : 1f);
                    phase += 2f * Mathf.PI * frequency / SampleRate;
                    // 出だしはこもって、だんだん明るく（「わ」の音）
                    var wah = 0.5f + 0.5f * Mathf.Sin(Mathf.PI * Mathf.Min(1f, t / 0.25f));
                    var tone = Mathf.Sin(phase) + 0.6f * wah * Mathf.Sin(2f * phase) + 0.35f * wah * Mathf.Sin(3f * phase);
                    var release = t < d * 0.85f ? 1f : Mathf.Exp(-(t - d * 0.85f) * 10f);
                    data[at + i] = tone * Mathf.Min(1f, t / 0.03f) * release * 0.28f;
                }
                at += length;
            }
            return Clip("gameover", data);
        }

        /// <summary>
        /// 水に流されている間：細い水の、こまかいしずくの「ちょろちょろ」（3 秒をくり返す）。
        /// しずくの音は、くり返しのつなぎ目をまたぐときは頭に重ねるので、つなぎ目で切れない。
        /// </summary>
        static AudioClip MakeTrickle()
        {
            const float seconds = 3f;
            var length = Mathf.RoundToInt(SampleRate * seconds);
            var data = new float[length];
            var random = new System.Random(7);
            var dropLength = Mathf.RoundToInt(SampleRate * 0.04f);
            for (var d = 0; d < 84; d++)
            {
                var start = random.Next(length);
                var frequency = 1400f + 1200f * (float)random.NextDouble();
                var loudness = 0.06f + 0.08f * (float)random.NextDouble();
                var phase = 0f;
                for (var i = 0; i < dropLength; i++)
                {
                    var t = i / (float)SampleRate;
                    phase += 2f * Mathf.PI * frequency * (1f + 0.6f * i / dropLength) / SampleRate; // しずくは、少し上がる
                    data[(start + i) % length] += Mathf.Sin(phase) * Envelope(t, 0.001f, 70f) * loudness;
                }
            }
            return Clip("water", data);
        }

        static AudioClip MakeNoiseClick()
        {
            var length = Mathf.RoundToInt(SampleRate * 0.09f);
            var data = new float[length];
            var random = new System.Random(5);
            for (var i = 0; i < length; i++)
            {
                var envelope = Mathf.Exp(-30f * i / length);
                data[i] = ((float)random.NextDouble() * 2f - 1f) * envelope * 0.5f;
            }
            var clip = AudioClip.Create("shutter", length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
