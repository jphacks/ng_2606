using System.Collections.Generic;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>
    /// 読み取りの補助の計算（B の Step 9）：箱を足す・動かす・大きさを変える・回す・消す・もどす。
    /// MonoBehaviour から切り離して、EditMode テストで確かめられるようにする。
    /// 決まり：補助で足した箱・変えた箱は manual。位置と大きさは 5mm 単位、回転は 15 度ごとの向きに 5 度以内なら吸い付け。
    /// 操作の前の箱の一覧全体を「もどす用の記録」に積み、最大20回分残す。
    /// </summary>
    public class BoxAssistCore
    {
        public struct Settings
        {
            public float addBoxSize;
            public float step;
            public float rotateSnapStep;
            public float rotateSnapTolerance;
            public float minHeight;
            public float maxHeight;
            public float minWidth;
            public float maxWidth;
            public int undoLimit;

            /// <summary>手順書の初期値（BoxSettings と同じ）。</summary>
            public static Settings Default => new Settings
            {
                addBoxSize = 0.05f, step = 0.005f, rotateSnapStep = 15f, rotateSnapTolerance = 5f,
                minHeight = 0.005f, maxHeight = 0.40f, minWidth = 0.01f, maxWidth = 0.60f, undoLimit = 20,
            };

            public static Settings From(BoxSettings s) => new Settings
            {
                addBoxSize = s.addBoxSize, step = s.moveResizeStep, rotateSnapStep = s.rotateSnapStep,
                rotateSnapTolerance = s.rotateSnapTolerance, minHeight = s.assistMinHeight, maxHeight = s.assistMaxHeight,
                minWidth = s.assistMinWidth, maxWidth = s.assistMaxWidth, undoLimit = s.undoLimit,
            };
        }

        readonly Settings settings;
        readonly LinkedList<List<BoxData>> undo = new LinkedList<List<BoxData>>();
        // 回すときの、吸い付ける前の角度（少しずつ回しても吸い付きから抜け出せるように）
        readonly Dictionary<string, float> rawYaw = new Dictionary<string, float>();

        public BoxAssistCore(Settings settings)
        {
            this.settings = settings;
        }

        public int UndoCount => undo.Count;

        /// <summary>
        /// 一辺 addBoxSize の箱を足す。position は机の天面の上の点（m、ステージ座標）。
        /// baseHeight は、その点の下にある箱の上面の高さ（机なら 0）。箱の上なら積む（その上面から上に addBoxSize）。
        /// </summary>
        public BoxData Add(List<BoxData> boxes, Vector2 positionXZ, float baseHeight, Vector2 deskSize)
        {
            Remember(boxes);
            var size = settings.addBoxSize;
            var height = Mathf.Clamp(Snap(baseHeight + size), settings.minHeight, settings.maxHeight);
            var half = size / 2f;
            var x = ClampToDesk(Snap(positionXZ.x), half, deskSize.x);
            var z = ClampToDesk(Snap(positionXZ.y), half, deskSize.y);
            var box = new BoxData
            {
                id = NewId(boxes),
                center = new[] { x, height / 2f, z },
                size = new[] { size, height, size },
                yawDegrees = 0f,
                source = BoxData.SourceManual,
            };
            boxes.Add(box);
            return box;
        }

        /// <summary>机の面に沿って動かす。机の外には出ない。</summary>
        public BoxData Move(List<BoxData> boxes, string id, Vector2 deltaXZ, Vector2 deskSize)
        {
            var i = boxes.FindIndex(b => b.id == id);
            if (i < 0)
            {
                return null;
            }
            Remember(boxes);
            var box = Copy(boxes[i]);
            var halfX = box.size[0] / 2f;
            var halfZ = box.size[2] / 2f;
            box.center[0] = ClampToDesk(Snap(box.center[0] + deltaXZ.x), halfX, deskSize.x);
            box.center[2] = ClampToDesk(Snap(box.center[2] + deltaXZ.y), halfZ, deskSize.y);
            box.source = BoxData.SourceManual;
            boxes[i] = box;
            return box;
        }

        /// <summary>幅・奥行きは中心を変えずに、高さは底を机に付けたまま変える。範囲で止まる。</summary>
        public BoxData Resize(List<BoxData> boxes, string id, Vector3 delta)
        {
            var i = boxes.FindIndex(b => b.id == id);
            if (i < 0)
            {
                return null;
            }
            Remember(boxes);
            var box = Copy(boxes[i]);
            box.size[0] = Mathf.Clamp(Snap(box.size[0] + delta.x), settings.minWidth, settings.maxWidth);
            box.size[1] = Mathf.Clamp(Snap(box.size[1] + delta.y), settings.minHeight, settings.maxHeight);
            box.size[2] = Mathf.Clamp(Snap(box.size[2] + delta.z), settings.minWidth, settings.maxWidth);
            box.center[1] = box.size[1] / 2f;
            box.source = BoxData.SourceManual;
            boxes[i] = box;
            return box;
        }

        /// <summary>上から見た向きだけ回す。15 度ごとの向きに 5 度以内なら吸い付ける。</summary>
        public BoxData Rotate(List<BoxData> boxes, string id, float degrees)
        {
            var i = boxes.FindIndex(b => b.id == id);
            if (i < 0)
            {
                return null;
            }
            Remember(boxes);
            var box = Copy(boxes[i]);
            var raw = (rawYaw.TryGetValue(id, out var r) ? r : box.yawDegrees) + degrees;
            rawYaw[id] = raw;
            box.yawDegrees = SnapYaw(raw);
            box.source = BoxData.SourceManual;
            boxes[i] = box;
            return box;
        }

        /// <summary>箱を消す。消せたら true。</summary>
        public bool Delete(List<BoxData> boxes, string id)
        {
            var i = boxes.FindIndex(b => b.id == id);
            if (i < 0)
            {
                return false;
            }
            Remember(boxes);
            boxes.RemoveAt(i);
            rawYaw.Remove(id);
            return true;
        }

        /// <summary>1つ前の箱の一覧に戻す。戻せたら true。</summary>
        public bool Undo(List<BoxData> boxes)
        {
            if (undo.Count == 0)
            {
                return false;
            }
            var last = undo.Last.Value;
            undo.RemoveLast();
            boxes.Clear();
            boxes.AddRange(last);
            rawYaw.Clear();
            return true;
        }

        /// <summary>もどす用の記録を消す（もういちど撮るとき）。</summary>
        public void ClearHistory()
        {
            undo.Clear();
            rawYaw.Clear();
        }

        public float SnapYaw(float degrees)
        {
            var yaw = Mathf.Repeat(degrees, 360f);
            if (settings.rotateSnapStep > 0f)
            {
                var nearest = Mathf.Round(yaw / settings.rotateSnapStep) * settings.rotateSnapStep;
                if (Mathf.Abs(yaw - nearest) <= settings.rotateSnapTolerance)
                {
                    yaw = Mathf.Repeat(nearest, 360f);
                }
            }
            return yaw;
        }

        void Remember(List<BoxData> boxes)
        {
            var copy = new List<BoxData>(boxes.Count);
            foreach (var b in boxes)
            {
                copy.Add(Copy(b));
            }
            undo.AddLast(copy);
            while (undo.Count > Mathf.Max(1, settings.undoLimit))
            {
                undo.RemoveFirst();
            }
        }

        float Snap(float v) => settings.step > 0f ? Mathf.Round(v / settings.step) * settings.step : v;

        /// <summary>中心を、箱が机からはみ出さない範囲に収める。</summary>
        static float ClampToDesk(float center, float half, float deskLength)
        {
            if (deskLength <= 0f)
            {
                return center;
            }
            var limit = Mathf.Max(0f, deskLength / 2f - half);
            return Mathf.Clamp(center, -limit, limit);
        }

        static string NewId(List<BoxData> boxes)
        {
            for (var n = 1; ; n++)
            {
                var id = $"box-{n:00}";
                if (!boxes.Exists(b => b.id == id))
                {
                    return id;
                }
            }
        }

        static BoxData Copy(BoxData b)
        {
            return new BoxData
            {
                id = b.id,
                center = (float[])b.center.Clone(),
                size = (float[])b.size.Clone(),
                yawDegrees = b.yawDegrees,
                source = b.source,
                texture = b.texture,
                gimmick = b.gimmick,
            };
        }
    }
}
