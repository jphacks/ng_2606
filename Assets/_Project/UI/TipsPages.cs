using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeskStage.UI
{
    /// <summary>
    /// ならべかたのコツの絵の一覧（Step 9）。上から順にめくって見せる。
    /// 絵は仮の画像。人が後で差し替える。P0 の判定の案 B・C の絵や、見つけにくい物の絵は Enabled で出し入れする。
    /// </summary>
    [CreateAssetMenu(menuName = "DeskStage/D/TipsPages", fileName = "TipsPages")]
    public class TipsPages : ScriptableObject
    {
        [Serializable]
        public class Page
        {
            [Tooltip("Wording のキー（絵の下に出す文）")]
            public string Key;
            public Sprite Image;
            public bool Enabled = true;
        }

        public List<Page> Pages = new List<Page>();

        /// <summary>出す絵だけを並べて返す。</summary>
        public List<Page> EnabledPages() => Pages.FindAll(p => p.Enabled);
    }
}
