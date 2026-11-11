using UnityEngine;

namespace DeskStage.App
{
    /// <summary>
    /// スタート画面の BGM（2026-11-11）。音の素材は無いので、効果音（UiSound）と同じく、コードで作る。
    /// 明るい 8 小節（ハ長調、1 分に 112 拍。約 17 秒）を、くり返し鳴らす：
    ///  ・メロディー：マリンバのような、ころんとした音
    ///  ・ベース：やわらかい低い音（1 拍目と 3 拍目）
    ///  ・和音：うすく、のばした音
    /// 終わりの音の響きを、曲の頭に重ねて作るので、くり返しのつなぎ目で音が切れない。
    /// Play でふわっと鳴り始め、Stop でふわっと消える（音量を少しずつ変える部品 MusicFader）。
    /// </summary>
    public static class AppMusic
    {
        const int SampleRate = 22050;
        const float Bpm = 112f;
        const int Bars = 8;
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
            (67, 1f), (72, 1f), (76, 1f), (79, 1f),                         // C
            (81, 1.5f), (79, 0.5f), (76, 1f), (72, 1f),                     // Am
            (77, 1f), (81, 1f), (79, 0.5f), (77, 0.5f), (76, 1f),           // F
            (74, 1f), (79, 1f), (71, 1f), (74, 1f),                         // G
            (76, 0.5f), (79, 0.5f), (84, 1f), (83, 0.5f), (81, 0.5f), (79, 1f), // C
            (81, 1f), (76, 1f), (72, 1f), (76, 1f),                         // Am
            (77, 1f), (81, 1f), (79, 1f), (71, 1f),                         // F → G
            (72, 1f), (76, 0.5f), (79, 0.5f), (84, 2f),                     // C
        };

        // 小節ごとの和音（根音と、のばす 3 つの音）
        static readonly int[][] Chords =
        {
            new[] { 48, 60, 64, 67 }, new[] { 45, 57, 60, 64 }, new[] { 41, 53, 57, 60 }, new[] { 43, 55, 59, 62 },
            new[] { 48, 60, 64, 67 }, new[] { 45, 57, 60, 64 }, new[] { 41, 53, 57, 60 }, new[] { 48, 60, 64, 67 },
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
                // ベース：1 拍目に根音、3 拍目に 5 度上（F→G の小節は、3 拍目から G に）
                var third = bar == 6 ? 43 : chord[0] + 7;
                AddNote(data, start, beat * 2f, chord[0], Bass, 0.26f);
                AddNote(data, start + beat * 2f, beat * 2f, third, Bass, 0.22f);
                // 和音：小節いっぱい、うすく
                for (var i = 1; i < chord.Length; i++) AddNote(data, start, beat * 4f, chord[i], Pad, 0.05f);
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
        static void AddNote(float[] data, float start, float duration, int note, Voice voice, float volume)
        {
            var frequency = 440f * Mathf.Pow(2f, (note - 69) / 12f);
            var first = Mathf.RoundToInt(start * SampleRate);
            var count = Mathf.RoundToInt((duration + 0.6f) * SampleRate); // のばした後の響きまで
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

        // 和音：ゆっくり立ち上がって、のびる音
        static float Pad(float f, float t, float duration)
        {
            var attack = Mathf.Min(1f, t / 0.25f);
            var release = t < duration ? 1f : Mathf.Exp(-(t - duration) * 6f);
            var w = 2f * Mathf.PI * f * t;
            return (Mathf.Sin(w) + 0.3f * Mathf.Sin(2f * w)) * attack * release;
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
