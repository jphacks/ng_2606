using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>
    /// 坂（BoxData.ramp）のくさび形。箱と同じ、1 辺が 1 の立方体（中心が原点）の中に収まる形で、
    /// -x の端は底（y = -0.5）、+x の端は上（y = +0.5）まで上がる。箱と同じ位置・大きさの変形（StageLayout.Box）をそのまま使える。
    /// </summary>
    public static class RampMesh
    {
        static Mesh wedge;
        static Mesh surface;

        /// <summary>坂の表面（箱の座標）の高さ。-x の端が -0.5、+x の端が +0.5。</summary>
        public static float SurfaceLocalY(float localX) => localX;

        /// <summary>くさび形（底・うしろ・斜めの面・左右の三角）。</summary>
        public static Mesh Wedge()
        {
            if (wedge == null)
            {
                wedge = BuildWedge();
            }
            return wedge;
        }

        /// <summary>斜めの面だけの板（写真やしかけの模様を、坂の上に貼るため）。UV は、箱の上面（x、z）と同じ向き。</summary>
        public static Mesh Surface()
        {
            if (surface == null)
            {
                surface = BuildSurface(0f);
            }
            return surface;
        }

        /// <summary>斜めの面だけの板を、面から少し浮かせた物（浮かせる量は、箱の座標の y）。</summary>
        public static Mesh SurfaceLifted(float lift) => BuildSurface(lift);

        static Mesh BuildSurface(float lift)
        {
            var up = new Vector3(0f, lift, 0f);
            var mesh = new Mesh { name = "RampSurface" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, -0.5f) + up, new Vector3(0.5f, 0.5f, -0.5f) + up,
                new Vector3(0.5f, 0.5f, 0.5f) + up, new Vector3(-0.5f, -0.5f, 0.5f) + up,
            };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            mesh.triangles = new[] { 0, 3, 2, 0, 2, 1 };
            var normal = new Vector3(-1f, 1f, 0f).normalized;
            mesh.normals = new[] { normal, normal, normal, normal };
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh BuildWedge()
        {
            var vertices = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var normals = new System.Collections.Generic.List<Vector3>();
            var triangles = new System.Collections.Generic.List<int>();

            void Face(Vector3 normal, params Vector3[] corners)
            {
                var start = vertices.Count;
                foreach (var c in corners)
                {
                    vertices.Add(c);
                    normals.Add(normal);
                    uvs.Add(new Vector2(c.x + 0.5f, c.z + 0.5f));
                }
                // 三角形の並びは、corners の順（Unity は、時計まわりが表）
                for (var i = 1; i + 1 < corners.Length; i++)
                {
                    triangles.Add(start);
                    triangles.Add(start + i);
                    triangles.Add(start + i + 1);
                }
            }

            // 底（下向き）
            Face(Vector3.down, new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f));
            // うしろ（+x の向き）
            Face(Vector3.right, new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f));
            // 斜めの面（上向きで、-x の方へ少し傾く）
            Face(new Vector3(-1f, 1f, 0f).normalized, new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, -0.5f));
            // 左右の三角（-z、+z）
            Face(Vector3.back, new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f));
            Face(Vector3.forward, new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f));

            var mesh = new Mesh { name = "RampWedge" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
