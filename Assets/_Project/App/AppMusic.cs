using UnityEngine;

namespace DeskStage.App
{
    /// <summary>
    /// スタート画面の BGM（2026-11-11）。音の素材は無いので、効果音（UiSound）と同じく、コードで作る。
    /// テンポのよい、明るい 8 小節（ハ長調、1 分に 140 拍。約 14 秒。2026-11-11：112 拍の、ゆったりした曲から作り直した）を、くり返し鳴らす：
    ///  ・メロディー：マリンバのような、ころんとした音。8 分音符で弾む。和音は ド → ソ → ラ → ファ の明るい流れ
    ///  ・ベース：低い音と、1 オクターブ上の音を交互に（ぴょんぴょん跳ねる）
    ///  ・裏拍の和音：拍の裏で、短く「ちゃっ」（ノリを出す）
    ///  ・リズム：1 拍目と 3 拍目に軽い太鼓、8 分音符ごとに小さな「チッ」
    /// 終わりの音の響きを、曲の頭に重ねて作るので、くり返しのつなぎ目で音が切れない。
    /// Play でふわっと鳴り始め、Stop でふわっと消える（音量を少しずつ変える部品 MusicFader）。
    /// </summary>
    public static class AppMusic
    {
        const int SampleRate = 22050;
        public const float Bpm = 140f;
        public const int Bars = 8;
        const float FadeSeconds = 0.8f;

        /// <summary>BGM の大きさ（効果音より小さく）。</summary>
        public const float Volume = 0.28f;

        /// <summary>false にすると、鳴らさない（確認用の画像を作るときなど）。</summary>
        public static bool Enabled = true;

        static AudioClip clip;
        static AudioSource source;
        static MusicFader fader;

        // メロディー：(音の番号, 拍の長さ)。音の番号は、真ん中のド（C4）を 60 とする数（MIDI）。-1 は休み
        static readonly (int note, float beats)[] Melody =
        {
            (76, .5f), (79, .5f), (84, .5f), (79, .5f), (76, .5f), (79, .5f), (81, .5f), (79, .5f), // C
            (74, .5f), (79, .5f), (83, .5f), (79, .5f), (86, 1f), (83, 1f),                         // G
            (84, .5f), (83, .5f), (81, .5f), (76, .5f), (81, 1f), (84, 1f),                         // Am
            (81, .5f), (79, .5f), (77, .5f), (81, .5f), (84, 1.5f), (-1, .5f),                      // F
            (76, .5f), (79, .5f), (84, .5f), (88, .5f), (86, .5f), (84, .5f), (79, 1f),             // C
            (83, .5f), (86, .5f), (91, .5f), (86, .5f), (83, 1f), (79, 1f),                         // G
            (81, .5f), (84, .5f), (89, .5f), (84, .5f), (81, .5f), (77, .5f), (81, .5f), (84, .5f), // F
            (83, .5f), (86, .5f), (83, .5f), (79, .5f), (86, 1f), (-1, 1f),                         // G（頭の C へもどる）
        };

        // 小節ごとの和音（根音と、3 つの音）：ド → ソ → ラ → ファ を 2 回（最後はソで、頭のドへもどる）
        static readonly int[][] Chords =
        {
            new[] { 48, 60, 64, 67 }, new[] { 43, 59, 62, 67 }, new[] { 45, 60, 64, 69 }, new[] { 41, 60, 65, 69 },
            new[] { 48, 60, 64, 67 }, new[] { 43, 59, 62, 67 }, new[] { 41, 60, 65, 69 }, new[] { 43, 59, 62, 67 },
        };

        /// <summary>BGM を鳴らし始める（もう鳴っていれば、そのまま）。</summary>
        public static void Play()
        {
            if (!Enabled || !Application.isPlaying) return;
            Ensure();
            if (source == null) return;
            if (!source.isPlaying)
            {
                source.volume = 0f;
                source.Play();
            }
            fader.Target = Volume;
        }

        /// <summary>BGM を、ふわっと消す。</summary>
        public static void Stop()
        {
            if (fader != null) fader.Target = 0f;
        }

