using System.Collections.Generic;
using UnityEngine;

namespace DeskStage.Gameplay
{
    /// <summary>
    /// ステージごとの最速記録（C_gameplay.md の Step 16）。PlayerPrefs の「best_＋ステージID」に秒で保存する。
    /// 並べ直してスキャンしたステージは新しいステージID になるので、記録も新しく始まる。
    /// </summary>
    public class RecordStore : MonoBehaviour
    {
        RecordBook book;

        RecordBook Book => book ??= new RecordBook(new PlayerPrefsRecordStorage());

        /// <summary>ゴールしたタイムを記録と比べる。速ければ更新して true（初めてのゴールも true）。</summary>
        public bool Submit(string stageId, float seconds) => Book.Submit(stageId, seconds);

        /// <summary>最速記録（秒）。まだなければ null。</summary>
        public float? GetBest(string stageId) => Book.GetBest(stageId);

        /// <summary>最速記録を消す（練習用シーンの確かめ用）。</summary>
        public void Clear(string stageId) => Book.Clear(stageId);
    }

    /// <summary>記録の置き場所。テストでは PlayerPrefs の代わりにメモリを使う。</summary>
    public interface IRecordStorage
    {
        bool TryGet(string key, out float value);
        void Set(string key, float value);
        void Delete(string key);
    }

    /// <summary>PlayerPrefs に保存する（アプリを起動し直しても残る）。</summary>
    public class PlayerPrefsRecordStorage : IRecordStorage
    {
        public bool TryGet(string key, out float value)
        {
            value = PlayerPrefs.GetFloat(key, 0f);
            return PlayerPrefs.HasKey(key);
        }

        public void Set(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }

    /// <summary>メモリに置く（EditMode テスト用）。</summary>
    public class MemoryRecordStorage : IRecordStorage
    {
        readonly Dictionary<string, float> values = new Dictionary<string, float>();

        public bool TryGet(string key, out float value) => values.TryGetValue(key, out value);

        public void Set(string key, float value) => values[key] = value;

        public void Delete(string key) => values.Remove(key);
    }

    /// <summary>最速記録の比べ方（計算部分）。</summary>
    public class RecordBook
    {
        readonly IRecordStorage storage;

        public RecordBook(IRecordStorage storage)
        {
            this.storage = storage;
        }

        /// <summary>PlayerPrefs のキー</summary>
        public static string Key(string stageId) => "best_" + stageId;

        public float? GetBest(string stageId)
        {
            return storage.TryGet(Key(stageId), out float best) ? best : (float?)null;
        }

        /// <summary>記録がないか、記録より速ければ更新して true。同じタイムは更新しない。</summary>
        public bool Submit(string stageId, float seconds)
        {
            var best = GetBest(stageId);
            if (best.HasValue && seconds >= best.Value)
            {
                return false;
            }
            storage.Set(Key(stageId), seconds);
            return true;
        }

        public void Clear(string stageId) => storage.Delete(Key(stageId));
    }
}
