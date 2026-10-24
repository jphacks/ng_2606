using System.Collections.Generic;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.UI
{
    /// <summary>
    /// 場面（Scan、Check、Play）に合わせて、StageRoot の下の机の板（Desk）と箱（box-…）の素材を差し替え、
    /// 遊ぶ画面では箱ごとに色を付け、背景の色を変える（Step 7）。
    /// 本来は B の StageBuilder が ViewLook の素材を選ぶ予定。受け渡しの形が決まるまで、D がここで差し替える。
    /// </summary>
    public class StageLook : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] ViewLook look;

        AppMode appliedMode = (AppMode)(-1);
        string appliedSelected;
        MaterialPropertyBlock block;
        readonly List<Renderer> boxRenderers = new List<Renderer>();
        readonly List<Rect> footprints = new List<Rect>();

        public void SetLook(ViewLook value) => look = value;

        void OnEnable()
        {
            StageContext.StageBuilt += Apply;
            Apply();
        }

        void OnDisable() => StageContext.StageBuilt -= Apply;

        void Update()
        {
            // Mode は D の ScreenRouter が書く。Mode か選んでいる箱が変わったら見た目を直す
            if (StageContext.Mode != appliedMode || ScanContext.SelectedBoxId != appliedSelected)
            {
                Apply();
            }
        }

        public void Apply()
        {
            appliedMode = StageContext.Mode;
            appliedSelected = ScanContext.SelectedBoxId;
            if (look == null)
            {
                return;
            }
            if (appliedMode == AppMode.Play && Camera.main != null)
            {
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
                Camera.main.backgroundColor = look.PlayBackground;
            }

            var root = StageContext.StageRoot;
            if (root == null)
            {
                return;
            }
            block ??= new MaterialPropertyBlock();
            boxRenderers.Clear();
            footprints.Clear();
            foreach (Transform child in root)
            {
                var r = child.GetComponent<Renderer>();
                if (r == null)
                {
                    continue;
                }
                if (child.name == "Desk")
                {
                    SetMaterial(r, look.DeskFor(appliedMode));
                    r.SetPropertyBlock(null);
                }
                else if (child.name.StartsWith("box-"))
                {
                    // Step 11：補助で選んだ箱は、太い黄色のふちで光らせる（遊ぶ画面では選ばない）
                    var selected = appliedMode != AppMode.Play && child.name == appliedSelected;
                    SetMaterial(r, selected ? look.BoxSelected : look.BoxFor(appliedMode));
                    boxRenderers.Add(r);
                    var b = r.bounds;
                    footprints.Add(Rect.MinMaxRect(b.min.x, b.min.z, b.max.x, b.max.z));
                }
            }

            // 遊ぶ画面だけ、箱ごとに色を変える（スキャン中は半透明の同じ色で本物と見比べる）
            var colors = appliedMode == AppMode.Play && look.BoxPalette.Length > 0
                ? BoxColorAssigner.Assign(footprints, look.BoxPalette.Length, look.NeighborMargin)
                : null;
            for (int i = 0; i < boxRenderers.Count; i++)
            {
                if (colors == null)
                {
                    boxRenderers[i].SetPropertyBlock(null);
                    continue;
                }
                block.Clear();
                block.SetColor(BaseColorId, look.BoxPalette[colors[i]]);
                boxRenderers[i].SetPropertyBlock(block);
            }
        }

        static void SetMaterial(Renderer r, Material m)
        {
            if (m != null && r.sharedMaterial != m)
            {
                r.sharedMaterial = m;
            }
        }
    }
}
