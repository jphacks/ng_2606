using UnityEngine;

namespace DeskStage.App
{
    /// <summary>
    /// 効果音（押した音、できた音、失敗の音、シャッターの音）。音の素材は無いので、コードで作る短い音。
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
        }

        const int SampleRate = 22050;

        static AudioSource source;
        static AudioClip click;
        static AudioClip success;
        static AudioClip error;
        static AudioClip shutter;

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
            switch (cue)
            {
                case Cue.Success: clip = success ??= Make(new[] { 660f, 880f, 1320f }, 0.11f, 0.5f); break;
                case Cue.Error: clip = error ??= Make(new[] { 330f, 247f }, 0.16f, 0.5f); break;
                case Cue.Shutter: clip = shutter ??= MakeNoiseClick(); break;
                default: clip = click ??= Make(new[] { 900f }, 0.06f, 0.45f); break;
            }
            source.PlayOneShot(clip, 0.8f);
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
