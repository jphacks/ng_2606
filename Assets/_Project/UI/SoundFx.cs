using UnityEngine;

namespace DeskStage.UI
{
    /// <summary>
    /// 短い効果音（Step 8：箱が生える、机が決まる）。音の素材は人が後で用意する（Step 17）ので、
    /// それまではその場で作った短い音を鳴らす。素材を Inspector に入れたら、そちらを使う。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SoundFx : MonoBehaviour
    {
        [SerializeField] AudioClip pop;
        [SerializeField] AudioClip chime;

        AudioSource source;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            if (pop == null)
            {
                pop = Tone("pop", 0.08f, 660f, 990f);
            }
            if (chime == null)
            {
                chime = Tone("chime", 0.35f, 784f, 1175f);
            }
        }

        public void PlayPop() => source.PlayOneShot(pop, 0.6f);

        public void PlayChime() => source.PlayOneShot(chime, 0.6f);

        // 高さが from から to へ上がる、だんだん小さくなる音
        static AudioClip Tone(string name, float seconds, float from, float to)
        {
            const int rate = 44100;
            var count = Mathf.CeilToInt(seconds * rate);
            var data = new float[count];
            var phase = 0f;
            for (int i = 0; i < count; i++)
            {
                var t = (float)i / count;
                phase += 2f * Mathf.PI * Mathf.Lerp(from, to, t) / rate;
                data[i] = Mathf.Sin(phase) * (1f - t) * (1f - t) * 0.5f;
            }
            var clip = AudioClip.Create(name, count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
