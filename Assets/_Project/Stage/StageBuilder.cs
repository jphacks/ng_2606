using System.Collections.Generic;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>
    /// 箱の一覧から、机の板と箱のオブジェクトを StageRoot の子に作る（B の Step 5）。
    /// 箱1つにつきオブジェクト1つ。箱の id で探し、変わった箱だけを直す。
    /// </summary>
    public class StageBuilder : MonoBehaviour
    {
        [SerializeField] BoxSettings settings;

        [Tooltip("机の板の素材。空なら単色の仮の素材を使う（D の素材ができたら差し替える）")]
        [SerializeField] Material deskMaterial;

        [Tooltip("箱の素材。空なら単色の仮の素材を使う（D の素材ができたら差し替える）")]
        [SerializeField] Material boxMaterial;

        static readonly Color FallbackDeskColor = new Color(0.82f, 0.70f, 0.55f);
        static readonly Color FallbackBoxColor = new Color(0.45f, 0.65f, 0.90f);

        readonly Dictionary<string, GameObject> boxes = new Dictionary<string, GameObject>();
        GameObject desk;
        Transform builtRoot;

        /// <summary>
        /// ステージ全体を組み立てる。前に作った物は、残す箱は直し、ない箱は消す。
        /// 作り終えたら StageBuilt を出す。
        /// </summary>
        public void Build(StageData stage)
        {
            var root = EnsureStageRoot();
            if (builtRoot != root)
            {
                // StageRoot が替わったら、前の物は使わない
                Clear();
                builtRoot = root;
            }

            BuildDesk(stage.desk);

            var keep = new HashSet<string>();
            foreach (var box in stage.boxes)
            {
                keep.Add(box.id);
                UpsertBox(box);
            }

            var remove = new List<string>();
            foreach (var id in boxes.Keys)
            {
                if (!keep.Contains(id))
                {
                    remove.Add(id);
                }
            }
            foreach (var id in remove)
            {
                RemoveBox(id);
            }

            StageContext.RaiseStageBuilt();
        }

        /// <summary>箱を1つ作る。同じ id の箱がもうあれば、その箱の位置と大きさだけを直す。</summary>
        public void UpsertBox(BoxData box)
        {
            var root = EnsureStageRoot();
            if (!boxes.TryGetValue(box.id, out var go) || go == null)
            {
                go = CreateCube(box.id, root, boxMaterial, FallbackBoxColor);
                boxes[box.id] = go;
            }
            Apply(go.transform, StageLayout.Box(box));
        }

        /// <summary>箱を1つ消す。</summary>
        public void RemoveBox(string id)
        {
            if (boxes.TryGetValue(id, out var go))
            {
                boxes.Remove(id);
                if (go != null)
                {
                    Destroy(go);
                }
            }
        }

        /// <summary>箱の id からオブジェクトを探す。なければ null。</summary>
        public GameObject FindBox(string id)
        {
            return boxes.TryGetValue(id, out var go) ? go : null;
        }

        /// <summary>作った物をすべて消す。</summary>
        public void Clear()
        {
            foreach (var go in boxes.Values)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }
            boxes.Clear();
            if (desk != null)
            {
                Destroy(desk);
                desk = null;
            }
        }

        void BuildDesk(DeskData deskData)
        {
            if (desk == null)
            {
                desk = CreateCube("Desk", EnsureStageRoot(), deskMaterial, FallbackDeskColor);
            }
            var thickness = settings != null ? settings.deskThickness : 0.01f;
            Apply(desk.transform, StageLayout.Desk(deskData, thickness));
        }

        /// <summary>遊ぶときは StageRoot がないので、B が原点に作る（00_overview.md 3.1）。</summary>
        static Transform EnsureStageRoot()
        {
            if (StageContext.StageRoot == null)
            {
                StageContext.StageRoot = new GameObject("StageRoot").transform;
            }
            return StageContext.StageRoot;
        }

        static GameObject CreateCube(string name, Transform parent, Material material, Color fallbackColor)
        {
            // Cube には最初から Box Collider が付いている
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.layer = Layers.Terrain;
            go.transform.SetParent(parent, false);
            var useMaterial = material != null ? material : FallbackMaterial(fallbackColor);
            if (useMaterial != null)
            {
                go.GetComponent<Renderer>().sharedMaterial = useMaterial;
            }
            return go;
        }

        static readonly Dictionary<Color, Material> s_fallbackMaterials = new Dictionary<Color, Material>();

        static Material FallbackMaterial(Color color)
        {
            if (!s_fallbackMaterials.TryGetValue(color, out var material) || material == null)
            {
                // ビルドではシェーダーが省かれて見つからないことがある。そのときは Cube の元の素材のままにする
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    return null;
                }
                material = new Material(shader) { color = color };
                s_fallbackMaterials[color] = material;
            }
            return material;
        }

        static void Apply(Transform t, LocalPlacement p)
        {
            t.localPosition = p.position;
            t.localRotation = p.rotation;
            t.localScale = p.scale;
        }
    }
}
