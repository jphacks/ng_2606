using System;
using System.Collections.Generic;

namespace DeskStage.UI
{
    /// <summary>
    /// デバッグ表示に出す項目の登録先（Step 6 の2）。各担当が「名前と値」「切り替え」「スライダー」を登録する。
    /// 他の担当から使うには共通コード（Shared）に置く必要がある。今は D の中に置き、全員の合意後に Shared へ移す案。
    /// 同じ名前で登録し直すと上書きする。登録した側は OnDisable などで Remove する。
    /// </summary>
    public static class DebugRegistry
    {
        public enum Kind
        {
            Value,
            Toggle,
            Slider,
        }

        public class Item
        {
            public string Name;
            public Kind Kind;
            public Func<string> GetText;
            public Func<bool> GetBool;
            public Action<bool> SetBool;
            public Func<float> GetFloat;
            public Action<float> SetFloat;
            public float Min;
            public float Max;
        }

        static readonly List<Item> items = new List<Item>();

        public static IReadOnlyList<Item> Items => items;

        /// <summary>表示だけの項目。</summary>
        public static void AddValue(string name, Func<string> getText)
            => Put(new Item { Name = name, Kind = Kind.Value, GetText = getText });

        /// <summary>オン・オフの切り替え。</summary>
        public static void AddToggle(string name, Func<bool> get, Action<bool> set)
            => Put(new Item { Name = name, Kind = Kind.Toggle, GetBool = get, SetBool = set });

        /// <summary>数値のスライダー。</summary>
        public static void AddSlider(string name, float min, float max, Func<float> get, Action<float> set)
            => Put(new Item { Name = name, Kind = Kind.Slider, Min = min, Max = max, GetFloat = get, SetFloat = set });

        public static void Remove(string name) => items.RemoveAll(i => i.Name == name);

        public static void Clear() => items.Clear();

        // このプロジェクトは再生のたびに static を作り直さない設定（Domain Reload なし）なので、再生の始めに空にする
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => items.Clear();

        static void Put(Item item)
        {
            var index = items.FindIndex(i => i.Name == item.Name);
            if (index >= 0)
            {
                items[index] = item;
            }
            else
            {
                items.Add(item);
            }
        }
    }
}
