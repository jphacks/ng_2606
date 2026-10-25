using System;
using System.Collections.Generic;
using System.IO;
using DeskStage.Shared;
using DeskStage.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DeskStage.App.EditorTools
{
    /// <summary>
    /// 新しい UI の各画面を、画像（PNG）に書き出して、レイアウトを目で確かめる（開発用）。
    /// 実機に入れなくても、はみ出し、重なり、文字の大きさ、色を、見られる。
    ///
    /// バッチで使う：Unity を -nographics なしで、
    ///   -executeMethod DeskStage.App.EditorTools.AppUiPreview.Run -out 出力のフォルダ [-only 名前の先頭] [-photo 写真]
    /// 画面ごとの、見せたい状態（ステージの一覧、写真、失敗など）は、Scenarios に書く。
    /// </summary>
    public static class AppUiPreview
    {
        const string FontPath = "Assets/_Project/UI/Fonts/NotoSansJP-Regular SDF.asset";

        // 書き出す大きさ（基準 2532 x 1170 と同じ比）
        const int Width = 1624;
        const int Height = 750;

        class Scenario
        {
            public string Name;
            public AppScreenId Screen;
            public Action<AppContext> Setup;
            public object Arg;
        }

        public static void Run()
        {
            var outDir = Arg("-out");
            var only = Arg("-only");
            if (outDir == null)
            {
                Debug.LogError("[UiPreview] -out を指定してください");
                Finish(1);
                return;
            }
            Directory.CreateDirectory(outDir);
            UiSound.Enabled = false;

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraGo = new GameObject("PreviewCamera");
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 10f;

            var rootGo = new GameObject("App");
            var root = rootGo.AddComponent<AppRoot>();
            var so = new SerializedObject(root);
            so.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            so.FindProperty("viewLook").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ViewLook>("Assets/_Project/Rendering/ViewLook.asset");
            AssetDatabase.ImportAsset("Assets/_Project/App/Art/Title.png", ImportAssetOptions.ForceUpdate); // 取り込み設定（TitleImageImport）を、確実に当てる
            so.FindProperty("titleImage").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/App/Art/Title.png");
            so.ApplyModifiedPropertiesWithoutUndo();
            Theme.Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var context = root.Build(camera);

            var previewStageDir = PrepareThumbnail();
            try
            {
                foreach (var scenario in Scenarios())
                {
                    if (only != null && !scenario.Name.StartsWith(only))
                    {
                        continue;
                    }
                    Reset();
                    scenario.Setup?.Invoke(context);
                    context.Router.Show(scenario.Screen, scenario.Arg, remember: false, animate: false);
                    scenario.Setup?.Invoke(context); // 画面を出したあとに、見せたい状態を整える物もある
                    // 編集モードでは Update が回らないので、いまの画面の Update を 1 度だけ呼ぶ
                    var current = context.Router.Get(scenario.Screen);
                    current.GetType().GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(current, null);
                    Shoot(context, camera, Path.Combine(outDir, scenario.Name + ".png"));
                }
            }
            finally
            {
                if (previewStageDir != null && Directory.Exists(previewStageDir))
                {
                    Directory.Delete(previewStageDir, true);
                }
            }
            Finish(0);
        }

        static IEnumerable<Scenario> Scenarios()
        {
            yield return new Scenario { Name = "01-title", Screen = AppScreenId.Title };
            yield return new Scenario
            {
                Name = "02-home-empty",
                Screen = AppScreenId.Home,
                Setup = c => StageContext.SetStageList(new StageSummary[0]),
            };
            yield return new Scenario
            {
                Name = "03-home-stages",
                Screen = AppScreenId.Home,
                Setup = c => StageContext.SetStageList(SampleStages()),
            };
            yield return new Scenario
            {
                Name = "04-home-selected",
                Screen = AppScreenId.Home,
                Setup = c =>
                {
                    StageContext.SetStageList(SampleStages());
                    SelectHome(c, "preview-1");
                },
            };
            yield return new Scenario { Name = "05-howto", Screen = AppScreenId.HowTo };
            yield return new Scenario { Name = "05b-reach", Screen = AppScreenId.Reach };
            yield return new Scenario
            {
                Name = "06-capture-top",
                Screen = AppScreenId.Capture,
                Setup = c => FakeCamera(ScanStatus.FindingDesk),
            };
            yield return new Scenario
            {
                Name = "07-capture-preparing",
                Screen = AppScreenId.Capture,
                Setup = c =>
                {
                    ScanContext.CameraPreview = null;
                    StageContext.SetScanStatus(ScanStatus.Starting);
                },
            };
            yield return new Scenario
            {
                Name = "08-capture-limited",
                Screen = AppScreenId.Capture,
                Setup = c => FakeCamera(ScanStatus.Limited, ScanReason.Dark),
            };
            yield return new Scenario { Name = "09-capture-failed", Screen = AppScreenId.Capture, Arg = FailureKind.Network };
            yield return new Scenario { Name = "10-capture-denied", Screen = AppScreenId.Capture, Arg = FailureKind.CameraDenied };
            yield return new Scenario { Name = "11-generating", Screen = AppScreenId.Generating, Setup = c => SetSeconds(c, 8) };
            yield return new Scenario { Name = "11b-generating-long", Screen = AppScreenId.Generating, Setup = c => SetSeconds(c, 128) };
            yield return new Scenario { Name = "12-check", Screen = AppScreenId.Check, Setup = c => FakeBackground(c) };
            yield return new Scenario { Name = "13-play", Screen = AppScreenId.Play, Setup = c => { FakeBackground(c); c.Gimmicks.Health.Reset(); } };
            yield return new Scenario
            {
                Name = "13b-play-hit",
                Screen = AppScreenId.Play,
                Setup = c =>
                {
                    FakeBackground(c);
                    c.Gimmicks.Health.Reset();
                    c.Gimmicks.Health.TryDamage(0f);
                },
            };
            yield return new Scenario
            {
                Name = "19-gameover",
                Screen = AppScreenId.GameOver,
                Setup = c => c.Gimmicks.Health.Reset(),
            };
            yield return new Scenario
            {
                Name = "14-clear",
                Screen = AppScreenId.Clear,
                Arg = new ClearInfo { Seconds = 23.4f, IsBest = true },
            };
            yield return new Scenario { Name = "15-characters", Screen = AppScreenId.Characters };
            yield return new Scenario { Name = "16-backdrop", Screen = AppScreenId.Backdrop };
            yield return new Scenario
            {
                Name = "17-characters-capture",
                Screen = AppScreenId.Characters,
                Setup = c => Invoke(c.Router.Get(AppScreenId.Characters), "ShowCapture"),
            };
            yield return new Scenario
            {
                Name = "18-backdrop-capture",
                Screen = AppScreenId.Backdrop,
                Setup = c => Invoke(c.Router.Get(AppScreenId.Backdrop), "ShowCapture"),
            };
        }

        static void Invoke(object target, string method)
        {
            target.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(target, null);
        }

        // 「つくっている」画面の経過秒数を、見せたい値にする
        static void SetSeconds(AppContext c, int seconds)
        {
            var screen = c.Router.Get(AppScreenId.Generating);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(GeneratingScreen).GetField("since", flags).SetValue(screen, UnityEngine.Time.realtimeSinceStartup - seconds);
        }

        static Texture2D cameraTexture;

        static void FakeCamera(ScanStatus status, ScanReason reason = ScanReason.None)
        {
            var photo = Arg("-photo");
            if (cameraTexture == null && photo != null && File.Exists(photo))
            {
                cameraTexture = new Texture2D(2, 2);
                cameraTexture.LoadImage(File.ReadAllBytes(photo));
            }
            ScanContext.CameraPreview = cameraTexture;
            StageContext.SetScanStatus(status, reason);
        }

        // 3D の代わりに、写真を後ろに敷く
        static void FakeBackground(AppContext c)
        {
            if (c.Full.Find("FakeBack") != null)
            {
                return;
            }
            var rect = UiKit.Fill(UiKit.NewRect("FakeBack", c.Full));
            rect.SetAsFirstSibling();
            var raw = rect.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            var photo = Arg("-photo");
            if (photo != null && File.Exists(photo))
            {
                var tex = new Texture2D(2, 2);
                tex.LoadImage(File.ReadAllBytes(photo));
                raw.texture = tex;
            }
            else
            {
                raw.color = new Color(0.85f, 0.93f, 1f);
            }
        }

        static StageSummary[] SampleStages() => new[]
        {
            new StageSummary { id = "preview-1", name = "ステージ 3", createdAt = "2026-10-24T14:30:00+09:00", loadable = true },
            new StageSummary { id = "sample-01", name = "おてほん", createdAt = "2026-10-20T10:00:00+09:00", loadable = true },
            new StageSummary { id = "old-1", name = "ステージ 1", createdAt = "2026-10-01T10:00:00+09:00", loadable = false, reason = "古い形式" },
        };

        static void SelectHome(AppContext context, string id)
        {
            var home = (HomeScreen)context.Router.Get(AppScreenId.Home);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(HomeScreen).GetField("selectedId", flags).SetValue(home, id);
            typeof(HomeScreen).GetMethod("Refresh", flags).Invoke(home, null);
        }

        /// <summary>一覧の絵の確認用に、保存先に、仮のステージの絵を置く（終わったら消す）。</summary>
        static string PrepareThumbnail()
        {
            var photo = Arg("-photo");
            if (photo == null || !File.Exists(photo))
            {
                return null;
            }
            var dir = Path.Combine(Application.persistentDataPath, "Stages", "preview-1");
            Directory.CreateDirectory(dir);
            File.Copy(photo, Path.Combine(dir, "thumbnail.jpg"), true);
            return dir;
        }

        static void Reset()
        {
            StageContext.Mode = AppMode.Menu;
            StageContext.SetScanStatus(ScanStatus.NotStarted);
            ScanContext.WorkingBoxes.Clear();
            ScanContext.SelectedBoxId = "";
        }

        static void Shoot(AppContext context, Camera camera, string path)
        {
            // Canvas の大きさは、描く先（camera の targetTexture）で決まるので、先に決めてから、レイアウトする
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            foreach (var rect in context.Canvas.GetComponentsInChildren<RectTransform>(false))
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            }
            Canvas.ForceUpdateCanvases();
            foreach (var cover in context.Canvas.GetComponentsInChildren<CoverUv>(false))
            {
                cover.Apply();
            }

            camera.Render();
            var before = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = before;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(rt);
            Debug.Log($"[UiPreview] 書きました：{path}");
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
