using UnityEngine;

namespace DeskStage.App
{
    /// <summary>
    /// スタート画面の BGM（2026-11-11）。音の素材は無いので、効果音（UiSound）と同じく、コードで作る。
    /// 曲は、データ（Song：テンポ、メロディー、和音、どの音で鳴らすか）と、鳴らし方（Samples）に分けてある。
    /// 鳴らすのは Songs[Current]。どの曲も 8 小節を、くり返し鳴らす。
    /// 終わりの音の響きを、曲の頭に重ねて作るので、くり返しのつなぎ目で音が切れない。
    /// Play でふわっと鳴り始め、Stop でふわっと消える（音量を少しずつ変える部品 MusicFader）。
    /// </summary>
    public static class AppMusic
    {
        const int SampleRate = 22050;
        const float FadeSeconds = 0.8f;

        /// <summary>どの曲も、8 小節（4 拍子）。</summary>
        public const int Bars = 8;

        /// <summary>BGM の大きさ（効果音より小さく）。</summary>
        public const float Volume = 0.28f;

        /// <summary>false にすると、鳴らさない（確認用の画像を作るときなど）。</summary>
        public static bool Enabled = true;

        public enum Lead { Marimba, Brass, Xylophone, Square }

        public enum BassStyle
        {
            Bounce,   // 8 分音符で、根音と 1 オクターブ上を交互に（はずむ）
            March,    // 4 分音符で、根音と 5 度上を交互に（行進）
            Running,  // 8 分音符で、走るように（根音、根音、オクターブ…）
            Staccato, // 4 分音符を、短く切る（ぽん、ぽん）
        }

        public enum Drums
        {
            Light,  // 1・3 拍目に軽い太鼓、8 分音符ごとに小さな「チッ」、裏拍に短い和音
            March,  // 1・3 拍目に太鼓、2・4 拍目に小太鼓、最後の小節の終わりに連打
            Blocks, // 1 拍目に軽い太鼓、2・4 拍目に木をたたく「トントン」
            Drive,  // 4 つ打ちの太鼓、2・4 拍目に小太鼓、8 分音符ごとに「チッ」
        }

        public class Song
        {
            public string Name;
            public float Bpm;
            /// <summary>(音の番号, 拍の長さ)。音の番号は、真ん中のド（C4）を 60 とする数（MIDI）。-1 は休み。合わせて 32 拍。</summary>
            public (int note, float beats)[] Melody;
            /// <summary>小節ごとの和音（根音と、3 つの音）。</summary>
            public int[][] Chords;
            public Lead Lead;
            public BassStyle Bass;
            public Drums Drums;
        }

        /// <summary>
        /// 曲。今は「つみき トントン」（2026-11-11：はずむ曲・ぼうけんの はじまり・よーい どん と聞き比べて、えらんだ）。
        /// 曲を変えるときは、ここに足して、Current を変える（ラッパ、ピコピコ音、行進や 4 つ打ちのリズムも、鳴らせる）。
        /// </summary>
        public static readonly Song[] Songs =
        {
            new Song
            {
                // つみき トントン（ヘ長調。木琴の、ぽんぽん跳ねるメロディーと、木をたたく「トントン」）
                Name = "つみき トントン", Bpm = 132f, Lead = Lead.Xylophone, Bass = BassStyle.Staccato, Drums = Drums.Blocks,
                Melody = new (int, float)[]
                {
                    (72, .5f), (72, .5f), (77, .5f), (-1, .5f), (81, .5f), (79, .5f), (77, 1f),
                    (74, .5f), (77, .5f), (81, .5f), (-1, .5f), (86, 1f), (81, 1f),
                    (70, .5f), (74, .5f), (77, .5f), (-1, .5f), (82, .5f), (81, .5f), (79, 1f),
                    (72, .5f), (76, .5f), (79, .5f), (84, .5f), (-1, .5f), (79, .5f), (84, 1f),
                    (81, .5f), (81, .5f), (84, .5f), (-1, .5f), (81, .5f), (77, .5f), (72, 1f),
                    (86, .5f), (84, .5f), (81, .5f), (-1, .5f), (77, .5f), (81, .5f), (86, 1f),
                    (82, .5f), (81, .5f), (79, .5f), (77, .5f), (79, .5f), (81, .5f), (82, 1f),
                    (84, 1f), (76, .5f), (79, .5f), (84, 1f), (76, .5f), (79, .5f), // 最後は、頭のドへもどるきっかけ
                },
                Chords = new[]
                {
                    new[] { 41, 57, 60, 65 }, new[] { 38, 57, 62, 65 }, new[] { 46, 58, 62, 65 }, new[] { 48, 55, 60, 64 },
                    new[] { 41, 57, 60, 65 }, new[] { 38, 57, 62, 65 }, new[] { 43, 55, 58, 62 }, new[] { 48, 55, 60, 64 },
                },
            },
        };

