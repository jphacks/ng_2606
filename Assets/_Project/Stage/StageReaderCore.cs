using System;
using System.Collections.Generic;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>StageReaderCore.Read の結果。</summary>
    public class StageReadResult
    {
        /// <summary>箱を作れたか。false なら Reason に理由。</summary>
        public bool Ok;
        public string Reason = "";

        /// <summary>机の天面の幅（X）と奥行き（Z）（m）。</summary>
        public Vector2 DeskSize;

        /// <summary>整えた箱（ステージ座標、m）。</summary>
        public List<BoxData> Boxes = new List<BoxData>();

        /// <summary>範囲を外れたなどで、箱にしなかった物の数。</summary>
        public int Skipped;

        /// <summary>基準の紙が写っていたか（false なら大きさは推定）。</summary>
        public bool PaperFound;

        /// <summary>長さの基準に使った物（ログ用）。</summary>
        public string ScaleReference = "";
    }

    /// <summary>
    /// Claude の答えの JSON から、ステージの箱を作る計算（B の Step 7）。
    /// MonoBehaviour から切り離して、EditMode テストで確かめられるようにする。
    /// 直し方は Docs/LlmScanFormat.md の「ステージ座標への直し方」。
    /// </summary>
    public class StageReaderCore
    {
        // 範囲（Docs/LlmScanFormat.md、cm）
        const float MinSideCm = 1f;
        const float MaxSideCm = 60f;
        const float MaxHeightCm = 40f;
        const float MinDeskCm = 20f;
        const float MaxDeskCm = 300f;
        const int MaxBlocks = 100;

        // ステージの机の大きさの範囲（Docs/StageFormat.md、m）
        const float MinDeskM = 0.2f;
        const float MaxDeskM = 3f;

        readonly float minBoxHeight;
        readonly float heightMergeThreshold;
        readonly float gapCloseThreshold;
        readonly float deskMargin;

        /// <summary>手順書の初期値で作る（テスト用）。</summary>
        public StageReaderCore() : this(0.005f, 0.008f, 0.016f, 0.10f)
        {
        }

        public StageReaderCore(BoxSettings settings)
            : this(settings.minBoxHeight, settings.heightMergeThreshold, settings.gapCloseThreshold, settings.deskMargin)
        {
        }

        public StageReaderCore(float minBoxHeight, float heightMergeThreshold, float gapCloseThreshold, float deskMargin)
        {
            this.minBoxHeight = minBoxHeight;
            this.heightMergeThreshold = heightMergeThreshold;
            this.gapCloseThreshold = gapCloseThreshold;
            this.deskMargin = deskMargin;
        }

        public StageReadResult Read(string json)
        {
            var result = new StageReadResult();

            LlmScanAnswer answer;
            try
            {
                answer = JsonUtility.FromJson<LlmScanAnswer>(json);
            }
            catch (ArgumentException e)
            {
                return Fail(result, $"答えが JSON として読めません：{e.Message}");
            }
            if (answer == null)
            {
                return Fail(result, "答えが空です");
            }
            result.PaperFound = answer.paperFound;
            result.ScaleReference = answer.scaleReference ?? "";

            // 1. 範囲に入る物だけを残す
            var blocks = new List<LlmBlock>();
            foreach (var b in answer.blocks ?? Array.Empty<LlmBlock>())
            {
                if (blocks.Count < MaxBlocks && IsValid(b))
                {
                    blocks.Add(b);
                }
                else
                {
                    result.Skipped++;
                }
            }
            if (blocks.Count == 0)
            {
                return Fail(result, "答えに箱にできる物がありません");
            }

            // 2. 机の範囲（答えの座標、cm）を決める
            Rect deskCm;
            if (answer.desk != null && InRange(answer.desk.widthCm, MinDeskCm, MaxDeskCm) && InRange(answer.desk.depthCm, MinDeskCm, MaxDeskCm))
            {
                // 紙の左手前の角＝机の左手前の角
                deskCm = new Rect(0f, 0f, answer.desk.widthCm, answer.desk.depthCm);
            }
            else
            {
                // 机が分からないときは、物を囲む長方形に余白を付ける
                deskCm = Enclose(blocks);
                var m = deskMargin * 100f;
                deskCm = Rect.MinMaxRect(deskCm.xMin - m, deskCm.yMin - m, deskCm.xMax + m, deskCm.yMax + m);
            }

            var deskW = Mathf.Clamp(deskCm.width / 100f, MinDeskM, MaxDeskM);
            var deskD = Mathf.Clamp(deskCm.height / 100f, MinDeskM, MaxDeskM);
            result.DeskSize = new Vector2(deskW, deskD);
            var centerCm = deskCm.center;

            // 3. 物ごとに箱を作る（ステージ座標：机の中心が原点、m）
            var raw = new List<BoxData>();
            foreach (var b in blocks)
            {
                var height = b.heightCm / 100f;
                if (height < minBoxHeight)
                {
                    result.Skipped++;
                    continue;
                }
                var x = (b.centerXCm - centerCm.x) / 100f;
                var z = (b.centerZCm - centerCm.y) / 100f;
                if (Mathf.Abs(x) > deskW / 2f || Mathf.Abs(z) > deskD / 2f)
                {
                    // 中心が机の外の物は入れない
                    result.Skipped++;
                    continue;
                }
                raw.Add(new BoxData
                {
                    id = $"box-{raw.Count + 1:00}",
                    center = new[] { x, height / 2f, z },
                    size = new[] { b.widthCm / 100f, height, b.depthCm / 100f },
                    // 答えは上から見て反時計回り。Unity の Y 軸まわりは上から見て時計回り
                    yawDegrees = Mathf.Repeat(360f - b.yawDegrees, 360f),
                    source = BoxData.SourceAuto,
                });
            }
            if (raw.Count == 0)
            {
                return Fail(result, "机の中に箱にできる物がありません");
            }

            // 4. 高さをそろえ、すき間をなくす（Step 7 の4）
            result.Boxes = BoxRefiner.Refine(raw, heightMergeThreshold, gapCloseThreshold);
            result.Ok = true;
            return result;
        }

        static bool IsValid(LlmBlock b)
        {
            return b != null
                && IsFinite(b.centerXCm) && IsFinite(b.centerZCm) && IsFinite(b.yawDegrees)
                && InRange(b.widthCm, MinSideCm, MaxSideCm)
                && InRange(b.depthCm, MinSideCm, MaxSideCm)
                && InRange(b.heightCm, 0f, MaxHeightCm) && b.heightCm > 0f;
        }

        static bool InRange(float v, float min, float max) => IsFinite(v) && v >= min && v <= max;

        static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);

        /// <summary>回した物も含めて、上から見た物を全部囲む長方形（cm）。</summary>
        static Rect Enclose(List<LlmBlock> blocks)
        {
            float xMin = float.MaxValue, zMin = float.MaxValue, xMax = float.MinValue, zMax = float.MinValue;
            foreach (var b in blocks)
            {
                var rad = b.yawDegrees * Mathf.Deg2Rad;
                var cos = Mathf.Abs(Mathf.Cos(rad));
                var sin = Mathf.Abs(Mathf.Sin(rad));
                var hx = (b.widthCm * cos + b.depthCm * sin) / 2f;
                var hz = (b.widthCm * sin + b.depthCm * cos) / 2f;
                xMin = Mathf.Min(xMin, b.centerXCm - hx);
                xMax = Mathf.Max(xMax, b.centerXCm + hx);
                zMin = Mathf.Min(zMin, b.centerZCm - hz);
                zMax = Mathf.Max(zMax, b.centerZCm + hz);
            }
            return Rect.MinMaxRect(xMin, zMin, xMax, zMax);
        }

        static StageReadResult Fail(StageReadResult result, string reason)
        {
            result.Ok = false;
            result.Reason = reason;
            return result;
        }
    }
}
