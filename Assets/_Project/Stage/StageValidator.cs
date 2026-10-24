using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>
    /// stage.json の中身を確かめる（B の Step 10）。範囲は Docs/StageFormat.md の「範囲の一覧」。
    /// 読み込むときと保存する前に使う。1つでも当てはまらなければ「読み込めない」とし、理由を返す。
    /// </summary>
    public static class StageValidator
    {
        const float HeightCenterTolerance = 0.001f;

        public static bool Validate(StageData s, string folderId, out string reason)
        {
            reason = Check(s, folderId);
            return reason.Length == 0;
        }

        static string Check(StageData s, string folderId)
        {
            if (s == null)
            {
                return "中身がありません";
            }
            if (s.version == 1)
            {
                return "古い形式";
            }
            if (s.version != StageData.CurrentVersion)
            {
                return $"形式の版が違います（{s.version}）";
            }
            if (string.IsNullOrEmpty(s.id) || (folderId != null && s.id != folderId))
            {
                return $"ID がフォルダ名と違います（{s.id}）";
            }
            if (s.desk == null || !InRange(s.desk.width, 0.2f, 3f) || !InRange(s.desk.depth, 0.2f, 3f))
            {
                return "机の大きさが範囲外です";
            }

            var boxes = s.boxes ?? new BoxData[0];
            if (boxes.Length > 100)
            {
                return "箱が多すぎます";
            }
            foreach (var b in boxes)
            {
                if (b == null || !IsVec3(b.center) || !IsVec3(b.size))
                {
                    return "箱の座標の形が違います";
                }
                if (!InRange(b.size[0], 0.01f, 0.6f) || !InRange(b.size[2], 0.01f, 0.6f) || !InRange(b.size[1], 0.005f, 0.4f))
                {
                    return $"箱の大きさが範囲外です（{b.id}）";
                }
                if (Mathf.Abs(b.center[1] - b.size[1] / 2f) > HeightCenterTolerance)
                {
                    return $"箱の中心の高さが合いません（{b.id}）";
                }
                if (!(b.yawDegrees >= 0f && b.yawDegrees < 360f))
                {
                    return $"箱の回転が範囲外です（{b.id}）";
                }
            }

            if (s.spawn == null || !IsVec3(s.spawn.position))
            {
                return "スタートがありません";
            }
            if (s.goal == null || !IsVec3(s.goal.position) || !InRange(s.goal.radius, 0.01f, 0.05f))
            {
                return "ゴールがありません";
            }

            var checkpoints = s.checkpoints ?? new GoalData[0];
            if (checkpoints.Length > StageData.MaxCheckpoints)
            {
                return "チェックポイントが多すぎます";
            }
            foreach (var c in checkpoints)
            {
                if (c == null || !IsVec3(c.position) || !InRange(c.radius, 0.01f, 0.05f))
                {
                    return "チェックポイントの形が違います";
                }
            }

            if (!(s.fallDepth > 0f))
            {
                return "落下の深さが範囲外です";
            }
            return "";
        }

        static bool IsVec3(float[] v)
        {
            if (v == null || v.Length != 3)
            {
                return false;
            }
            foreach (var x in v)
            {
                if (float.IsNaN(x) || float.IsInfinity(x))
                {
                    return false;
                }
            }
            return true;
        }

        static bool InRange(float v, float min, float max) => v >= min && v <= max;
    }
}
