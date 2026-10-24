using System.Collections.Generic;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.UI
{
    /// <summary>
    /// スキャン中の3Dの演出（Step 8）。StageRoot の子に置く。
    /// ・A の上面（ScanContext.Surfaces）を ScanSurface の素材で光らせる（ScanView レイヤー）
    /// ・机が決まったら、机の長方形のふちを1秒でなぞるように光らせる
    /// ・箱が足されたら、机から「ぽん」と伸びる箱を0.3秒出す。B の箱のオブジェクトがあれば、その間は隠しておく
    /// </summary>
    public class ScanEffects : MonoBehaviour
    {
        public const float TraceSeconds = 1f;
        public const float PopSeconds = 0.3f;

        [SerializeField] ViewLook look;
        [SerializeField] SoundFx sound;

        class Pop
        {
            public string Id;
            public GameObject Ghost;
            public Renderer Hidden;
            public float Time;
            public Vector3 Size;
        }

        readonly Dictionary<int, MeshFilter> surfaceViews = new Dictionary<int, MeshFilter>();
        readonly List<Pop> pops = new List<Pop>();
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<int> triangles = new List<int>();
        Transform container;
        LineRenderer trace;
        float traceTime = -1f;

        public void Set(ViewLook value, SoundFx fx)
        {
            look = value;
            sound = fx;
        }

        void OnEnable()
        {
            StageContext.DeskDecided += OnDeskDecided;
            ScanContext.SurfacesChanged += OnSurfacesChanged;
            ScanContext.BoxesChanged += OnBoxesChanged;
            OnSurfacesChanged();
        }

        void OnDisable()
        {
            StageContext.DeskDecided -= OnDeskDecided;
            ScanContext.SurfacesChanged -= OnSurfacesChanged;
            ScanContext.BoxesChanged -= OnBoxesChanged;
            ClearAll();
        }

        // 演出の物は StageRoot の子にまとめる（StageRoot が替わったら作り直す）
        Transform Container()
        {
            var root = StageContext.StageRoot;
            if (root == null)
            {
                return null;
            }
            if (container == null || container.parent != root)
            {
                ClearAll();
                container = new GameObject("ScanEffects").transform;
                container.SetParent(root, false);
            }
            return container;
        }

        void ClearAll()
        {
            if (container != null)
            {
                Destroy(container.gameObject);
            }
            container = null;
            trace = null;
            traceTime = -1f;
            surfaceViews.Clear();
            foreach (var p in pops)
            {
                if (p.Hidden != null)
                {
                    p.Hidden.enabled = true;
                }
            }
            pops.Clear();
        }

        void OnSurfacesChanged()
        {
            var parent = Container();
            if (parent == null || look == null)
            {
                return;
            }
            var alive = new HashSet<int>();
            foreach (var s in ScanContext.Surfaces)
            {
                alive.Add(s.Id);
                if (!surfaceViews.TryGetValue(s.Id, out var view) || view == null)
                {
                    var go = new GameObject("Surface " + s.Id) { layer = Layers.ScanView };
                    go.transform.SetParent(parent, false);
                    view = go.AddComponent<MeshFilter>();
                    view.sharedMesh = new Mesh { name = "Surface " + s.Id };
                    go.AddComponent<MeshRenderer>().sharedMaterial = look.ScanSurface;
                    surfaceViews[s.Id] = view;
                }
                // 箱の上面と重なってちらつかないよう、2mm 上に描く
                ScanGeometry.FanMesh(s.Outline, s.Height + 0.002f, vertices, triangles);
                var mesh = view.sharedMesh;
                mesh.Clear();
                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateBounds();
            }
            var gone = new List<int>();
            foreach (var id in surfaceViews.Keys)
            {
                if (!alive.Contains(id))
                {
                    gone.Add(id);
                }
            }
            foreach (var id in gone)
            {
                if (surfaceViews[id] != null)
                {
                    Destroy(surfaceViews[id].gameObject);
                }
                surfaceViews.Remove(id);
            }
        }

        void OnDeskDecided()
        {
            var parent = Container();
            if (parent == null)
            {
                return;
            }
            if (trace == null)
            {
                var go = new GameObject("DeskTrace") { layer = Layers.ScanView };
                go.transform.SetParent(parent, false);
                trace = go.AddComponent<LineRenderer>();
                trace.useWorldSpace = false;
                trace.widthMultiplier = 0.006f;
                trace.numCornerVertices = 2;
                trace.sharedMaterial = look != null ? look.ScanLine : null;
            }
            traceTime = 0f;
            if (sound != null)
            {
                sound.PlayChime();
            }
        }

        void OnBoxesChanged(string id, BoxChange change, string source)
        {
            if (change == BoxChange.Removed)
            {
                RemovePop(id);
                return;
            }
            if (change != BoxChange.Added)
            {
                return;
            }
            var box = ScanContext.WorkingBoxes.Find(b => b.id == id);
            var parent = Container();
            if (box == null || parent == null || look == null)
            {
                return;
            }
            var ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ghost.name = "Pop " + id;
            ghost.layer = Layers.ScanView;
            Destroy(ghost.GetComponent<Collider>());
            ghost.GetComponent<Renderer>().sharedMaterial = look.BoxScan;
            ghost.transform.SetParent(parent, false);
            ghost.transform.localRotation = Quaternion.Euler(0f, box.yawDegrees, 0f);
            var size = new Vector3(box.size[0], box.size[1], box.size[2]);
            ghost.transform.localPosition = new Vector3(box.center[0], 0f, box.center[2]);
            ghost.transform.localScale = new Vector3(size.x, 0.0001f, size.z);
            pops.Add(new Pop { Id = id, Ghost = ghost, Time = 0f, Size = size });
            if (sound != null)
            {
                sound.PlayPop();
            }
        }

        void RemovePop(string id)
        {
            for (int i = pops.Count - 1; i >= 0; i--)
            {
                if (pops[i].Id != id)
                {
                    continue;
                }
                if (pops[i].Ghost != null)
                {
                    Destroy(pops[i].Ghost);
                }
                if (pops[i].Hidden != null)
                {
                    pops[i].Hidden.enabled = true;
                }
                pops.RemoveAt(i);
            }
        }

        void Update()
        {
            var dt = Time.deltaTime;
            if (traceTime >= 0f && trace != null)
            {
                traceTime += dt;
                var t = traceTime / TraceSeconds;
                var points = ScanGeometry.TraceRectangle(ScanContext.DeskSize, t, 0.002f);
                trace.positionCount = points.Count;
                trace.SetPositions(points.ToArray());
                if (t >= 1.5f)
                {
                    // なぞり終えて少し見せたら消す（その後は B の机の板が DeskOutline で見える）
                    trace.positionCount = 0;
                    traceTime = -1f;
                }
            }

            for (int i = pops.Count - 1; i >= 0; i--)
            {
                var p = pops[i];
                if (p.Ghost == null)
                {
                    pops.RemoveAt(i);
                    continue;
                }
                p.Time += dt;
                // B の同じ id の箱があれば、伸びている間は隠す
                if (p.Hidden == null && StageContext.StageRoot != null)
                {
                    var real = StageContext.StageRoot.Find(p.Id);
                    if (real != null && real.TryGetComponent<Renderer>(out var r))
                    {
                        p.Hidden = r;
                        r.enabled = false;
                    }
                }
                var k = ScanGeometry.PopCurve(p.Time / PopSeconds);
                var h = Mathf.Max(0.0001f, p.Size.y * k);
                p.Ghost.transform.localScale = new Vector3(p.Size.x, h, p.Size.z);
                var pos = p.Ghost.transform.localPosition;
                p.Ghost.transform.localPosition = new Vector3(pos.x, h * 0.5f, pos.z);
                if (p.Time >= PopSeconds && p.Hidden != null)
                {
                    // 伸び終わったら B の箱に任せる。B の箱がまだなければ、仮の箱をそのまま見せておく
                    p.Hidden.enabled = true;
                    Destroy(p.Ghost);
                    pops.RemoveAt(i);
                }
            }
        }
    }
}
