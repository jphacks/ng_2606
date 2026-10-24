using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeskStage.UI
{
    /// <summary>
    /// 画面の文の表（文言のアセット）。中身は Docs/Wording.md と同じにする。
    /// 表を直したら、メニュー「DeskStage/D/文言の表を読み込む」で読み直す。
    /// </summary>
    [CreateAssetMenu(menuName = "DeskStage/D/Wording", fileName = "Wording")]
    public class Wording : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string Key;
            public string Text;
        }

        [SerializeField] List<Entry> entries = new List<Entry>();

        Dictionary<string, string> lookup;

        public IReadOnlyList<Entry> Entries => entries;

        /// <summary>キーの文を返す。ないときはキーを括弧付きで返し、抜けに気づけるようにする。</summary>
        public string Get(string key)
        {
            if (lookup == null)
            {
                BuildLookup();
            }
            return lookup.TryGetValue(key, out var text) ? text : "[" + key + "]";
        }

        public bool Has(string key)
        {
            if (lookup == null)
            {
                BuildLookup();
            }
            return lookup.ContainsKey(key);
        }

        /// <summary>エディタ用スクリプトが表を書き込む。</summary>
        public void SetEntries(List<Entry> value)
        {
            entries = value;
            lookup = null;
        }

        void OnEnable() => lookup = null;

        void BuildLookup()
        {
            lookup = new Dictionary<string, string>();
            foreach (var e in entries)
            {
                lookup[e.Key] = e.Text;
            }
        }
    }
}
