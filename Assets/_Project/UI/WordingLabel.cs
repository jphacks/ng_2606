using TMPro;
using UnityEngine;

namespace DeskStage.UI
{
    /// <summary>
    /// 同じオブジェクトの文字を、Wording のキーの文にする。
    /// 画面の文はすべてこれを通して出す（Step 3）。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class WordingLabel : MonoBehaviour
    {
        [SerializeField] Wording wording;
        [SerializeField] string key;

        public string Key => key;

        void Awake() => Apply();

        public void Set(Wording table, string newKey)
        {
            wording = table;
            key = newKey;
            Apply();
        }

        void Apply()
        {
            if (wording != null)
            {
                GetComponent<TMP_Text>().text = wording.Get(key);
            }
        }
    }
}
