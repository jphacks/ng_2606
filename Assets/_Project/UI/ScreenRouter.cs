using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.UI
{
    /// <summary>
    /// 一度に1つの画面だけを表示し、切り替えたら StageContext.Mode を書く。
    /// screens は ScreenId の並び順（Title、List、Tips、Scan、Check、Play、Clear）に入れる。
    /// </summary>
    public class ScreenRouter : MonoBehaviour
    {
        [SerializeField] GameObject[] screens = new GameObject[7];
        [SerializeField] ScreenId first = ScreenId.Title;

        public ScreenId Current { get; private set; }

        /// <summary>画面を切り替えたとき。</summary>
        public event System.Action<ScreenId> ScreenChanged;

        void Start() => Show(first);

        public void Show(ScreenId id)
        {
            for (int i = 0; i < screens.Length; i++)
            {
                if (screens[i] != null)
                {
                    screens[i].SetActive(i == (int)id);
                }
            }
            Current = id;
            StageContext.Mode = ScreenModeMap.ToMode(id);
            ScreenChanged?.Invoke(id);
        }

        /// <summary>UnityEvent（ボタンの OnClick）から呼ぶ用。ScreenId の番号で指定する。</summary>
        public void ShowByIndex(int index) => Show((ScreenId)index);

        /// <summary>エディタ用スクリプトが画面を登録する。</summary>
        public void SetScreens(GameObject[] value) => screens = value;
    }
}
