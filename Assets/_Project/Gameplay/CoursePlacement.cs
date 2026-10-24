using System.Collections.Generic;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Gameplay
{
    /// <summary>
    /// コースの物（スタート・ゴール・チェックポイント）をステージデータに書く決まり（C_gameplay.md の Step 12）。
    /// 光線の当たり方の判定と、ステージデータの書き換えだけを持つ。EditMode でテストする。
    /// CoursePlaced の理由は、D が子ども向けの文に置き換えるための決まった記号で返す（Docs/CoursePlacement.md）。
    /// </summary>
    public static class CoursePlacement
    {
        /// <summary>机か箱に当たらなかった</summary>
        public const string ReasonNotOnTerrain = "not-on-terrain";
        /// <summary>箱の側面など、上向きでない面に当たった</summary>
        public const string ReasonNotTop = "not-top";
        /// <summary>チェックポイントがもう5個ある</summary>
        public const string ReasonCheckpointFull = "checkpoint-full";
        /// <summary>既にあるチェックポイントの近くをタップしたので、それを消した</summary>
        public const string ReasonCheckpointRemoved = "checkpoint-removed";
        /// <summary>スタートからゴールへ、壁を通らないと行けないかもしれない</summary>
        public const string ReasonMayBeUnreachable = "may-be-unreachable";

        /// <summary>上向きの面とみなす、面の向きの上の成分の下限（約30度まで）</summary>
        public const float MinUpNormal = 0.866f;

        /// <summary>既にあるチェックポイントのこの距離以内をタップしたら、それを消す（m）</summary>
        public const float CheckpointRemoveDistance = 0.03f;

        /// <summary>置いた点を面から浮かせる高さ（m）</summary>
        public const float LiftAboveSurface = 0.001f;

        /// <summary>ゴールとチェックポイントの、触れる範囲の半径（m）</summary>
        public const float DefaultRadius = 0.03f;

        /// <summary>当たった面に置けるか。置けなければ理由の記号を返す（置けるなら null）。</summary>
        public static string CheckSurface(bool hit, Vector3 normal)
        {
            if (!hit) return ReasonNotOnTerrain;
            if (normal.y < MinUpNormal) return ReasonNotTop;
            return null;
        }

        /// <summary>水平な向き（ステージ座標）を、上から見た角度 0 以上 360 未満にする。</summary>
        public static float YawFromDirection(Vector3 localDirection)
        {
            float yaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
            return Mathf.Repeat(yaw, 360f);
        }

        /// <summary>
        /// ステージデータに書く。point は当たった位置（ステージ座標）。スタートとゴールは1つずつで、もう一度置くと動く。
        /// チェックポイントは最大5個。既にあるものの 3cm 以内なら、それを消す。
        /// </summary>
        public static (PlacementResult result, string reason) Apply(StageData stage, CourseItem item, Vector3 point, float startYawDegrees)
        {
            Vector3 lifted = point + Vector3.up * LiftAboveSurface;
            switch (item)
            {
                case CourseItem.Start:
                    stage.spawn.position = Vec3.From(lifted);
                    stage.spawn.yawDegrees = startYawDegrees;
                    return (PlacementResult.Placed, "");

                case CourseItem.Goal:
                    stage.goal.position = Vec3.From(lifted);
                    if (stage.goal.radius <= 0f) stage.goal.radius = DefaultRadius;
                    return (PlacementResult.Placed, "");

                default:
                    var list = new List<GoalData>(stage.checkpoints);
                    int near = list.FindIndex(cp => Vector3.Distance(Vec3.To(cp.position), lifted) <= CheckpointRemoveDistance);
                    if (near >= 0)
                    {
                        list.RemoveAt(near);
                        stage.checkpoints = list.ToArray();
                        return (PlacementResult.Placed, ReasonCheckpointRemoved);
                    }
                    if (list.Count >= StageData.MaxCheckpoints)
                    {
                        return (PlacementResult.Rejected, ReasonCheckpointFull);
                    }
                    list.Add(new GoalData { position = Vec3.From(lifted), radius = DefaultRadius });
                    stage.checkpoints = list.ToArray();
                    return (PlacementResult.Placed, "");
            }
        }
    }
}
