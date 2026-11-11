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
                case Cue.StepLeft: clip = stepLeft ??= MakeStep(230f); volume = 0.5f; break;
                case Cue.StepRight: clip = stepRight ??= MakeStep(205f); volume = 0.5f; break;
                case Cue.Jump: clip = jump ??= MakeSweep(380f, 900f, 0.16f, 0.45f); break;
                case Cue.AirJump: clip = airJump ??= MakeSweep(620f, 1400f, 0.13f, 0.4f); break;
                case Cue.JumpPad: clip = jumpPad ??= MakeSpring(); break;
                case Cue.Goal: clip = goal ??= MakeFanfare(); break;
                default: clip = click ??= Make(new[] { 900f }, 0.06f, 0.45f); break;
            }
            source.PlayOneShot(clip, volume);
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
        /// 足音：やわらかい「ぽふ」（2026-11-11）。pitch は音の高さ（Hz）。
        /// 立ち上がりをゆっくり（約 8 ミリ秒）にし、少し高さを下げながら、すぐ小さくなる丸い音。高い音を削る。
        /// 前は、ざらざらした雑音を一瞬混ぜていて、パンという破裂音（銃のよう）に聞こえた。
        /// </summary>
        static AudioClip MakeStep(float pitch)
        {
            var length = Mathf.RoundToInt(SampleRate * 0.11f);
            var data = new float[length];
            var phase = 0f;
            var smooth = 0f;
            var attackSamples = SampleRate * 0.008f;
            for (var i = 0; i < length; i++)
            {
                var t = i / (float)SampleRate;
                // 高さを、出だしの 1.25 倍から、少しずつ下げる
                var frequency = pitch * (1f + 0.25f * Mathf.Exp(-t / 0.02f));
                phase += 2f * Mathf.PI * frequency / SampleRate;
                var attack = Mathf.Sin(Mathf.Clamp01(i / attackSamples) * Mathf.PI * 0.5f);
                var envelope = attack * Mathf.Exp(-t / 0.028f);
                var tone = Mathf.Sin(phase) + 0.12f * Mathf.Sin(2f * phase);
                // 高い音を削って、角のない音にする
                smooth = Mathf.Lerp(smooth, tone * envelope, 0.35f);
                data[i] = smooth * 0.5f;
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