        /// <summary>鳴らす曲（Songs の番号）。</summary>
        public const int Current = 0;

        static AudioClip clip;
        static AudioSource source;
        static MusicFader fader;

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

        /// <summary>今の曲の音のデータ（くり返して鳴らす 1 回分）。</summary>
        public static float[] Samples() => Samples(Songs[Current]);

        /// <summary>曲の音のデータを作る（くり返して鳴らす 1 回分）。</summary>
        public static float[] Samples(Song song)
        {
            var beat = 60f / song.Bpm;
            var length = Mathf.RoundToInt(SampleRate * beat * 4f * Bars);
            var data = new float[length];

            // メロディー
            Voice lead;
            float leadVolume, leadTail;
            switch (song.Lead)
            {
                case Lead.Brass: lead = Brass; leadVolume = 0.26f; leadTail = 0.25f; break;
                case Lead.Xylophone: lead = Xylophone; leadVolume = 0.34f; leadTail = 0.35f; break;
                case Lead.Square: lead = Square; leadVolume = 0.16f; leadTail = 0.15f; break;
                default: lead = Marimba; leadVolume = 0.32f; leadTail = 0.6f; break;
            }
            var at = 0f;
            foreach (var (note, beats) in song.Melody)
            {
                if (note >= 0)
                {
                    AddNote(data, at, beats * beat, note, lead, leadVolume, leadTail);
                    // ラッパは、1 オクターブ下を重ねて、厚みを出す
                    if (song.Lead == Lead.Brass) AddNote(data, at, beats * beat, note - 12, Brass, leadVolume * 0.45f, leadTail);
                }
                at += beats * beat;
            }

            for (var bar = 0; bar < Bars; bar++)
            {
                var start = bar * 4f * beat;
                var chord = song.Chords[bar];
                AddBass(data, song.Bass, start, beat, chord[0]);
                AddDrums(data, song.Drums, start, beat, chord, bar == Bars - 1);
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

        static void AddBass(float[] data, BassStyle style, float start, float beat, int root)
        {
            switch (style)
            {
                case BassStyle.March:
                    for (var q = 0; q < 4; q++) AddNote(data, start + q * beat, beat * 0.8f, root + (q % 2 == 1 ? 7 : 0), Bass, q % 2 == 0 ? 0.26f : 0.2f, 0.25f);
                    break;
                case BassStyle.Running:
                    var pattern = new[] { 0, 0, 12, 0, 0, 12, 0, 12 };
                    for (var e = 0; e < 8; e++) AddNote(data, start + e * beat * 0.5f, beat * 0.4f, root + pattern[e], Bass, e % 2 == 0 ? 0.24f : 0.18f, 0.2f);
                    break;
                case BassStyle.Staccato:
                    for (var q = 0; q < 4; q++) AddNote(data, start + q * beat, beat * 0.3f, root + (q == 2 ? 7 : 0), Bass, 0.26f, 0.15f);
                    break;
                default:
                    for (var e = 0; e < 8; e++) AddNote(data, start + e * beat * 0.5f, beat * 0.45f, root + (e % 2 == 1 ? 12 : 0), Bass, e % 2 == 0 ? 0.24f : 0.16f, 0.25f);
                    break;
            }
        }

        static void AddDrums(float[] data, Drums style, float start, float beat, int[] chord, bool lastBar)
        {
            switch (style)
            {
                case Drums.March:
                    AddNote(data, start, 0.12f, 0, Kick, 0.34f, 0.1f);
                    AddNote(data, start + beat * 2f, 0.12f, 0, Kick, 0.3f, 0.1f);
                    AddNote(data, start + beat, 0.1f, 0, Snare, 0.2f, 0.1f);
                    AddNote(data, start + beat * 3f, 0.1f, 0, Snare, 0.2f, 0.1f);
                    if (lastBar)
                    {
                        // 最後の小節の 4 拍目：小太鼓の連打で、頭へもどる勢いをつける
                        for (var s = 1; s < 4; s++) AddNote(data, start + beat * 3f + s * beat * 0.25f, 0.06f, 0, Snare, 0.14f + s * 0.03f, 0.08f);
                    }
                    for (var e = 0; e < 4; e++) AddChord(data, start + e * beat + beat * 0.5f, beat, chord, 0.035f); // 裏拍を、うすく
                    break;
                case Drums.Blocks:
                    AddNote(data, start, 0.12f, 0, Kick, 0.28f, 0.1f);
                    AddNote(data, start + beat * 2f, 0.12f, 0, Kick, 0.22f, 0.1f);
                    foreach (var b in new[] { 1f, 1.5f, 3f, 3.5f }) AddNote(data, start + beat * b, 0.05f, b % 1f == 0f ? 79 : 84, Woodblock, 0.22f, 0.05f);
                    for (var e = 0; e < 4; e++) AddChord(data, start + e * beat + beat * 0.5f, beat, chord, 0.04f);
                    break;
                case Drums.Drive:
                    for (var q = 0; q < 4; q++) AddNote(data, start + q * beat, 0.12f, 0, Kick, 0.3f, 0.1f);
                    AddNote(data, start + beat, 0.1f, 0, Snare, 0.18f, 0.1f);
                    AddNote(data, start + beat * 3f, 0.1f, 0, Snare, 0.18f, 0.1f);
                    for (var e = 0; e < 8; e++) AddNote(data, start + e * beat * 0.5f, 0.03f, 0, Tick, e % 2 == 1 ? 0.08f : 0.05f, 0.02f);
                    for (var e = 0; e < 4; e++) AddChord(data, start + e * beat + beat * 0.5f, beat, chord, 0.04f);
                    break;
                default:
                    for (var e = 0; e < 8; e++)
                    {
                        var t = start + e * beat * 0.5f;
                        if (e % 2 == 1) AddChord(data, t, beat, chord, 0.06f);
                        AddNote(data, t, 0.03f, 0, Tick, e % 2 == 1 ? 0.07f : 0.04f, 0.02f);
                    }
                    AddNote(data, start, 0.12f, 0, Kick, 0.32f, 0.1f);
                    AddNote(data, start + beat * 2f, 0.12f, 0, Kick, 0.26f, 0.1f);
                    break;
            }
        }

        // 裏拍の和音：根音より上の 3 つの音を、短く「ちゃっ」
        static void AddChord(float[] data, float at, float beat, int[] chord, float volume)
        {
            for (var i = 1; i < chord.Length; i++) AddNote(data, at, beat * 0.2f, chord[i], Stab, volume, 0.2f);
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

        // ラッパ：少しゆっくり立ち上がり、のばしている間は鳴り続ける、明るい音（倍音を多めに）
        static float Brass(float f, float t, float duration)
        {
            var attack = Mathf.Min(1f, t / 0.025f);
            var release = t < duration * 0.9f ? 1f : Mathf.Exp(-(t - duration * 0.9f) * 18f);
            var w = 2f * Mathf.PI * f * t;
            var bright = 0.5f + 0.5f * attack; // 出だしは少しこもり、すぐ明るく
            return (Mathf.Sin(w) + 0.5f * bright * Mathf.Sin(2f * w) + 0.3f * bright * Mathf.Sin(3f * w) + 0.15f * bright * Mathf.Sin(4f * w)) * attack * release * 0.7f;
        }

        // 木琴：硬く、すぐ消える音（高い倍音が、少し鳴る）
        static float Xylophone(float f, float t, float duration)
        {
            var attack = Mathf.Min(1f, t / 0.002f);
            var w = 2f * Mathf.PI * f * t;
            return (Mathf.Sin(w) * Mathf.Exp(-t * 9f) + 0.35f * Mathf.Sin(3f * w) * Mathf.Exp(-t * 30f)) * attack;
        }

        // ピコピコ音：四角い波を、奇数の倍音を少しだけ重ねて、角の丸い音で作る
        static float Square(float f, float t, float duration)
        {
            var attack = Mathf.Min(1f, t / 0.003f);
            var release = t < duration * 0.85f ? 1f : Mathf.Exp(-(t - duration * 0.85f) * 40f);
            var w = 2f * Mathf.PI * f * t;
            var wave = Mathf.Sin(w) + Mathf.Sin(3f * w) / 3f + Mathf.Sin(5f * w) / 5f + Mathf.Sin(7f * w) / 7f;
            return wave * attack * release;
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

        // 小太鼓：「タン」（ざらつきは、決まった並びの数で作るので、毎回同じ音）
        static float Snare(float f, float t, float duration)
        {
            var i = Mathf.FloorToInt(t * SampleRate);
            var noise = Mathf.Sin(i * 12.9898f + 78.233f) * 43758.5453f;
            noise = (noise - Mathf.Floor(noise)) * 2f - 1f;
            var body = Mathf.Sin(2f * Mathf.PI * 190f * t) * Mathf.Exp(-t * 35f);
            return (noise * 0.6f * Mathf.Exp(-t * 30f) + body * 0.5f) * Mathf.Min(1f, t / 0.001f);
        }

        // 木をたたく「トン」：高めの、とても短い音
        static float Woodblock(float f, float t, float duration)
        {
            var w = 2f * Mathf.PI * f * 2f * t;
            return (Mathf.Sin(w) + 0.3f * Mathf.Sin(2.7f * w)) * Mathf.Exp(-t * 55f) * Mathf.Min(1f, t / 0.001f);
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
