using System.Collections.Generic;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.App
{
    /// <summary>
    /// ワープ（ピンク）の箱のつながり（Docs/specs/warp-pairs）。2 つずつ組になり、どちらに乗っても相手の箱へ飛ぶ（行き来できる）。
    /// つながりは BoxData.warpTo（相手の箱の id）。画面に触れない計算だけ（EditMode でテストする）。
    /// </summary>
    public static class WarpPairs
    {
        static bool IsWarp(BoxData b) => b != null && b.gimmick == BoxData.GimmickWarp;

        static List<BoxData> Warps(IList<BoxData> boxes)
        {
            var list = new List<BoxData>();
            foreach (var b in boxes) if (IsWarp(b)) list.Add(b);
            return list;
        }

        static BoxData Find(IList<BoxData> boxes, string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var b in boxes) if (b.id == id) return b;
            return null;
        }

        static float Distance(BoxData a, BoxData b)
        {
            var dx = a.center[0] - b.center[0];
            var dz = a.center[2] - b.center[2];
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>壊れたつながり（相手がいない・ワープでない・自分・相手が自分を指していない）を空にする。ワープでない箱のつながりも消す。</summary>
        public static void Repair(IList<BoxData> boxes)
        {
            foreach (var b in boxes)
            {
                if (b.warpTo == null) b.warpTo = "";
                if (!IsWarp(b))
                {
                    b.warpTo = "";
                    continue;
                }
                var partner = Find(boxes, b.warpTo);
                if (partner == null || partner == b || !IsWarp(partner) || partner.warpTo != b.id)
                {
                    b.warpTo = "";
                }
            }
        }

        /// <summary>相手のいないワープの箱を、上から見た距離が近い組から順に、2 つずつつなぐ（奇数なら 1 つ余る）。</summary>
        public static void AutoPair(IList<BoxData> boxes)
        {
            Repair(boxes);
            var free = Warps(boxes).FindAll(b => b.warpTo.Length == 0);
            while (free.Count >= 2)
            {
                int bestA = 0, bestB = 1;
                var best = float.MaxValue;
                for (var i = 0; i < free.Count; i++)
                {
                    for (var j = i + 1; j < free.Count; j++)
                    {
                        var d = Distance(free[i], free[j]);
                        if (d < best)
                        {
                            best = d;
                            bestA = i;
                            bestB = j;
                        }
                    }
                }
                Link(free[bestA], free[bestB]);
                var a = free[bestA];
                var b = free[bestB];
                free.Remove(a);
                free.Remove(b);
            }
        }

        static void Link(BoxData a, BoxData b)
        {
            a.warpTo = b.id;
            b.warpTo = a.id;
        }

        /// <summary>
        /// その箱の相手を、次のワープの箱（距離の近い順。いまの相手の次。最後の次は最初）に変える。
        /// 前の相手と、新しい相手の前の相手は、余った物どうしで、つなぎ直す。
        /// </summary>
        public static void Cycle(IList<BoxData> boxes, string id)
        {
            Repair(boxes);
            var self = Find(boxes, id);
            if (!IsWarp(self)) return;
            var others = Warps(boxes);
            others.Remove(self);
            if (others.Count == 0) return;
            others.Sort((x, y) => Distance(self, x).CompareTo(Distance(self, y)));
            var current = others.FindIndex(b => b.id == self.warpTo);
            var next = others[(current + 1) % others.Count];
            if (next.id == self.warpTo) return; // 相手が 1 つしかいない

            var oldPartner = Find(boxes, self.warpTo);
            var nextOldPartner = Find(boxes, next.warpTo);
            if (oldPartner != null) oldPartner.warpTo = "";
            if (nextOldPartner != null) nextOldPartner.warpTo = "";
            Link(self, next);
            AutoPair(boxes);
        }

        /// <summary>相手の箱の id（いなければ空）。</summary>
        public static string Partner(IList<BoxData> boxes, string id)
        {
            var b = Find(boxes, id);
            return IsWarp(b) ? b.warpTo ?? "" : "";
        }

        /// <summary>ワープの箱に、1, 2, 3… の番号を付ける（並びは、箱の並びのまま）。ボタンの表示用。</summary>
        public static Dictionary<string, int> Numbers(IList<BoxData> boxes)
        {
            var numbers = new Dictionary<string, int>();
            foreach (var b in Warps(boxes)) numbers[b.id] = numbers.Count + 1;
            return numbers;
        }

        /// <summary>ワープの箱の数。</summary>
        public static int Count(IList<BoxData> boxes) => Warps(boxes).Count;
    }
}