        static void Ensure()
        {
            if (source != null) return;
            clip ??= Make();
            var go = new GameObject("AppMusic") { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(go);
            source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            fader = go.AddComponent<MusicFader>();
            fader.Source = source;
            fader.Speed = Volume / FadeSeconds;
        }

        /// <summary>曲の音のデータを作る（くり返して鳴らす 1 回分）。</summary>
        public static float[] Samples()
        {
            var beat = 60f / Bpm;
            var length = Mathf.RoundToInt(SampleRate * beat * 4f * Bars);
            var data = new float[length];

            var at = 0f;
            foreach (var (note, beats) in Melody)
            {
                if (note >= 0) AddNote(data, at, beats * beat, note, Marimba, 0.32f);
                at += beats * beat;
            }
            for (var bar = 0; bar < Bars; bar++)
            {
                var start = bar * 4f * beat;
                var chord = Chords[bar];
                for (var step = 0; step < 8; step++)
                {
                    var t = start + step * beat * 0.5f;
                    // ベース：8 分音符ごとに、根音と 1 オクターブ上を交互に
                    AddNote(data, t, beat * 0.45f, chord[0] + (step % 2 == 1 ? 12 : 0), Bass, step % 2 == 0 ? 0.24f : 0.16f, 0.25f);
                    // 裏拍の和音：拍の裏（2 つ目の 8 分音符）で、短く
                    if (step % 2 == 1)
                    {
                        for (var i = 1; i < chord.Length; i++) AddNote(data, t, beat * 0.2f, chord[i], Stab, 0.06f, 0.2f);
                    }
                    // チッ：8 分音符ごと（裏を少し強く）
                    AddNote(data, t, 0.03f, 0, Tick, step % 2 == 1 ? 0.07f : 0.04f, 0.02f);
                }
                // 軽い太鼓：1 拍目と 3 拍目
                AddNote(data, start, 0.12f, 0, Kick, 0.32f, 0.1f);
                AddNote(data, start + beat * 2f, 0.12f, 0, Kick, 0.26f, 0.1f);
            }
            // 大きすぎるときだけ、全体を下げる（音が割れないように）
            var peak = 0f;
            foreach (var v in data) peak = Mathf.Max(peak, Mathf.Abs(v));
            if (peak > 0.95f)
            {
                for (var i = 0; i < data.Length; i++) data[i] *= 0.95f / peak;
            }
            return data;
        }

        static AudioClip Make()
        {
            var data = Samples();
            var made = AudioClip.Create("title-bgm", data.Length, 1, SampleRate, false);
            made.SetData(data, 0);
            return made;
        }

        delegate float Voice(float frequency, float t, float duration);

        // 音を 1 つ、start 秒から足す。響きが曲の終わりを越えたら、曲の頭に重ねる（くり返しのつなぎ目で切れないように）
        // tail は、のばした後に響きを残す長さ（秒）。音ごとに必要なだけにして、作る手間を減らす
        static void AddNote(float[] data, float start, float duration, int note, Voice voice, float volume, float tail = 0.6f)
        {
            var frequency = 440f * Mathf.Pow(2f, (note - 69) / 12f);
            var first = Mathf.RoundToInt(start * SampleRate);
            var count = Mathf.RoundToInt((duration + tail) * SampleRate); // のばした後の響きまで
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                data[(first + i) % data.Length] += voice(frequency, t, duration) * volume;
            }
        }

        // マリンバ：すぐ小さくなる、ころんとした音（元の音に、4 倍の高さの音を少し）
        static float Marimba(float f, float t, float duration)
        {
            var attack = Mathf.Min(1f, t / 0.004f);
            var envelope = attack * Mathf.Exp(-t * 5.5f);
            var w = 2f * Mathf.PI * f * t;
            return (Mathf.Sin(w) + 0.25f * Mathf.Sin(4f * w) * Mathf.Exp(-t * 18f)) * envelope;
        }

        // ベース：やわらかい低い音
        static float Bass(float f, float t, float duration)
        {
            var attack = Mathf.Min(1f, t / 0.01f);
            var release = t < duration ? 1f : Mathf.Exp(-(t - duration) * 12f);
            var envelope = attack * Mathf.Exp(-t * 1.8f) * release;
            var w = 2f * Mathf.PI * f * t;
            return (Mathf.Sin(w) + 0.2f * Mathf.Sin(2f * w)) * envelope;
        }

        // 裏拍の和音：短く「ちゃっ」と切れる音
        static float Stab(float f, float t, float duration)
        {
            var attack = Mathf.Min(1f, t / 0.003f);
            var envelope = attack * Mathf.Exp(-t * 22f);
            var w = 2f * Mathf.PI * f * t;
            return (Mathf.Sin(w) + 0.35f * Mathf.Sin(2f * w) + 0.15f * Mathf.Sin(3f * w)) * envelope;
        }

        // 軽い太鼓：低い「ぽん」（高さを下げながら、すぐ消える）
        static float Kick(float f, float t, float duration)
        {
            var phase = 2f * Mathf.PI * (55f * t + 45f * (1f - Mathf.Exp(-t * 30f)) / 30f);
            return Mathf.Sin(phase) * Mathf.Exp(-t * 28f) * Mathf.Min(1f, t / 0.002f);
        }

        // チッ：とても短い、高い音（雑音の代わりに、高い音を 2 つ重ねて、決まった音にする）
        static float Tick(float f, float t, float duration)
        {
            var w = 2f * Mathf.PI * t;
            return (Mathf.Sin(w * 7300f) * 0.6f + Mathf.Sin(w * 9100f) * 0.4f) * Mathf.Exp(-t * 160f);
        }
    }

    /// <summary>BGM の音量を、目標の大きさへ少しずつ変える（ふわっと鳴り始め、ふわっと消える）。0 になったら止める。</summary>
    public class MusicFader : MonoBehaviour
    {
        public AudioSource Source;
        public float Target;
        public float Speed = 0.3f;

        void Update()
        {
            if (Source == null) return;
            Source.volume = Mathf.MoveTowards(Source.volume, Target, Speed * Time.unscaledDeltaTime);
            if (Target <= 0f && Source.volume <= 0f && Source.isPlaying) Source.Stop();
        }
    }
}
