using System.Linq;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Gameplay
{
    /// <summary>
    /// 落下とはさまりからの復帰（C_gameplay.md の Step 15）。
    /// 落ちたら、または はさまって動けなくなったら、最後のチェックポイント（なければスタート）へ戻す。タイムは止めない。
    /// </summary>
    public class SafetyGuard : MonoBehaviour
    {
        public GameFlow gameFlow;
        public CharacterSettings settings;

        [Tooltip("落ちたと判定してから、さらに落ち続けて見せる時間（秒）。この後で戻す")]
        public float fallRespawnDelay = 1f;

        [Tooltip("落ちて戻った直後に、入力を受け付けない時間（秒）")]
        public float inputLockAfterFall = 1f;

        // 落ちたと判定した時刻（落ちていなければ null）
        float? fallingSince;

        [Tooltip("スティックを倒しているのに、この時間ほとんど動けなければ はさまったとみなす（秒）")]
        public float stuckSeconds = 3f;

        [Tooltip("はさまったときに、近くの箱としてログに出す範囲（m）")]
        public float nearbyRadius = 0.03f;

        [Tooltip("確かめ用：地面に立って壁を押しているときも、はさまりとみなす（ふだんは切る。壁を押しただけで戻されないように）")]
        public bool countWallPushAsStuck;

        readonly StuckDetector stuck = new StuckDetector();

        void OnEnable()
        {
            GameEvents.GameStarted += ResetState;
        }

        void OnDisable()
        {
            GameEvents.GameStarted -= ResetState;
        }

        void ResetState()
        {
            stuck.Reset();
            fallingSince = null;
        }

        void Update()
        {
            var motor = gameFlow != null ? gameFlow.Motor : null;
            if (motor == null || gameFlow.State != GameState.Playing)
            {
                stuck.Reset();
                return;
            }

            Vector3 feet = motor.transform.position;
            var root = StageContext.StageRoot;
            float localFeetY = root != null ? root.InverseTransformPoint(feet).y : feet.y;
            float fallDepth = StageContext.CurrentStage != null ? StageContext.CurrentStage.fallDepth : 0.10f;

            // 1：落下。足元が「0 − fallDepth」より下になったら Fell を出し、少し落ち続けて見せてから戻す
            if (fallingSince == null && SafetyMath.HasFallen(localFeetY, fallDepth))
            {
                fallingSince = Time.time;
                GameEvents.RaiseFell();
            }
            if (fallingSince != null)
            {
                if (Time.time - fallingSince.Value >= fallRespawnDelay)
                {
                    gameFlow.RespawnAtLastCheckpoint();
                    motor.LockInput(inputLockAfterFall);
                    ResetState();
                }
                return;
            }

            // 2：はさまり。前が壁で止まっているだけ（地面に立って壁を押している）なら、はさまりとはみなさない
            bool pushingWall = !countWallPushAsStuck && motor.IsGrounded && WallAhead(feet, motor.MoveDirection);
            bool pushing = motor.Stick.magnitude >= StuckDetector.MinStick && !pushingWall;
            if (stuck.Update(Time.time, feet, pushing, stuckSeconds))
            {
                Debug.Log($"[Gameplay] はさまったので戻します。近くの箱：{NearbyBoxIds(feet)}");
                gameFlow.RespawnAtLastCheckpoint();
                stuck.Reset();
            }
        }

        // 進む向きのすぐ前に、箱の面があるか
        bool WallAhead(Vector3 feet, Vector3 dir)
        {
            if (dir == Vector3.zero)
            {
                return false;
            }
            var body = feet + Vector3.up * settings.centerY;
            return Physics.SphereCast(body, settings.radius * 0.9f, dir, out _, 0.01f, Layers.TerrainMask, QueryTriggerInteraction.Ignore);
        }

        // 近くの箱の id（B は箱の GameObject に id を名前として付けている）
        string NearbyBoxIds(Vector3 feet)
        {
            var body = feet + Vector3.up * settings.centerY;
            var names = Physics.OverlapSphere(body, nearbyRadius, Layers.TerrainMask, QueryTriggerInteraction.Ignore)
                .Select(c => c.name)
                .Distinct()
                .OrderBy(n => n)
                .ToArray();
            return names.Length > 0 ? string.Join("、", names) : "なし";
        }
    }

    /// <summary>落下の判定（Step 15）。EditMode でテストする。</summary>
    public static class SafetyMath
    {
        /// <summary>足元の高さ（ステージ座標）が「0 − fallDepth」より下なら落ちたとする。</summary>
        public static bool HasFallen(float localFeetY, float fallDepth)
        {
            return localFeetY < -fallDepth;
        }
    }

    /// <summary>
    /// はさまりの判定（Step 15）。スティックを倒している間に、ほとんど動けない時間が続いたら true を返す。
    /// </summary>
    public class StuckDetector
    {
        /// <summary>これ以上スティックを倒していたら「進もうとしている」とみなす</summary>
        public const float MinStick = 0.5f;

        /// <summary>この距離より動いたら「動けている」とみなす（m）</summary>
        public const float MinMove = 0.005f;

        Vector3 anchor;
        float anchorTime;
        bool tracking;

        public void Reset()
        {
            tracking = false;
        }

        /// <summary>
        /// 毎フレーム呼ぶ。pushing はスティックを倒して進もうとしているか。
        /// 進もうとしたまま seconds 秒の間、MinMove より動けなければ true。
        /// </summary>
        public bool Update(float time, Vector3 position, bool pushing, float seconds)
        {
            if (!pushing)
            {
                tracking = false;
                return false;
            }
            if (!tracking || Vector3.Distance(position, anchor) > MinMove)
            {
                anchor = position;
                anchorTime = time;
                tracking = true;
                return false;
            }
            return time - anchorTime >= seconds;
        }
    }
}
