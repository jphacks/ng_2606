using System;
using UnityEngine;

namespace DeskStage.Shared
{
    /// <summary>
    /// stage.json と同じ形のデータ（00_overview.md 4.2）。
    /// Unity 標準の JsonUtility で読み書きできるよう、座標は長さ3の配列で持つ。
    /// </summary>
    [Serializable]
    public class StageData
    {
        public const int CurrentVersion = 2;

        /// <summary>チェックポイントの上限。</summary>
        public const int MaxCheckpoints = 5;

        public int version = CurrentVersion;

        /// <summary>ステージID。保存先のフォルダ名と同じ。</summary>
        public string id = "";

        /// <summary>表示名。</summary>
        public string name = "";

        /// <summary>スキャンした日時（ISO 8601）。</summary>
        public string createdAt = "";

        /// <summary>机の天面の長方形の大きさ。中心が原点。</summary>
        public DeskData desk = new DeskData();

        /// <summary>箱の一覧。</summary>
        public BoxData[] boxes = Array.Empty<BoxData>();

        /// <summary>スタート地点。</summary>
        public SpawnData spawn = new SpawnData();

        /// <summary>ゴール。</summary>
        public GoalData goal = new GoalData();

        /// <summary>チェックポイント（最大 <see cref="MaxCheckpoints"/> 個）。</summary>
        public GoalData[] checkpoints = Array.Empty<GoalData>();

        /// <summary>机の天面からどれだけ下で落下とするか（m）。</summary>
        public float fallDepth = 0.10f;

        /// <summary>しばり（コースを作る人が付ける、遊び方の条件）。古い保存データには無いので、読み込むと しばり無し になる。</summary>
        public CourseRules rules = new CourseRules();
    }

    /// <summary>しばり（Docs/specs/course-rules）。最初の値は、どれも しばり無し。</summary>
    [Serializable]
    public class CourseRules
    {
        /// <summary>ジャンプの回数の上限。-1 はむせいげん、0 はきんし、1 以上はその回数まで（2 段ジャンプも 1 回と数える）。</summary>
        public int maxJumps = -1;

        /// <summary>true なら、2 段ジャンプ（空中でのジャンプ）を使えない。</summary>
        public bool noDoubleJump;

        /// <summary>true なら、ダメージを 1 回受けたら、ハートが残っていてもゲームオーバー。</summary>
        public bool oneHit;

        /// <summary>制限時間（秒）。0 はなし。</summary>
        public float timeLimitSeconds;

        public CourseRules Clone() => new CourseRules
        {
            maxJumps = maxJumps,
            noDoubleJump = noDoubleJump,
            oneHit = oneHit,
            timeLimitSeconds = timeLimitSeconds,
        };
    }

    /// <summary>机の天面の長方形の大きさ（m）。</summary>
    [Serializable]
    public class DeskData
    {
        public float width;
        public float depth;
    }

    /// <summary>箱1つ（00_overview.md 4.2）。</summary>
    [Serializable]
    public class BoxData
    {
        /// <summary>スキャンで作った箱。</summary>
        public const string SourceAuto = "auto";

        /// <summary>補助で足した・直した箱。</summary>
        public const string SourceManual = "manual";

        /// <summary>ステージの中で重ならない名前。例：box-01</summary>
        public string id = "";

        /// <summary>箱の中心。高さは「箱の高さの半分」。</summary>
        public float[] center = Vec3.Zero();

        /// <summary>幅・高さ・奥行き。高さ＝上面から机の天面まで。</summary>
        public float[] size = Vec3.Zero();

        /// <summary>上から見た回転（度）。Y軸まわりだけ。</summary>
        public float yawDegrees;

        /// <summary>auto か manual。</summary>
        public string source = SourceAuto;

        /// <summary>表面の画像のファイル名。なければ空。</summary>
        public string texture = "";

        /// <summary>水の箱。上面が一面、青い折り紙のとき。遊ぶと、箱の上で、決まった向きに少し流される。</summary>
        public const string GimmickWater = "water";

        /// <summary>炎の箱。上面が一面、赤い折り紙のとき。乗るとハートが 1 つ減る。</summary>
        public const string GimmickFire = "fire";

        /// <summary>回復の箱。上面が一面、緑の折り紙のとき。乗っている間、ハートが回復する。</summary>
        public const string GimmickHeal = "heal";

        /// <summary>ジャンプ台の箱。上面が一面、黄色い折り紙のとき。乗ると自動で高く跳ね上がる。</summary>
        public const string GimmickJumpPad = "jumppad";

        /// <summary>敵の出る箱。上面が一面、紫の折り紙のとき。7 秒ごとに敵が 1 体出て、当たるとハートが 1 つ減る。</summary>
        public const string GimmickEnemy = "enemy";

        /// <summary>ワープの箱。上面が一面、ピンクの折り紙のとき。踏むと、机の上のどこかランダムな所へ飛ぶ。</summary>
        public const string GimmickWarp = "warp";

        /// <summary>
        /// 箱のしかけ（water / fire / heal / jumppad / enemy / warp）。なければ空。撮影の写真から、上面が一面その色のときに決まる（Merge の GimmickDetector）。
        /// 古い保存データには無いので、読み込むと空（しかけ無し）になる。
        /// </summary>
        public string gimmick = "";

        /// <summary>
        /// 水の箱の、流れる向き。0 は「箱の id から決まる向き」（古い保存データ・まだ選んでいない箱）。
        /// 1〜4 は、箱の座標の +x、+z、-x、-z（遊ぶ人が、たしかめの画面で選ぶ）。
        /// </summary>
        public int flow;

        /// <summary>
        /// 坂（斜めに立てかけた板など）。true のとき、箱ではなく、くさび形になる。
        /// 上から見た形は size[0]（のぼる向きの長さ）× size[2]（横はば）で、箱の座標の +x の方へのぼる。
        /// -x の端は机の高さ 0、+x の端は size[1]（高さ）。向きは yawDegrees で決める。
        /// 古い保存データには無いので、読み込むと false（ふつうの箱）になる。
        /// </summary>
        public bool ramp;
    }

    /// <summary>スタート地点。</summary>
    [Serializable]
    public class SpawnData
    {
        public float[] position = Vec3.Zero();
        public float yawDegrees;
    }

    /// <summary>ゴールとチェックポイント（位置と、触れたと判定する半径）。</summary>
    [Serializable]
    public class GoalData
    {
        public float[] position = Vec3.Zero();
        public float radius;
    }

    /// <summary>
    /// 長さ3の配列と Vector3 の変換。
    /// JsonUtility が Vector3 をそのまま扱えないので、保存は配列、計算は Vector3 と使い分ける。
    /// </summary>
    public static class Vec3
    {
        /// <summary>すべて 0 の長さ3の配列を作る。</summary>
        public static float[] Zero() => new float[3];

        /// <summary>Vector3 を長さ3の配列にする。</summary>
        public static float[] From(Vector3 value) => new[] { value.x, value.y, value.z };

        /// <summary>長さ3の配列を Vector3 にする。長さが違えば Vector3.zero を返す。</summary>
        public static Vector3 To(float[] value)
        {
            if (value == null || value.Length != 3)
            {
                return Vector3.zero;
            }
            return new Vector3(value[0], value[1], value[2]);
        }
    }
}
