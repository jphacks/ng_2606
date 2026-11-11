using System.Collections.Generic;
using UnityEngine;

namespace DeskStage.Merge
{
    /// <summary>
    /// 写真のキャラを、立体にする（2026-11-10）。ずんぐりした、ゲームのキャラのような形：
    ///  ・頭：大きな丸い頭。前の半分に、撮った顔の写真（HumanMask の顔の枠）を貼る。後ろは、顔の写真の平均の色を暗くした色（髪のつもり）
    ///  ・胴・腕：青い服。腕は、肩の玉から下へのびる太く短い腕と、白い手袋の手
    ///  ・足：紺のズボンの、太く短い足と、茶色の大きな靴
    /// 胴・手足の形と色は、だれが撮っても同じ（撮るのは顔だけ）。手足は胴にめり込ませて、つなぎ目が見えないようにする。
    /// 腕は肩、足は付け根に軸（Joint）を置き、軸を回すと、その手足が回る。手足を動かすのは CharacterLimbs。
    /// 見た目だけで、当たり判定は付けない。
    /// </summary>
    public static class CharacterBody3D
    {
        /// <summary>部品の数と順番（Build が返す軸の並び）。</summary>
        public const int PartCount = 6;
        public const int Head = 0;
        public const int Torso = 1;
        public const int LeftArm = 2;
        public const int RightArm = 3;
        public const int LeftLeg = 4;
        public const int RightLeg = 5;

        /// <summary>足の長さ（付け根から足の裏まで）：身長に対する割合。手足の動き（CharacterLimbs）が、歩幅を決めるのに使う。</summary>
        public static float LegLength => Hip.y;

        /// <summary>写真を取る範囲を、楕円より少し内側にする（ふちの、ぼかした部分を貼らない）。</summary>
        const float UvInset = 0.94f;

        // 形（身長に対する割合）。足元が 0
        static readonly Vector3 HeadCenter = new Vector3(0f, 0.70f, 0f);
        static readonly Vector3 HeadRadius = new Vector3(0.26f, 0.27f, 0.24f);
        static readonly Vector3 TorsoCenter = new Vector3(0f, 0.38f, 0f);
        static readonly Vector3 TorsoRadius = new Vector3(0.21f, 0.20f, 0.17f);
        static readonly Vector3 Shoulder = new Vector3(0.18f, 0.47f, 0f);
        const float ShoulderBall = 0.075f;
        const float ArmLength = 0.19f;
        const float ArmRadius = 0.058f;
        const float ArmTilt = 14f; // 腕を、下から少し外へ開く角度（度）
        const float HandRadius = 0.078f;
        static readonly Vector3 Hip = new Vector3(0.09f, 0.22f, 0f);
        const float LegRadius = 0.066f;
        static readonly Vector3 ShoeRadius = new Vector3(0.078f, 0.052f, 0.115f);
        const float ShoeForward = 0.035f;

        static readonly Color ShirtColor = new Color(0.22f, 0.45f, 0.88f);
        static readonly Color PantsColor = new Color(0.17f, 0.22f, 0.42f);
        static readonly Color GloveColor = new Color(0.97f, 0.97f, 0.97f);
        static readonly Color ShoeColor = new Color(0.38f, 0.22f, 0.13f);
        const float HairDarken = 0.6f;

        static Mesh sphereMesh;
        static Mesh capsuleMesh;

