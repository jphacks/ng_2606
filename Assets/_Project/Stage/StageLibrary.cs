using System;
using System.IO;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Stage
{
    /// <summary>
    /// 端末のステージの一覧・読み込み・保存・消す（B の Step 10・11）。ファイルの扱いは StageStorage。
    /// ・起動したら、同梱用のステージを保存用へコピーし、一覧を StageContext.StageList に入れる
    /// ・SelectStage：読み込んで中身を確かめ、StageLoaded を出して組み立てる（組み立て終わると StageBuilder が StageBuilt を出す）
    /// ・SaveStage：今のステージ（たしかめ画面の箱とコースの物）を保存し、SaveCompleted を出す
    /// ・DeleteStage：消して、一覧を作り直す
    /// </summary>
    public class StageLibrary : MonoBehaviour
    {
        [SerializeField] StageBuilder builder;

        StageStorage storage;

        void Awake()
        {
            storage = new StageStorage(
                Path.Combine(Application.streamingAssetsPath, "Stages"),
                Path.Combine(Application.persistentDataPath, "Stages"));
        }

        void OnEnable()
        {
            UiRequests.SelectStage += OnSelectStage;
            UiRequests.SaveStage += OnSaveStage;
            UiRequests.DeleteStage += OnDeleteStage;
        }

        void OnDisable()
        {
            UiRequests.SelectStage -= OnSelectStage;
            UiRequests.SaveStage -= OnSaveStage;
            UiRequests.DeleteStage -= OnDeleteStage;
        }

        void Start()
        {
            var copied = storage.CopyBundled();
            if (copied > 0)
            {
                Debug.Log($"[Stage] 同梱のステージを {copied} 個コピーしました");
            }
            Refresh();
        }

        /// <summary>一覧を作り直して、StageListChanged を出す。</summary>
        public void Refresh()
        {
            StageContext.SetStageList(storage.List());
        }

        void OnSelectStage(string id)
        {
            if (!storage.TryLoad(id, out var stage, out var reason))
            {
                Debug.LogWarning($"[Stage] {id} を読み込めません：{reason}");
                return;
            }
            StageContext.RaiseStageLoaded(stage);
            if (builder != null)
            {
                builder.Build(stage);
            }
        }

        void OnSaveStage(string displayName)
        {
            var stage = StageSavePrep.Prepare(StageContext.CurrentStage, ScanContext.WorkingBoxes, displayName, DateTimeOffset.Now);
            var ok = storage.Save(stage, out var reason);
            if (ok)
            {
                Debug.Log($"[Stage] {stage.id}（{stage.name}）を保存しました：箱 {stage.boxes.Length} 個");
                Refresh();
            }
            else
            {
                Debug.LogWarning($"[Stage] 保存できませんでした：{reason}");
            }
            StageContext.RaiseSaveCompleted(ok, ok ? "" : reason);
        }

        void OnDeleteStage(string id)
        {
            if (storage.Delete(id))
            {
                Debug.Log($"[Stage] {id} を消しました");
                Refresh();
            }
            else
            {
                Debug.LogWarning($"[Stage] {id} を消せませんでした");
            }
        }
    }
}
