using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Gameplay
{
    /// <summary>
    /// ゲームの進行（C_gameplay.md の Step 11）。B の StageBuilt と D の依頼（Play、SetPaused、Retry、BackToList）を受けて、
    /// キャラを出し、スタートへ移し、タイムを数える。状態の決まりは GameFlowState に任せる。
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        public GameObject playerPrefab;
        public ViewCamera viewCamera;
        public CameraRelativeInput cameraRelativeInput;

        readonly GameFlowState flow = new GameFlowState();

        public GameState State => flow.State;

        /// <summary>今出ているキャラ（いなければ null）</summary>
        public CharacterMotor Motor { get; private set; }

        /// <summary>プレイ中のフレームだけ足し上げたタイム（秒）</summary>
        public float ElapsedSeconds { get; private set; }

        /// <summary>最後に通ったチェックポイントの番号（なければ -1）。Step 14・15 で使う</summary>
        public int LastCheckpoint { get; set; } = -1;

        void OnEnable()
        {
            StageContext.StageBuilt += OnStageBuilt;
            UiRequests.Play += OnPlay;
            UiRequests.SetPaused += OnSetPaused;
            UiRequests.Retry += OnRetry;
            UiRequests.BackToList += OnBackToList;
        }

        void OnDisable()
        {
            StageContext.StageBuilt -= OnStageBuilt;
            UiRequests.Play -= OnPlay;
            UiRequests.SetPaused -= OnSetPaused;
            UiRequests.Retry -= OnRetry;
            UiRequests.BackToList -= OnBackToList;
        }

        void Update()
        {
            if (flow.State != GameState.Playing)
            {
                return;
            }
            // 一時停止中は数えないよう、プレイ中のフレームだけ足し上げる
            ElapsedSeconds += Time.deltaTime;
            GameEvents.RaiseElapsedTime(ElapsedSeconds);
        }

        /// <summary>ゴールに触れた。Step 14 の CourseMarkers が呼ぶ。</summary>
        public void ReachGoal()
        {
            if (!flow.TryGoal())
            {
                Debug.Log($"[Gameplay] {flow.State} ではゴールにできません");
                return;
            }
            Motor.Paused = true;
            // 最速記録との比べは Step 16 の RecordStore で行う
            GameEvents.RaiseGoal(ElapsedSeconds, false);
        }

        void OnStageBuilt()
        {
            flow.OnStageBuilt();
        }

        void OnPlay()
        {
            if (!flow.TryPlay())
            {
                Debug.Log($"[Gameplay] {flow.State}（ステージ {(flow.StageReady ? "あり" : "なし")}）では Play できません");
                return;
            }
            SpawnPlayer();
            Prepare();
        }

        void OnSetPaused(bool paused)
        {
            if (!flow.TrySetPaused(paused))
            {
                Debug.Log($"[Gameplay] {flow.State} では {(paused ? "一時停止" : "再開")} できません");
                return;
            }
            Motor.Paused = paused;
        }

        void OnRetry()
        {
            if (!flow.TryRetry())
            {
                Debug.Log($"[Gameplay] {flow.State} では Retry できません");
                return;
            }
            LastCheckpoint = -1;
            Prepare();
        }

        void OnBackToList()
        {
            flow.BackToList();
            if (Motor != null)
            {
                Destroy(Motor.gameObject);
                Motor = null;
            }
            if (viewCamera != null) viewCamera.SetTarget(null);
            if (cameraRelativeInput != null) cameraRelativeInput.SetMotor(null);
        }

        // ステージ待ち → 準備：キャラを StageRoot の子に作る
        void SpawnPlayer()
        {
            if (Motor != null)
            {
                return;
            }
            var root = StageContext.StageRoot;
            if (root == null)
            {
                Debug.LogWarning("[Gameplay] StageRoot がないので、ワールドの原点にキャラを出します");
            }
            var go = Instantiate(playerPrefab, root);
            Motor = go.GetComponent<CharacterMotor>();
            if (viewCamera != null) viewCamera.SetTarget(go.transform);
            if (cameraRelativeInput != null) cameraRelativeInput.SetMotor(Motor);
        }

        // 準備 → プレイ中：スタートへ瞬間移動、タイムを0にして開始、GameStarted
        void Prepare()
        {
            var root = StageContext.StageRoot;
            var spawn = StageContext.CurrentStage != null ? StageContext.CurrentStage.spawn : new SpawnData();
            Vector3 local = Vec3.To(spawn.position) + Vector3.up * 0.001f;
            Vector3 position = root != null ? root.TransformPoint(local) : local;
            Quaternion rotation = (root != null ? root.rotation : Quaternion.identity) * Quaternion.Euler(0f, spawn.yawDegrees, 0f);

            Motor.Paused = false;
            Motor.Teleport(position, rotation);
            ElapsedSeconds = 0f;
            flow.TryStart();
            GameEvents.RaiseGameStarted();
        }
    }
}
