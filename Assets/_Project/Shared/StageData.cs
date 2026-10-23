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
