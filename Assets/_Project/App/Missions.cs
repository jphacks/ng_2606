using System.Collections.Generic;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.App
{
    /// <summary>お題の条件の種類（Docs/specs/home-modes の REQ-4）。</summary>
    public enum MissionKind
    {
        MinBoxes,
        MaxBoxes,
        Gimmick,
        Ramp,
        BoxHeight,
        GoalHeight,
        Steps,
        Distance,
    }

    /// <summary>お題の条件 1 つ。数（Count）か長さ（Meters）か、しかけ（Gimmick）を使う。</summary>
    public class MissionCondition
    {
        public MissionKind Kind;
        public int Count;
        public float Meters;
        public string Gimmick = "";

        /// <summary>子ども向けの文。</summary>
        public string Text
        {
            get
            {
                switch (Kind)
                {
                    case MissionKind.MinBoxes: return string.Format(AppText.MissionMinBoxes, Count);
                    case MissionKind.MaxBoxes: return string.Format(AppText.MissionMaxBoxes, Count);
                    case MissionKind.Gimmick: return string.Format(AppText.MissionGimmick, GimmickRules.Name(Gimmick));
                    case MissionKind.Ramp: return AppText.MissionRamp;
                    case MissionKind.BoxHeight: return string.Format(AppText.MissionBoxHeight, Mathf.RoundToInt(Meters * 100f));
                    case MissionKind.GoalHeight: return string.Format(AppText.MissionGoalHeight, Mathf.RoundToInt(Meters * 100f));
                    case MissionKind.Steps: return string.Format(AppText.MissionSteps, Count);
                    default: return string.Format(AppText.MissionDistance, Mathf.RoundToInt(Meters * 100f));
                }
            }
        }
    }

    /// <summary>お題 1 つ（名前と、条件 1〜2 個）。</summary>
    public class Mission
    {
        public string Id;
        public string Name;
        public MissionCondition[] Conditions;
    }

    /// <summary>お題の一覧（アプリに入れた 8 つ）。Docs/specs/home-modes の design.md「おだい」。</summary>
    public static class MissionCatalog
    {
        static MissionCondition C(MissionKind kind, int count = 0, float meters = 0f, string gimmick = "")
            => new MissionCondition { Kind = kind, Count = count, Meters = meters, Gimmick = gimmick };

        public static readonly Mission[] All =
        {
            new Mission { Id = "m01", Name = "はこ いっぱい", Conditions = new[] { C(MissionKind.MinBoxes, count: 5) } },
            new Mission { Id = "m02", Name = "みずの みち", Conditions = new[] { C(MissionKind.Gimmick, gimmick: BoxData.GimmickWater) } },
            new Mission { Id = "m03", Name = "あつい と ほっと", Conditions = new[] { C(MissionKind.Gimmick, gimmick: BoxData.GimmickFire), C(MissionKind.Gimmick, gimmick: BoxData.GimmickHeal) } },
            new Mission { Id = "m04", Name = "さかみち", Conditions = new[] { C(MissionKind.Ramp) } },
            new Mission { Id = "m05", Name = "たかい ところ", Conditions = new[] { C(MissionKind.GoalHeight, meters: 0.20f) } },
            new Mission { Id = "m06", Name = "かいだん", Conditions = new[] { C(MissionKind.Steps, count: 3) } },
            new Mission { Id = "m07", Name = "とおくの ゴール", Conditions = new[] { C(MissionKind.Distance, meters: 0.40f) } },
            new Mission { Id = "m08", Name = "すくなく つくる", Conditions = new[] { C(MissionKind.MaxBoxes, count: 3), C(MissionKind.GoalHeight, meters: 0.10f) } },
        };
    }

    /// <summary>
    /// お題の判定（画面に触れない計算。EditMode でテストする）。箱はたしかめの画面の箱（直したあと）、スタートとゴールは置いた物。
    /// </summary>
    public static class MissionRules
    {
        /// <summary>段とみなす高さの違い（m）。</summary>
        public const float StepGap = 0.02f;

        // スタート・ゴールを置くと、面から 1mm 浮かせるので、高さはその分を見込む
        const float Lift = 0.003f;

        public static bool IsMet(MissionCondition c, IReadOnlyList<BoxData> boxes, StageData stage)
        {
            boxes = boxes ?? new BoxData[0];
            switch (c.Kind)
            {
                case MissionKind.MinBoxes: return boxes.Count >= c.Count;
                case MissionKind.MaxBoxes: return boxes.Count >= 1 && boxes.Count <= c.Count;
                case MissionKind.Gimmick:
                    foreach (var b in boxes) if (b.gimmick == c.Gimmick) return true;
                    return false;
                case MissionKind.Ramp:
                    foreach (var b in boxes) if (b.ramp) return true;
                    return false;
                case MissionKind.BoxHeight:
                    foreach (var b in boxes) if (b.size != null && b.size.Length > 1 && b.size[1] >= c.Meters - 1e-4f) return true;
                    return false;
                case MissionKind.GoalHeight:
                    return GoalPlaced(stage) && stage.goal.position[1] >= c.Meters - Lift;
                case MissionKind.Steps:
                    return CountSteps(boxes) >= c.Count;
                default:
                    if (!GoalPlaced(stage) || !StartPlaced(stage)) return false;
                    var dx = stage.goal.position[0] - stage.spawn.position[0];
                    var dz = stage.goal.position[2] - stage.spawn.position[2];
                    return Mathf.Sqrt(dx * dx + dz * dz) >= c.Meters - 1e-4f;
            }
        }

        public static bool AllMet(Mission m, IReadOnlyList<BoxData> boxes, StageData stage)
        {
            if (m == null) return true;
            foreach (var c in m.Conditions)
            {
                if (!IsMet(c, boxes, stage)) return false;
            }
            return true;
        }

        /// <summary>高さのちがう段の数（坂は数えない。StepGap 以上ちがう高さを、別の段とみる）。</summary>
        public static int CountSteps(IReadOnlyList<BoxData> boxes)
        {
            var heights = new List<float>();
            foreach (var b in boxes)
            {
                if (!b.ramp && b.size != null && b.size.Length > 1) heights.Add(b.size[1]);
            }
            heights.Sort();
            var steps = 0;
            var last = float.NegativeInfinity;
            foreach (var h in heights)
            {
                if (h - last >= StepGap - 1e-4f)
                {
                    steps++;
                    last = h;
                }
            }
            return steps;
        }

        static bool GoalPlaced(StageData s) => s != null && s.goal != null && s.goal.radius > 0f && s.goal.position != null && s.goal.position.Length == 3;

        // スタートは、置くまで原点（0,0,0）のまま
        static bool StartPlaced(StageData s) => s != null && s.spawn != null && s.spawn.position != null && s.spawn.position.Length == 3
            && (s.spawn.position[0] != 0f || s.spawn.position[1] != 0f || s.spawn.position[2] != 0f);
    }

    /// <summary>お題の達成のスタンプ（この iPhone の中。PlayerPrefs）。</summary>
    public static class MissionStamps
    {
        static string Key(string id) => "mission-stamp-" + id;

        public static bool Has(string id) => PlayerPrefs.GetInt(Key(id), 0) == 1;

        public static void Mark(string id)
        {
            PlayerPrefs.SetInt(Key(id), 1);
            PlayerPrefs.Save();
        }
    }
}
