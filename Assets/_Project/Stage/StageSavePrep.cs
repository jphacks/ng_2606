using System;
using System.Collections.Generic;
using System.Globalization;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>
    /// 保存する stage.json の中身を作る計算（B の Step 11 の1〜3）。
    /// ・写真の方式では、箱は最初から机の中心を原点にして作っている（StageReader）ので、位置はずらさない
    /// ・位置と大きさを 1mm、回転を 0.1 度に丸める
    /// ・ステージ ID は「stage-」＋日時、表示名は依頼から受け取る
    /// </summary>
    public static class StageSavePrep
    {
        public static StageData Prepare(StageData current, IReadOnlyList<BoxData> workingBoxes, string displayName, DateTimeOffset now)
        {
            var src = current ?? new StageData();
            var boxes = new List<BoxData>();
            foreach (var b in workingBoxes.Count > 0 ? workingBoxes : (IReadOnlyList<BoxData>)(src.boxes ?? new BoxData[0]))
            {
                boxes.Add(new BoxData
                {
                    id = b.id,
                    center = RoundMm(b.center),
                    size = RoundMm(b.size),
                    yawDegrees = RoundYaw(b.yawDegrees),
                    source = b.source,
                    texture = b.texture ?? "",
                });
            }

            var checkpoints = new List<GoalData>();
            foreach (var c in src.checkpoints ?? new GoalData[0])
            {
                checkpoints.Add(new GoalData { position = RoundMm(c.position), radius = RoundMm(c.radius) });
            }

            var id = "stage-" + now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            return new StageData
            {
                id = id,
                name = string.IsNullOrWhiteSpace(displayName) ? id : displayName.Trim(),
                createdAt = now.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture),
                desk = new DeskData { width = RoundMm(src.desk?.width ?? 0f), depth = RoundMm(src.desk?.depth ?? 0f) },
                boxes = boxes.ToArray(),
                spawn = new SpawnData
                {
                    position = RoundMm(src.spawn?.position ?? Vec3.Zero()),
                    yawDegrees = RoundYaw(src.spawn?.yawDegrees ?? 0f),
                },
                goal = new GoalData
                {
                    position = RoundMm(src.goal?.position ?? Vec3.Zero()),
                    radius = RoundMm(src.goal?.radius ?? 0f),
                },
                checkpoints = checkpoints.ToArray(),
                fallDepth = src.fallDepth > 0f ? src.fallDepth : 0.10f,
            };
        }

        static float RoundMm(float v) => Mathf.Round(v * 1000f) / 1000f;

        static float[] RoundMm(float[] v)
        {
            var src = v ?? Vec3.Zero();
            var r = new float[src.Length];
            for (var i = 0; i < src.Length; i++)
            {
                r[i] = RoundMm(src[i]);
            }
            return r;
        }

        /// <summary>0.1 度に丸め、0 以上 360 未満にする。</summary>
        static float RoundYaw(float deg)
        {
            var r = Mathf.Round(Mathf.Repeat(deg, 360f) * 10f) / 10f;
            return r >= 360f ? 0f : r;
        }
    }
}
