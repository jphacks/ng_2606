using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DeskStage.Merge
{
    /// <summary>保存した、写真のキャラクター 1 人分の情報。</summary>
    [Serializable]
    public class CharacterInfo
    {
        /// <summary>フォルダ名と同じ。</summary>
        public string id = "";

        /// <summary>表示名。</summary>
        public string name = "";

        /// <summary>作った日時（ISO 8601）。</summary>
        public string createdAt = "";

        /// <summary>切り抜きの横 ÷ 縦。</summary>
        public float aspect = 1f;
    }

    /// <summary>
    /// 写真のキャラクター（人形の切り抜き）の保存・一覧・削除と、遊ぶときに使うキャラの選択（キャラクターを写真で作る）。
    /// 端末の中のフォルダ（Characters）に、キャラごとのフォルダ（character.json と sprite.png）として置く。
    /// 選んでいるキャラの id は、同じフォルダの selected.txt。空なら、いつものキャラ（カプセル）。
    /// ファイルの扱いだけで、Unity の画面には触れない。EditMode テストで確かめる。
    /// </summary>
    public class CharacterStore
    {
        public const string InfoFile = "character.json";
        public const string SpriteFile = "sprite.png";
        const string SelectedFile = "selected.txt";

        readonly string rootDir;

        public CharacterStore(string rootDir)
        {
            this.rootDir = rootDir;
        }

        public static CharacterStore AtDefaultPlace() =>
            new CharacterStore(Path.Combine(Application.persistentDataPath, "Characters"));

        /// <summary>保存したキャラの一覧。新しい順。</summary>
        public List<CharacterInfo> List()
        {
            var result = new List<CharacterInfo>();
            if (!Directory.Exists(rootDir))
            {
                return result;
            }
            foreach (var dir in Directory.GetDirectories(rootDir))
            {
                var info = ReadInfo(dir);
                if (info != null && File.Exists(Path.Combine(dir, SpriteFile)))
                {
                    result.Add(info);
                }
            }
            result.Sort((a, b) => string.CompareOrdinal(b.createdAt, a.createdAt));
            return result;
        }

        /// <summary>キャラを足す。png は切り抜き（透明つき）の PNG。足したキャラの情報を返す。失敗したら null。</summary>
        public CharacterInfo Add(string name, byte[] png, float aspect, DateTimeOffset now)
        {
            if (png == null || png.Length == 0)
            {
                return null;
            }
            try
            {
                var id = NewId(now);
                var dir = Path.Combine(rootDir, id);
                Directory.CreateDirectory(dir);
                File.WriteAllBytes(Path.Combine(dir, SpriteFile), png);
                var info = new CharacterInfo
                {
                    id = id,
                    name = string.IsNullOrWhiteSpace(name) ? DefaultName() : name.Trim(),
                    createdAt = now.ToString("o"),
                    aspect = aspect > 0f ? aspect : 1f,
                };
                File.WriteAllText(Path.Combine(dir, InfoFile), JsonUtility.ToJson(info, true));
                return info;
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        /// <summary>切り抜きの PNG を読む。無ければ null。</summary>
        public byte[] LoadPng(string id)
        {
            if (!IsSafeId(id))
            {
                return null;
            }
            var path = Path.Combine(rootDir, id, SpriteFile);
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }

        public CharacterInfo Find(string id)
        {
            if (!IsSafeId(id))
            {
                return null;
            }
            return ReadInfo(Path.Combine(rootDir, id));
        }

        /// <summary>キャラを消す。選んでいたら、選びを外す。消せたら true。</summary>
        public bool Delete(string id)
        {
            if (!IsSafeId(id))
            {
                return false;
            }
            var dir = Path.Combine(rootDir, id);
            if (!Directory.Exists(dir))
            {
                return false;
            }
            try
            {
                Directory.Delete(dir, true);
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            if (SelectedId == id)
            {
                SelectedId = "";
            }
            return true;
        }

        /// <summary>遊ぶときに使うキャラの id。空なら、いつものキャラ。消えたキャラの id は、空として扱う。</summary>
        public string SelectedId
        {
            get
            {
                var path = Path.Combine(rootDir, SelectedFile);
                if (!File.Exists(path))
                {
                    return "";
                }
                var id = File.ReadAllText(path).Trim();
                return IsSafeId(id) && File.Exists(Path.Combine(rootDir, id, SpriteFile)) ? id : "";
            }
            set
            {
                try
                {
                    Directory.CreateDirectory(rootDir);
                    File.WriteAllText(Path.Combine(rootDir, SelectedFile), value ?? "");
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }

        string NewId(DateTimeOffset now)
        {
            var baseId = $"char-{now:yyyyMMdd-HHmmss}";
            var id = baseId;
            for (var n = 2; Directory.Exists(Path.Combine(rootDir, id)); n++)
            {
                id = $"{baseId}-{n}";
            }
            return id;
        }

        string DefaultName() => $"キャラ {List().Count + 1}";

        static CharacterInfo ReadInfo(string dir)
        {
            var path = Path.Combine(dir, InfoFile);
            if (!File.Exists(path))
            {
                return null;
            }
            try
            {
                var info = JsonUtility.FromJson<CharacterInfo>(File.ReadAllText(path));
                if (info == null || string.IsNullOrEmpty(info.id) || info.id != Path.GetFileName(dir))
                {
                    return null;
                }
                return info;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>フォルダ名に使える id か（英数字と - と _ だけ）。</summary>
        public static bool IsSafeId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 64)
            {
                return false;
            }
            foreach (var c in id)
            {
                if (!(char.IsLetterOrDigit(c) && c < 128) && c != '-' && c != '_')
                {
                    return false;
                }
            }
            return true;
        }
    }
}
