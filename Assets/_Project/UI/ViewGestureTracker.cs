using System.Collections.Generic;
using UnityEngine;

namespace DeskStage.UI
{
    /// <summary>
    /// 指の動きから視点の量を計算する。計算だけの普通のクラス（Step 4）。
    /// 1本指でなぞる → 回す量（度）。2本指でつまむ・広げる → 近づく量（m、広げると正）。
    /// 3本目以降の指は使わない。指の数が変わったときは基準を取り直し、視点が飛ばないようにする。
    /// </summary>
    public class ViewGestureTracker
    {
        public struct Output
        {
            public Vector2 OrbitDegrees;
            public float ZoomMeters;
        }

        readonly Dictionary<int, Vector2> pointers = new Dictionary<int, Vector2>();
        readonly List<int> order = new List<int>();
        float lastPinchDistance;

        public float DegreesPerScreenHeight = 180f;
        public float ZoomMetersPerScreenHeight = 0.6f;
        public bool InvertVertical;

        public int Count => order.Count;

        public void Down(int id, Vector2 position)
        {
            if (pointers.ContainsKey(id) || order.Count >= 2)
            {
                return;
            }
            pointers[id] = position;
            order.Add(id);
            ResetPinch();
        }

        public void Up(int id)
        {
            if (pointers.Remove(id))
            {
                order.Remove(id);
                ResetPinch();
            }
        }

        public void Clear()
        {
            pointers.Clear();
            order.Clear();
        }

        /// <summary>指が動いた。screenHeight は画面の高さ（ピクセル）。</summary>
        public Output Move(int id, Vector2 position, float screenHeight)
        {
            var result = new Output();
            if (!pointers.TryGetValue(id, out var previous) || screenHeight <= 0f)
            {
                return result;
            }
            pointers[id] = position;

            if (order.Count == 1)
            {
                var delta = (position - previous) / screenHeight * DegreesPerScreenHeight;
                if (InvertVertical)
                {
                    delta.y = -delta.y;
                }
                result.OrbitDegrees = delta;
            }
            else if (order.Count == 2)
            {
                var distance = PinchDistance();
                result.ZoomMeters = (distance - lastPinchDistance) / screenHeight * ZoomMetersPerScreenHeight;
                lastPinchDistance = distance;
            }
            return result;
        }

        float PinchDistance() => Vector2.Distance(pointers[order[0]], pointers[order[1]]);

        void ResetPinch()
        {
            lastPinchDistance = order.Count == 2 ? PinchDistance() : 0f;
        }
    }
}
