using System;
using System.IO;
using System.Reflection;
using DeskStage.Merge;
using DeskStage.Shared;
using DeskStage.Stage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeskStage.App.EditorTools
{
    /// <summary>
    /// 保存したステージ（stage.json と、箱の上面の写真）を、遊ぶときと同じ光・素材・写真の貼り方で組み立てて、画像（PNG）に書き出す（開発用。2026-11-11）。
    /// 実機に入れなくても、光・影・質感の変化を比べられる。
    /// バッチで使う：-executeMethod DeskStage.App.EditorTools.StagePreview.Run -out 出力のフォルダ -stage 保存したステージのフォルダ [-backdrop 背景の JPEG]
    /// 光は、実機の Main.unity と同じく、シーンの「Directional Light」（影なし）と、App_System の PlayLight の 2 つを置く。
    /// </summary>
    public static class StagePreview
    {
        const string AppPrefab = "Assets/_Project/App/Prefabs/App_System.prefab";
        const string PhotoMaterialPath = "Assets/_Project/Merge/Materials/PhotoUnlit.mat";
        const string DeskMaterialPath = "Assets/_Project/Rendering/DeskPlay.mat";
        const string BoxMaterialPath = "Assets/_Project/Rendering/BoxPlay.mat";

        public static void Run()
        {
            var outDir = Arg("-out");
            var stageDir = Arg("-stage");
            if (outDir == null || stageDir == null)
            {
                Debug.LogError("[StagePreview] -out と -stage を指定してください");
                EditorApplication.Exit(1);
                return;
            }
            Directory.CreateDirectory(outDir);
            QualitySettings.SetQualityLevel(0, true); // iPhone と同じ「Mobile」の設定（URP の資産）

            // 1 回目の描画は素材がそろわないことがあるので、捨てる絵を先に描く
            foreach (var pass in new[] { "warmup", "play", "near" })
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var stage = JsonUtility.FromJson<StageData>(File.ReadAllText(Path.Combine(stageDir, "stage.json")));
                var root = Build(stage);
                AddLights();
                Texture(root, stage, stageDir, Arg("-backdrop"));
                var camera = MakeCamera(stage, pass == "near");
                var path = Path.Combine(outDir, "stage-" + pass + ".png");
                Shoot(camera, path);
            }
            EditorApplication.Exit(0);
        }

        static Transform Build(StageData stage)
        {
            var root = new GameObject("StageRoot").transform;
            StageContext.StageRoot = root;
            StageContext.CurrentStage = stage;
            var desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            desk.name = "Desk";
            desk.transform.SetParent(root, false);
            var thick = 0.03f;
            desk.transform.localScale = new Vector3(stage.desk.width, thick, stage.desk.depth);
            desk.transform.localPosition = new Vector3(0f, -thick / 2f, 0f);
            desk.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(DeskMaterialPath);
            var boxMaterial = AssetDatabase.LoadAssetAtPath<Material>(BoxMaterialPath);
            foreach (var b in stage.boxes)
            {
                GameObject go;
                if (b.ramp)
                {
                    go = new GameObject(b.id);
                    var mesh = RampMesh.Wedge();
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    go.AddComponent<MeshRenderer>();
                    go.AddComponent<MeshCollider>().sharedMesh = mesh;
                }
                else
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = b.id;
                }
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3(b.center[0], b.center[1], b.center[2]);
                go.transform.localRotation = Quaternion.Euler(0f, b.yawDegrees, 0f);
                go.transform.localScale = new Vector3(b.size[0], b.size[1], b.size[2]);
                go.GetComponent<Renderer>().sharedMaterial = boxMaterial;
            }
            return root;
        }

        // 実機の Main.unity と同じ 2 つの光（シーンの影なしの光と、App_System の PlayLight）
        static void AddLights()
        {
            var sceneLight = new GameObject("Directional Light").AddComponent<Light>();
            sceneLight.type = LightType.Directional;
            sceneLight.intensity = 1f;
            sceneLight.shadows = LightShadows.None;
            sceneLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var app = AssetDatabase.LoadAssetAtPath<GameObject>(AppPrefab);
            var play = app != null ? FindChild(app.transform, "PlayLight") : null;
            if (play != null)
            {
                var copy = UnityEngine.Object.Instantiate(play.gameObject);
                copy.name = "PlayLight";
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            DynamicGI.UpdateEnvironment();
        }

        static void Texture(Transform root, StageData stage, string stageDir, string backdrop)
        {
            var host = new GameObject("Texturer");
            host.SetActive(false); // Awake・OnEnable で、実機用の撮影の部品を探さないように
            var texturer = host.AddComponent<StagePhotoTexturer>();
            var so = new SerializedObject(texturer);
            so.FindProperty("photoMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(PhotoMaterialPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var addOverlay = typeof(StagePhotoTexturer).GetMethod("AddOverlay", flags);
            foreach (var b in stage.boxes)
            {
                var path = Path.Combine(stageDir, b.texture ?? "");
                var target = root.Find(b.id);
                if (string.IsNullOrEmpty(b.texture) || !File.Exists(path) || target == null) continue;
                var texture = new Texture2D(2, 2, TextureFormat.RGB24, true) { wrapMode = TextureWrapMode.Clamp };
                texture.LoadImage(File.ReadAllBytes(path));
                texture.wrapMode = TextureWrapMode.Clamp;
                addOverlay.Invoke(texturer, new object[] { target, b.id, texture, true, PhotoTextureMath.Average(texture.GetPixels32()) });
            }
            if (backdrop != null && File.Exists(backdrop))
            {
                typeof(StagePhotoTexturer).GetMethod("MakeBackdropPixels", flags).Invoke(texturer, new object[] { File.ReadAllBytes(backdrop) });
                typeof(StagePhotoTexturer).GetMethod("AddBackdrop", flags).Invoke(texturer, new object[] { root });
            }
            // 光を整える処理があれば、実機と同じく、写真を貼ったあとに呼ぶ
            typeof(StagePhotoTexturer).GetMethod("ApplyLighting", flags)?.Invoke(texturer, null);
        }

        // 遊ぶときのカメラに近い向き：キャラの後ろ上から、机の真ん中を見る。near は、低い所から箱に近づく
        static Camera MakeCamera(StageData stage, bool near)
        {
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.85f, 0.93f, 1f);
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 20f;
            camera.tag = "MainCamera";
            var size = Mathf.Max(stage.desk.width, stage.desk.depth);
            if (near)
            {
                camera.transform.position = new Vector3(size * 0.1f, 0.12f, -stage.desk.depth * 0.55f);
                camera.transform.LookAt(new Vector3(-size * 0.1f, 0.04f, 0f));
            }
            else
            {
                camera.transform.position = new Vector3(0f, size * 0.55f, -size * 0.85f);
                camera.transform.LookAt(new Vector3(0f, 0f, 0f));
            }
            return camera;
        }

        static void Shoot(Camera camera, string path)
        {
            const int w = 1600, h = 900;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = rt;
            camera.Render();
            var before = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = before;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(rt);
        }

        static Transform FindChild(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t)
            {
                var found = FindChild(c, name);
                if (found != null) return found;
            }
            return null;
        }

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }
            return null;
        }
    }
}
