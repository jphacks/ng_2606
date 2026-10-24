using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.UI
{
    /// <summary>
    /// 開発用：他の担当の部品がまだないうちに、知らせを自分で出すボタン用（Step 1）。
    /// 合流前（Step 15）に DevMarker と一緒に消す。
    /// </summary>
    public class DevEvents : MonoBehaviour
    {
        [SerializeField] ScreenRouter router;
        [SerializeField] GuidanceView guidance;

        public void SetRouter(ScreenRouter value) => router = value;

        public void SetGuidance(GuidanceView value) => guidance = value;

        void Awake()
        {
            // このプロジェクトは再生を止めても共通コードの値が残る設定（Domain Reload なし）。
            // テスト用シーンでは、前の再生の箱や状態が残らないよう、始めに空にする
            ScanContext.Clear();
            StageContext.StageRoot = null;
            StageContext.SetScanStatus(ScanStatus.NotStarted);
        }

        // スキャン状態を順に変えて、案内の表示を確かめる（Step 5）。本当は A が書く。
        static readonly (ScanStatus status, ScanReason reason, bool deskFound)[] ScanSteps =
        {
            (ScanStatus.Starting, ScanReason.None, false),
            (ScanStatus.FindingDesk, ScanReason.None, false),
            (ScanStatus.FindingDesk, ScanReason.None, true),
            (ScanStatus.Scanning, ScanReason.None, false),
            (ScanStatus.Limited, ScanReason.Dark, false),
            (ScanStatus.Limited, ScanReason.FastMotion, false),
            (ScanStatus.Limited, ScanReason.FewFeatures, false),
            (ScanStatus.Scanning, ScanReason.None, false),
            (ScanStatus.Lost, ScanReason.None, false),
            (ScanStatus.Finished, ScanReason.None, false),
            (ScanStatus.NotStarted, ScanReason.None, false),
        };

        int scanStep = -1;

        public void NextScanStatus()
        {
            scanStep = (scanStep + 1) % ScanSteps.Length;
            var step = ScanSteps[scanStep];
            StageContext.SetScanStatus(step.status, step.reason);
            if (guidance != null)
            {
                guidance.SetDeskFound(step.deskFound);
            }
            Debug.Log("DEV スキャン状態: " + step.status + " / " + step.reason + (step.deskFound ? "（机の面が見つかった）" : ""));
        }

        GameObject sampleBoxes;

        /// <summary>
        /// 仮の机と箱を置く・片付ける（Step 6：デバッグ表示の箱のタップと枠の表示を確かめるため）。
        /// 本当の箱は B の StageBuilder が作る。
        /// </summary>
        public void ToggleSampleBoxes()
        {
            if (sampleBoxes != null)
            {
                Destroy(sampleBoxes);
                ScanContext.WorkingBoxes.Clear();
                StageContext.StageRoot = null;
                return;
            }
            // 再生をまたいで前の箱が残っていることがあるので、先に空にする（Domain Reload なしの設定のため）
            ScanContext.WorkingBoxes.Clear();
            // B の StageBuilder と同じ形にする：StageRoot の子に「Desk」と「box-…」
            sampleBoxes = new GameObject("DEV_SampleBoxes");
            sampleBoxes.AddComponent<DevMarker>();
            StageContext.StageRoot = sampleBoxes.transform;
            AddSampleBox("Desk", new Vector3(0f, -0.005f, 0f), new Vector3(0.8f, 0.01f, 0.5f), null);
            // box-01 と box-02 は接して並べ、隣どうしで色が違うかを確かめられるようにする
            AddSampleBox("box-01", new Vector3(-0.2f, 0.03f, 0f), new Vector3(0.12f, 0.06f, 0.12f), BoxData.SourceAuto);
            AddSampleBox("box-02", new Vector3(-0.09f, 0.04f, 0.02f), new Vector3(0.1f, 0.08f, 0.15f), BoxData.SourceAuto);
            AddSampleBox("box-03", new Vector3(0.2f, 0.015f, -0.05f), new Vector3(0.15f, 0.03f, 0.1f), BoxData.SourceManual);
            StageContext.RaiseStageBuilt();
        }

        void AddSampleBox(string id, Vector3 center, Vector3 size, string source)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = id;
            box.layer = Layers.Terrain;
            box.transform.SetParent(sampleBoxes.transform, false);
            box.transform.localPosition = center;
            box.transform.localScale = size;
            if (source != null)
            {
                ScanContext.WorkingBoxes.Add(new BoxData
                {
                    id = id,
                    center = new[] { center.x, center.y, center.z },
                    size = new[] { size.x, size.y, size.z },
                    source = source,
                });
            }
        }

        // A が机を決めると StageRoot と机の大きさができる。まだなければ仮の物を用意する
        void EnsureDesk()
        {
            if (StageContext.StageRoot == null)
            {
                var root = new GameObject("DEV_StageRoot");
                root.AddComponent<DevMarker>();
                StageContext.StageRoot = root.transform;
            }
            if (ScanContext.DeskSize == Vector2.zero)
            {
                ScanContext.DeskSize = new Vector2(0.8f, 0.5f);
            }
        }

        public void RaiseDeskDecided()
        {
            EnsureDesk();
            StageContext.RaiseDeskDecided();
        }

        int foundCount;

        /// <summary>
        /// 上面が1つ見つかり、箱が1つ足されたことにする（Step 8：スキャン画面の演出を確かめるため）。
        /// 本当は A が Surfaces に書き、B が WorkingBoxes に箱を足して BoxesChanged を出す。
        /// </summary>
        public void RaiseBoxFound()
        {
            EnsureDesk();
            if (ScanContext.WorkingBoxes.Count == 0)
            {
                foundCount = 0;
            }
            foundCount++;
            var x = -0.3f + 0.15f * ((foundCount - 1) % 5);
            var z = (foundCount - 1) / 5 % 2 == 0 ? -0.05f : 0.12f;
            var height = 0.02f + 0.015f * (foundCount % 4);
            var half = 0.05f;
            ScanContext.Surfaces.Add(new SurfaceData
            {
                Id = 1000 + foundCount,
                Height = height,
                Outline = new[]
                {
                    new Vector2(x - half, z - half), new Vector2(x + half, z - half),
                    new Vector2(x + half, z + half), new Vector2(x - half, z + half),
                },
                UpdatedAt = Time.time,
            });
            ScanContext.RaiseSurfacesChanged();
            var id = "box-" + (90 + foundCount).ToString("00");
            ScanContext.WorkingBoxes.Add(new BoxData
            {
                id = id,
                center = new[] { x, height * 0.5f, z },
                size = new[] { half * 2f, height, half * 2f },
                source = BoxData.SourceAuto,
            });
            ScanContext.RaiseBoxesChanged(id, BoxChange.Added, BoxData.SourceAuto);
        }

        public void RaiseStageBuilt() => StageContext.RaiseStageBuilt();

        public void RaiseSaveOk() => StageContext.RaiseSaveCompleted(true);

        /// <summary>コツの絵を「まだ見ていない」に戻す（Step 9：1回目の見え方を確かめ直すため）。</summary>
        public void ResetTipsSeen()
        {
            // コツの画面を表示中なら、その場で絵の見え方に戻す
            var tips = FindAnyObjectByType<TipsScreen>(FindObjectsInactive.Include);
            if (tips != null)
            {
                tips.ResetSeen();
            }
            else
            {
                PlayerPrefs.DeleteKey(TipsScreen.SeenKey);
            }
            Debug.Log("DEV コツの絵を、まだ見ていないことにしました");
        }

        /// <summary>ゴールした知らせを出して、クリア画面へ移る。</summary>
        public void RaiseGoal()
        {
            GameEvents.RaiseGoal(12.34f, true);
            router.Show(ScreenId.Clear);
        }
    }
}