        /// <summary>
        /// 体を作り、parent の子にする。height はキャラの身長（m）。足の下の端が parent の原点（足元）に来る。
        /// material は、顔の写真 photo を貼った素材。face は、写真の中の顔の楕円。返す配列は、部品ごとの軸（PartCount の並び）。
        /// </summary>
        public static Transform[] Build(Transform parent, Material material, Texture2D photo, Vector4 face, float height, List<Object> created)
        {
            LoadMeshes();
            var joints = new Transform[PartCount];

            // 頭：前に顔の写真、後ろは髪の色
            var head = Joint(parent, "Head", HeadCenter * height);
            var headMesh = Ellipsoid(face, HeadRadius.x * height, HeadRadius.y * height, HeadRadius.z * height);
            created.Add(headMesh);
            var hair = AverageColor(photo, face) * HairDarken;
            hair.a = 1f;
            var headBall = new GameObject("HeadBall");
            headBall.transform.SetParent(head, false);
            headBall.AddComponent<MeshFilter>().sharedMesh = headMesh;
            headBall.AddComponent<MeshRenderer>().sharedMaterials = new[] { material, Plain(material, hair, created) };
            joints[Head] = head;

            var shirt = Plain(material, ShirtColor, created);
            var pants = Plain(material, PantsColor, created);
            var glove = Plain(material, GloveColor, created);
            var shoe = Plain(material, ShoeColor, created);

            var torso = Joint(parent, "Torso", TorsoCenter * height);
            Piece(torso, sphereMesh, shirt, Vector3.zero, Quaternion.identity, TorsoRadius * (2f * height));
            joints[Torso] = torso;

            foreach (var index in new[] { LeftArm, RightArm })
            {
                // LeftArm は、前から見て左（+x）
                var side = index == LeftArm ? 1f : -1f;
                var joint = Joint(parent, "Arm" + index, new Vector3(Shoulder.x * side, Shoulder.y, 0f) * height);
                Piece(joint, sphereMesh, shirt, Vector3.zero, Quaternion.identity, Vector3.one * (ShoulderBall * 2f * height));
                // 腕は、肩から下へ。少し外へ開く。Unity のカプセルは、半径 0.5、高さ 2：半径 r、両端の玉の中心の間 L なら、縦を (L + 2r) / 2 にする
                var tilt = Quaternion.Euler(0f, 0f, ArmTilt * side);
                var down = tilt * Vector3.down;
                Piece(joint, capsuleMesh, shirt, down * (ArmLength * 0.5f * height), tilt,
                    new Vector3(ArmRadius * 2f, ArmLength * 0.5f + ArmRadius, ArmRadius * 2f) * height);
                Piece(joint, sphereMesh, glove, down * (ArmLength * height), Quaternion.identity, Vector3.one * (HandRadius * 2f * height));
                joints[index] = joint;
            }

            foreach (var index in new[] { LeftLeg, RightLeg })
            {
                var side = index == LeftLeg ? 1f : -1f;
                var hip = new Vector3(Hip.x * side, Hip.y, 0f) * height;
                var joint = Joint(parent, "Leg" + index, hip);
                // 足：付け根から、靴の上まで
                var shoeCenterY = ShoeRadius.y * height;
                var legLength = hip.y - shoeCenterY;
                Piece(joint, capsuleMesh, pants, Vector3.down * (legLength * 0.5f), Quaternion.identity,
                    new Vector3(LegRadius * 2f * height, legLength * 0.5f + LegRadius * height, LegRadius * 2f * height));
                Piece(joint, sphereMesh, shoe, new Vector3(0f, shoeCenterY - hip.y, ShoeForward * height), Quaternion.identity, ShoeRadius * (2f * height));
                joints[index] = joint;
            }

            foreach (var joint in joints)
            {
                SetLayer(joint, parent.gameObject.layer);
            }
            return joints;
        }

        static void LoadMeshes()
        {
            if (sphereMesh != null && capsuleMesh != null) return;
            foreach (var type in new[] { PrimitiveType.Sphere, PrimitiveType.Capsule })
            {
                var probe = GameObject.CreatePrimitive(type);
                var mesh = probe.GetComponent<MeshFilter>().sharedMesh;
                if (type == PrimitiveType.Sphere) sphereMesh = mesh; else capsuleMesh = mesh;
                if (Application.isPlaying) Object.Destroy(probe); else Object.DestroyImmediate(probe);
            }
        }

        static Transform Joint(Transform parent, string name, Vector3 at)
        {
            var joint = new GameObject(name).transform;
            joint.SetParent(parent, false);
            joint.localPosition = at;
            return joint;
        }

