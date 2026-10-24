using System;
using System.IO;
using UnityEngine;

namespace DeskStage.Merge
{
    /// <summary>
    /// ステージの背景に使う写真（背景専用に撮った、1 枚の JPEG）の保存・読み込み・消去。
    /// 端末の中のフォルダ（Backdrop）に、background.jpg として置く。無ければ、ステージに背景は出ない。
    /// ファイルの扱いだけで、Unity の画面には触れない。EditMode テストで確かめる。
    /// </summary>
    public class BackdropStore
    {
        public const string FileName = "background.jpg";

        readonly string dir;

        public BackdropStore(string dir)
        {
            this.dir = dir;
        }

        public static BackdropStore AtDefaultPlace() =>
            new BackdropStore(Path.Combine(Application.persistentDataPath, "Backdrop"));

        string FilePath => Path.Combine(dir, FileName);

        public bool Exists => File.Exists(FilePath);

        /// <summary>背景の写真（JPEG）。無ければ null。</summary>
        public byte[] Load()
        {
            try
            {
                return Exists ? File.ReadAllBytes(FilePath) : null;
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

        /// <summary>背景の写真を保存する（前の物は置き換える）。保存できたら true。</summary>
        public bool Save(byte[] jpeg)
        {
            if (jpeg == null || jpeg.Length == 0)
            {
                return false;
            }
            try
            {
                Directory.CreateDirectory(dir);
                File.WriteAllBytes(FilePath, jpeg);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        /// <summary>背景の写真を消す。消せた、または、もともと無かったら true。</summary>
        public bool Clear()
        {
            try
            {
                if (Exists)
                {
                    File.Delete(FilePath);
                }
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }
    }
}
