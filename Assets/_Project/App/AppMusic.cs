using UnityEngine;

namespace DeskStage.App
{
    /// <summary>
    /// BGM（2026-11-11）。メニューの画面の曲と、遊んでいるときの曲（2026-11-15）。音の素材は無いので、効果音（UiSound）と同じく、コードで作る。
    /// 曲は、データ（Song：テンポ、メロディー、和音、どの音で鳴らすか）と、鳴らし方（Samples）に分けてある。
    /// メニューでは Songs[Current]、遊んでいるときは Songs[GameSong] を鳴らす（Play(Track)。曲を替えると、前の曲はふわっと消える）。どの曲も 8 小節を、くり返し鳴らす。
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

        public enum Lead { Marimba, Brass, Xylophone, Square, Bell }

        /// <summary>どの BGM か：メニューの画面の曲と、遊んでいるときの曲。</summary>
        public enum Track { Menu, Game }

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
            /// <summary>メロディーに重ねる音（無ければ重ねない）。HarmonyShift は、メロディーからずらす高さ（半音の数。12 で 1 オクターブ上）。</summary>
            public Lead? Harmony;
            public float HarmonyVolume;
            public int HarmonyShift;
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
            new Song
            {
                // ぴょんぴょん はねる（遊んでいるときの曲。2026-11-15：ぼうけん てくてく・わくわく ダッシュ・ゴー！ゴー！パレードなどと聞き比べて、えらんだ）
                // ト長調。木琴に、1 オクターブ上のベルをうすく重ねる。木をたたく「トントン」のリズム
                Name = "ぴょんぴょん はねる", Bpm = 136f, Lead = Lead.Xylophone, Bass = BassStyle.Bounce, Drums = Drums.Blocks,
                Harmony = Lead.Bell, HarmonyVolume = 0.08f, HarmonyShift = 12,
                Melody = new (int, float)[]
                {
                    (74, .5f), (79, .5f), (83, .5f), (-1, .5f), (81, .5f), (79, .5f), (78, 1f),
                    (76, .5f), (78, .5f), (79, .5f), (-1, .5f), (81, 1f), (74, 1f),
                    (72, .5f), (76, .5f), (79, .5f), (-1, .5f), (84, .5f), (83, .5f), (81, 1f),
                    (79, .5f), (78, .5f), (76, .5f), (78, .5f), (81, 2f),
                    (74, .5f), (79, .5f), (83, .5f), (-1, .5f), (86, .5f), (83, .5f), (79, 1f),
                    (81, .5f), (83, .5f), (84, .5f), (-1, .5f), (88, 1f), (84, 1f),
                    (83, .5f), (81, .5f), (79, .5f), (76, .5f), (78, .5f), (81, .5f), (86, 1f),
                    (84, .5f), (83, .5f), (81, .5f), (78, .5f), (79, 2f),
                },
                Chords = new[]
                {
                    new[] { 43, 59, 62, 67 }, new[] { 52, 59, 64, 67 }, new[] { 48, 60, 64, 67 }, new[] { 50, 57, 62, 66 },
                    new[] { 43, 59, 62, 67 }, new[] { 48, 60, 64, 67 }, new[] { 45, 57, 60, 64 }, new[] { 43, 59, 62, 67 },
                },
            },
        };

        /// <summary>メニューの画面で鳴らす曲（Songs の番号）。</summary>
        public const int Current = 0;

        /// <summary>遊んでいるときに鳴らす曲（Songs の番号）。</summary>
        public const int GameSong = 1;

        /// <summary>遊んでいるときの BGM の大きさ（2026-11-15：大きくしすぎないように、メニューの曲より小さく。足音や効果音が聞こえるように）。</summary>
        public const float GameVolume = 0.16f;

        static readonly AudioSource[] sources = new AudioSource[2];
        static readonly MusicFader[] faders = new MusicFader[2];

        /// <summary>メニューの BGM を鳴らし始める（もう鳴っていれば、そのまま）。遊んでいるときの曲は、ふわっと消す。</summary>
        public static void Play() => Play(Track.Menu);

        /// <summary>BGM を鳴らし始める（もう鳴っていれば、そのまま）。ほかの曲は、ふわっと消す。</summary>
        public static void Play(Track track)
        {
            if (!Enabled || !Application.isPlaying) return;
            var i = (int)track;
            Ensure(track);
            if (sources[i] == null) return;
            if (!sources[i].isPlaying)
            {
                sources[i].volume = 0f;
                sources[i].Play();
            }
            faders[i].Target = track == Track.Game ? GameVolume : Volume;
            var other = 1 - i;
            if (faders[other] != null) faders[other].Target = 0f;
        }

        /// <summary>BGM を、ふわっと消す（どの曲も）。</summary>
        public static void Stop()
        {
            foreach (var f in faders)
            {
                if (f != null) f.Target = 0f;
            }
        }

        static void Ensure(Track track)
        {
            var i = (int)track;
            if (sources[i] != null) return;
            var data = Samples(Songs[track == Track.Game ? GameSong : Current]);
            var clip = AudioClip.Create(track == Track.Game ? "game-bgm" : "title-bgm", data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            var go = new GameObject("AppMusic-" + track) { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(go);
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            var fader = go.AddComponent<MusicFader>();
            fader.Source = source;
            fader.Speed = (track == Track.Game ? GameVolume : Volume) / FadeSeconds;
            sources[i] = source;
            faders[i] = fader;
        }

        /// <summary>メニューの曲の音のデータ（くり返して鳴らす 1 回分）。</summary>
        public static float[] Samples() => Samples(Songs[Current]);

        /// <summary>曲の音のデータを作る（くり返して鳴らす 1 回分）。</summary>
        public static float[] Samples(Song song)
        {
            var beat = 60f / song.Bpm;
            var length = Mathf.RoundToInt(SampleRate * beat * 4f * Bars);
            var data = new float[length];

            // メロディー
            var lead = VoiceOf(song.Lead, out var leadVolume, out var leadTail);
            Voice harmony = null;
            var harmonyTail = 0f;
            if (song.Harmony.HasValue) harmony = VoiceOf(song.Harmony.Value, out _, out harmonyTail);
            var at = 0f;
            foreach (var (note, beats) in song.Melody)
            {
                if (note >= 0)
                {
                    AddNote(data, at, beats * beat, note, lead, leadVolume, leadTail);
                    // ラッパは、1 オクターブ下を重ねて、厚みを出す
                    if (song.Lead == Lead.Brass) AddNote(data, at, beats * beat, note - 12, Brass, leadVolume * 0.45f, leadTail);
                    if (harmony != null) AddNote(data, at, beats * beat, note + song.HarmonyShift, harmony, song.HarmonyVolume, harmonyTail);
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

        // メロディーの音の種類と、その大きさ・響きの長さ
        static Voice VoiceOf(Lead kind, out float volume, out float tail)
        {
            switch (kind)
            {
                case Lead.Brass: volume = 0.26f; tail = 0.25f; return Brass;
                case Lead.Xylophone: volume = 0.34f; tail = 0.35f; return Xylophone;
                case Lead.Square: volume = 0.16f; tail = 0.15f; return Square;
                case Lead.Bell: volume = 0.2f; tail = 0.8f; return Bell;
                default: volume = 0.32f; tail = 0.6f; return Marimba;
            }
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

        // ベル：きらっと鳴って、長く残る音（ずれた高さの倍音を、少し重ねる）
        static float Bell(float f, float t, float duration)
        {
            var w = 2f * Mathf.PI * f * t;
            return (Mathf.Sin(w) + 0.4f * Mathf.Sin(2.76f * w) * Mathf.Exp(-t * 6f) + 0.2f * Mathf.Sin(5.4f * w) * Mathf.Exp(-t * 12f))
                   * Mathf.Min(1f, t / 0.002f) * Mathf.Exp(-t * 3f);
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