        static void Piece(Transform parent, Mesh mesh, Material material, Vector3 at, Quaternion rotation, Vector3 scale)
        {
            var go = new GameObject("Piece");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            go.transform.localRotation = rotation;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        // 写真を貼らない、単色の素材（material と同じ陰の付き方）
        static Material Plain(Material material, Color color, List<Object> created)
        {
            var plain = new Material(material);
            plain.SetTexture("_BaseMap", null);
            plain.mainTexture = null;
            plain.SetColor("_BaseColor", color);
            plain.color = color;
            created.Add(plain);
            return plain;
        }

        static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            foreach (Transform child in t) SetLayer(child, layer);
        }

        /// <summary>
        /// 楕円の玉（半径 rx, ry, rz）。写真は前（+z）からまっすぐ貼る：玉の表面の点 (x, y) に、楕円 p の中の同じ割合の点の色。
        /// 前は左右を反転して貼る（前から見て、写真のとおり）。三角形は、前の半分（部分の 0 番）と後ろの半分（1 番）に分ける。
        /// </summary>
        public static Mesh Ellipsoid(Vector4 p, float rx, float ry, float rz)
        {
            const int lat = 20;
            const int lon = 28;
            var vertices = new Vector3[(lat + 1) * (lon + 1)];
            var uvs = new Vector2[vertices.Length];
            for (var a = 0; a <= lat; a++)
            {
                var theta = Mathf.PI * a / lat;
                for (var b = 0; b <= lon; b++)
                {
                    var phi = 2f * Mathf.PI * b / lon;
                    var s = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi));
                    var i = a * (lon + 1) + b;
                    vertices[i] = new Vector3(s.x * rx, s.y * ry, s.z * rz);
                    uvs[i] = PhotoUv(p, s);
                }
            }
            var front = new List<int>();
            var back = new List<int>();
            for (var a = 0; a < lat; a++)
            {
                for (var b = 0; b < lon; b++)
                {
                    var i0 = a * (lon + 1) + b;
                    var i1 = i0 + 1;
                    var i2 = i0 + lon + 1;
                    var i3 = i2 + 1;
                    var z = vertices[i0].z + vertices[i1].z + vertices[i2].z + vertices[i3].z;
                    var list = z >= 0f ? front : back;
                    list.Add(i0); list.Add(i1); list.Add(i2);
                    list.Add(i1); list.Add(i3); list.Add(i2);
                }
            }
            var mesh = new Mesh { name = "CharacterPart", vertices = vertices, uv = uvs, subMeshCount = 2 };
            mesh.SetTriangles(front, 0);
            mesh.SetTriangles(back, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>玉の表面の向き s（単位球の点）に貼る、写真の中の位置（前の半分で使う）。</summary>
        public static Vector2 PhotoUv(Vector4 p, Vector3 s)
        {
            var u = p.x - p.z * UvInset * s.x;
            var v = p.y + p.w * UvInset * s.y;
            return new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v));
        }

        /// <summary>写真の、楕円 p の中の色の平均（後ろの半分の色）。写真が読めなければ、灰色。</summary>
        public static Color AverageColor(Texture2D photo, Vector4 p)
        {
            if (photo == null || !photo.isReadable) return new Color(0.5f, 0.5f, 0.5f);
            const int n = 12;
            var sum = Vector3.zero;
            var count = 0;
            for (var j = 0; j < n; j++)
            {
                for (var i = 0; i < n; i++)
                {
                    var dx = (i + 0.5f) / n * 2f - 1f;
                    var dy = (j + 0.5f) / n * 2f - 1f;
                    if (dx * dx + dy * dy > 0.8f) continue;
                    var c = photo.GetPixelBilinear(p.x + p.z * dx, p.y + p.w * dy);
                    sum += new Vector3(c.r, c.g, c.b);
                    count++;
                }
            }
            sum /= Mathf.Max(1, count);
            return new Color(sum.x, sum.y, sum.z, 1f);
        }
    }
}
