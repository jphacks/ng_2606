using System;
using System.IO;
using System.Reflection;
using DeskStage.Shared;
using DeskStage.Stage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeskStage.Merge.EditorTools
{
    /// <summary>
    /// 3D のステージの見た目（写真を貼った机、箱、地面、背景）を、画像に書き出して確かめる（開発用）。
    /// 実機に入れなくても、見た目の不具合（背景が見えない、など）を調べられる。
    /// 撮影の 2 枚の写真（実機から取り出した物）と、手で決めた箱と位置から、ステージを組み立て、
    /// StagePhotoTexturer を動かして、カメラで見える画像を PNG に書く。
    ///
    /// バッチで使う：Unity を -nographics なし（描画できる状態）で、
    ///   -executeMethod DeskStage.Merge.EditorTools.VisualCheck.Run -topPhoto top.jpg -sidePhoto side.jpg -backdropPhoto bg.jpg -out 出力のフォルダ
    /// 背景の写真（-backdropPhoto）を渡さなければ、背景なし。端末の背景の保存場所（BackdropStore）は、実行のあいだだけ使い、元に戻す。
    /// </summary>
    public static class VisualCheck
    {
        const string PhotoMaterialPath = "Assets/_Project/Merge/Materials/PhotoUnlit.mat";

        public static void Run()
        {
            var topPath = Arg("-topPhoto");
            var sidePath = Arg("-sidePhoto");
            var backdropPath = Arg("-backdropPhoto");
            var outDir = Arg("-out");
            if (topPath == null || sidePath == null || outDir == null)
            {
                Debug.LogError("[VisualCheck] -topPhoto、-sidePhoto、-out を指定してください");
                Finish(1);
                return;
            }
            Directory.CreateDirectory(outDir);

            var store = BackdropStore.AtDefaultPlace();
            var savedBackdrop = store.Load();
            try
            {
                if (backdropPath != null) store.Save(File.ReadAllBytes(backdropPath)); else store.Clear();
                Render(File.ReadAllBytes(topPath), File.ReadAllBytes(sidePath), outDir);
            }
            finally
            {
                if (savedBackdrop != null) store.Save(savedBackdrop); else store.Clear();
            }
            Finish(0);
        }

        static void Render(byte[] top, byte[] side, string outDir)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            StageContext.StageRoot = null;

            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.13f, 0.16f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 20f;

            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 机 0.62 x 0.40 m に、箱 5 つ（実機の写真の物のおよその位置）
            var stage = new StageData
            {
                id = "visual-check",
                desk = new DeskData { width = 0.62f, depth = 0.40f },
                boxes = new[]
                {
                    Box("box-01", -0.20f, -0.12f, 0.165f, 0.04f, 0.065f),
                    Box("box-02", -0.19f, 0.04f, 0.10f, 0.17f, 0.12f),
                    Box("box-03", 0.05f, -0.10f, 0.26f, 0.06f, 0.07f),
                    Box("box-04", 0.07f, 0.02f, 0.22f, 0.12f, 0.13f),
                    Box("box-05", 0.25f, -0.02f, 0.12f, 0.16f, 0.12f),
                },
            };
            StageContext.CurrentStage = stage;
            ScanContext.SelectedBoxId = "";

            var system = new GameObject("Stage_System");
            var builder = system.AddComponent<StageBuilder>();
            builder.Build(stage);

            // 1枚目の写真の中の位置（左上が (0,0)。実機の写真で、目で見て決めた、およその値）
            StagePhotoRegions.Set(
                Quad(0.00f, 0.24f, 0.90f, 0.20f, 0.95f, 0.95f, 0.00f, 0.98f),
                new System.Collections.Generic.Dictionary<string, Vector2[]>
                {
                    ["box-01"] = Quad(0.09f, 0.67f, 0.32f, 0.68f, 0.32f, 0.83f, 0.09f, 0.83f),
                    ["box-02"] = Quad(0.07f, 0.20f, 0.32f, 0.20f, 0.33f, 0.46f, 0.07f, 0.46f),
                    ["box-03"] = Quad(0.44f, 0.64f, 0.76f, 0.64f, 0.76f, 0.79f, 0.44f, 0.79f),
                    ["box-04"] = Quad(0.46f, 0.15f, 0.78f, 0.15f, 0.78f, 0.53f, 0.46f, 0.53f),
                    ["box-05"] = Quad(0.76f, 0.30f, 0.95f, 0.30f, 0.95f, 0.77f, 0.76f, 0.77f),
                });

            // 写真を貼る部品を、UI_System と同じ形で動かす（エディタでは Awake、OnEnable が呼ばれないので、直接呼ぶ）
            var ui = new GameObject("UI");
            var texturer = ui.AddComponent<StagePhotoTexturer>();
            var so = new SerializedObject(texturer);
            so.FindProperty("photoMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(PhotoMaterialPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            Call(texturer, "OnPhotos", top, side);
            Call(texturer, "Apply");

            var root = StageContext.StageRoot;
            Debug.Log($"[VisualCheck] StageRoot の子：{ChildNames(root)}");

            // 机を、手前斜め上から（Main.unity のカメラと同じ位置）と、横から、見る
            Shoot(camera, new Vector3(0f, 0.55f, -0.75f), Vector3.zero, Path.Combine(outDir, "check-view.png"));
            Shoot(camera, new Vector3(0.45f, 0.18f, -0.45f), new Vector3(0f, 0.04f, 0f), Path.Combine(outDir, "side-view.png"));
            // ステージの外（背景のドーム）が見えるように、低い位置から遠くを見る
            Shoot(camera, new Vector3(0f, 0.05f, -0.5f), new Vector3(0f, 0.06f, 3f), Path.Combine(outDir, "backdrop-view.png"));
        }

        static BoxData Box(string id, float x, float z, float width, float height, float depth) => new BoxData
        {
            id = id,
            center = new[] { x, height / 2f, z },
            size = new[] { width, height, depth },
            yawDegrees = 0f,
            source = BoxData.SourceAuto,
        };

        static Vector2[] Quad(float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3) =>
            new[] { new Vector2(x0, y0), new Vector2(x1, y1), new Vector2(x2, y2), new Vector2(x3, y3) };

        static void Call(object target, string method, params object[] args)
        {
            var m = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (m == null)
            {
                Debug.LogError($"[VisualCheck] {method} が見つかりません");
                return;
            }
            m.Invoke(target, args);
        }

        static void Shoot(Camera camera, Vector3 position, Vector3 lookAt, string path)
        {
            camera.transform.position = position;
            camera.transform.LookAt(lookAt);
            var rt = new RenderTexture(1280, 720, 24);
            camera.targetTexture = rt;
            camera.Render();
            var before = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = before;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(rt);
            Debug.Log($"[VisualCheck] 書きました：{path}");
        }

        static string ChildNames(Transform root)
        {
            if (root == null) return "（StageRoot なし）";
            var names = new System.Text.StringBuilder();
            foreach (Transform child in root) names.Append(child.name).Append(' ');
            return names.ToString();
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

        static void Finish(int code)
        {
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(code);
            }
        }
    }
}
