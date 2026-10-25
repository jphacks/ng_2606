using System;
using System.IO;
using DeskStage.Shared;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeskStage.App.EditorTools
{
    /// <summary>
    /// 箱のしかけ（水・炎・回復）の 3D の見た目を、画像（PNG）に書き出して確かめる（開発用）。実機に入れなくても、見た目が分かる。
    /// バッチで使う：-executeMethod DeskStage.App.EditorTools.GimmickPreview.Run -out 出力のフォルダ
    /// </summary>
    public static class GimmickPreview
    {
        const string MaterialPath = "Assets/_Project/App/Materials/GimmickUnlit.mat";

        public static void Run()
        {
            var outDir = Arg("-out");
            if (outDir == null)
            {
                Debug.LogError("[GimmickPreview] -out を指定してください");
                Finish(1);
                return;
            }
            Directory.CreateDirectory(outDir);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Camera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.85f, 0.93f, 1f);
            camera.fieldOfView = 40f;
            camera.transform.position = new Vector3(0f, 0.55f, -0.62f);
            camera.transform.LookAt(new Vector3(0f, 0.03f, 0f));
            camera.tag = "MainCamera";

            // ステージ：机と、箱 4 つ（水、炎、回復、しかけなし）
            var root = new GameObject("StageRoot").transform;
            StageContext.StageRoot = root;
            var unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            var desk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            desk.name = "Desk";
            desk.transform.SetParent(root, false);
            desk.transform.localScale = new Vector3(0.9f, 0.01f, 0.5f);
            desk.transform.localPosition = new Vector3(0f, -0.005f, 0f);
            desk.GetComponent<Renderer>().sharedMaterial = new Material(unlit) { color = new Color(0.9f, 0.8f, 0.6f) };

            var boxes = new[]
            {
                Box(root, unlit, "box-01", -0.30f, 0.16f, BoxData.GimmickWater, new Color(0.85f, 0.7f, 0.5f)),
                Box(root, unlit, "box-02", -0.10f, 0.12f, BoxData.GimmickFire, new Color(0.85f, 0.7f, 0.5f)),
                Box(root, unlit, "box-03", 0.12f, 0.08f, BoxData.GimmickHeal, new Color(0.85f, 0.7f, 0.5f)),
                Box(root, unlit, "box-04", 0.32f, 0.10f, "", new Color(0.85f, 0.7f, 0.5f)),
            };
            StageContext.CurrentStage = new StageData { boxes = boxes };

            var systemGo = new GameObject("Gimmicks");
            var system = systemGo.AddComponent<GimmickSystem>();
            system.SetMaterial(AssetDatabase.LoadAssetAtPath<Material>(MaterialPath));
            Call(system, "Rebuild");
            Debug.Log("[GimmickPreview] " + system.Summary());

            // 時刻をずらして 2 枚（動きの違いが分かる）
            Shoot(system, camera, Path.Combine(outDir, "gimmick-3d.png"));
            Finish(0);
        }

        static BoxData Box(Transform root, Shader shader, string id, float x, float height, string gimmick, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = id;
            go.transform.SetParent(root, false);
            go.transform.localScale = new Vector3(0.14f, height, 0.14f);
            go.transform.localPosition = new Vector3(x, height * 0.5f, 0f);
            go.GetComponent<Renderer>().sharedMaterial = new Material(shader) { color = color };
            return new BoxData
            {
                id = id,
                center = new[] { x, height * 0.5f, 0f },
                size = new[] { 0.14f, height, 0.14f },
                gimmick = gimmick,
            };
        }

        static void Call(object target, string method)
        {
            target.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(target, null);
        }

        static void Shoot(GimmickSystem system, Camera camera, string path)
        {
            const int w = 1600, h = 900;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = rt;
            // 編集モードでは Update が回らないので、動きの 1 コマを作る
            Call(system, "AnimatePads");
            Call(system, "AnimateBillboards");
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
            Debug.Log($"[GimmickPreview] 書きました：{path}");
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
