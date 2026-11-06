using UnityEngine;

namespace DeskStage.App
{
    /// <summary>箱のしかけの決まり（どこに乗ったか、水が流す向き、回復の間隔など）。3D に触れない計算だけ。EditMode テストで確かめる。</summary>
    public static class GimmickRules
    {
        /// <summary>水が流す速さ（m/秒）。歩く速さ（0.15）の 3 分の 1。「少し」流される。</summary>
        public const float WaterSpeed = 0.05f;

        /// <summary>緑の箱で、ハートを 1 つ回復するまでの時間（秒）。</summary>
        public const float HealSeconds = 1f;

        /// <summary>黄色のジャンプ台で跳ね上がる高さ。ふつうのジャンプの高さの何倍か。</summary>
        public const float JumpPadHeightScale = 2f;

        /// <summary>ジャンプ台で跳ねたあと、続けて跳ねない時間（秒）。乗っている間に毎フレーム跳ねないように。</summary>
        public const float JumpPadCooldownSeconds = 0.3f;

        /// <summary>紫の箱から、敵が 1 体出るまでの時間（秒）。</summary>
        public const float EnemySpawnSeconds = 7f;

        /// <summary>机の上に、同時に出ている敵の最大の数。</summary>
        public const int MaxEnemies = 5;

        /// <summary>敵の歩く速さ（m/秒）。キャラの歩く速さ（0.15）の半分。</summary>
        public const float EnemySpeed = 0.075f;

        /// <summary>敵の大きさ（半径 m）。当たりの判定にも使う。</summary>
        public const float EnemyRadius = 0.01f;

        /// <summary>敵が登れる段差（m）。これより高い箱や壁は、通り抜けずに止まる。</summary>
        public const float EnemyMaxStep = 0.009f;

        /// <summary>うろうろする敵が、向きを変えるまでの時間（秒）。</summary>
        public const float EnemyWanderSeconds = 2f;

        /// <summary>ピンクのワープで飛んだあと、続けてワープしない時間（秒）。</summary>
        public const float WarpCooldownSeconds = 3f;

        /// <summary>ワープ先を、机のふちからこれだけ内側にする（m）。落ちないように。</summary>
        public const float WarpEdgeInset = 0.03f;

        /// <summary>敵の動き方。出るたびに、どちらかをくじで決める。</summary>
        public enum EnemyMode
        {
            /// <summary>キャラを追いかける</summary>
            Chase,
            /// <summary>ランダムに歩き回る</summary>
            Wander,
        }

        /// <summary>くじ（0〜1 の数）から、敵の動き方を決める。半分ずつ。</summary>
        public static EnemyMode ChooseEnemyMode(float random01) => random01 < 0.5f ? EnemyMode.Chase : EnemyMode.Wander;

        /// <summary>
        /// 次に出す敵の動き方。1 ステージの中で、追いかける敵と歩き回る敵の両方が出るように、
        /// 今出ている敵が全部同じ動き方なら、もう一方にする。両方いるか、まだ 1 体もいなければ、くじで決める。
        /// </summary>
        public static EnemyMode ChooseEnemyMode(float random01, int chaseCount, int wanderCount)
        {
            if (chaseCount > 0 && wanderCount == 0) return EnemyMode.Wander;
            if (wanderCount > 0 && chaseCount == 0) return EnemyMode.Chase;
            return ChooseEnemyMode(random01);
        }

        /// <summary>
        /// 敵どうしが重なるか（上から見た距離が、体 2 つ分より近く、高さもほぼ同じ）。
        /// </summary>
        public static bool EnemiesOverlap(Vector3 a, Vector3 b)
        {
            var flat = new Vector2(a.x - b.x, a.z - b.z);
            var reach = EnemyRadius * 2f;
            return flat.sqrMagnitude < reach * reach && Mathf.Abs(a.y - b.y) < EnemyRadius * 2f;
        }

        /// <summary>この時間ほとんど動けなければ、向きを変える（秒）。</summary>
        public const float EnemyStuckTurnSeconds = 1f;

        /// <summary>この時間たっても抜け出せなければ、出てきた紫の箱の上に戻す（秒）。</summary>
        public const float EnemyStuckRespawnSeconds = 4f;

        /// <summary>追いかける敵が壁で進めないとき、回り道（歩き回る）をする時間（秒）。</summary>
        public const float EnemyDetourSeconds = 1.5f;

        /// <summary>敵がキャラに当たったか（上から見た距離と高さの差で見る）。</summary>
        public static bool EnemyTouches(Vector3 enemy, Vector3 playerFeet, float playerRadius, float playerHeight)
        {
            var flat = new Vector2(enemy.x - playerFeet.x, enemy.z - playerFeet.z);
            var reach = EnemyRadius + playerRadius;
            return flat.sqrMagnitude <= reach * reach && enemy.y >= playerFeet.y - EnemyRadius && enemy.y <= playerFeet.y + playerHeight + EnemyRadius;
        }

        /// <summary>追いかける敵が、次に進む水平の向き（長さ1）。とても近ければゼロ。</summary>
        public static Vector3 ChaseDirection(Vector3 enemy, Vector3 target)
        {
            var d = new Vector3(target.x - enemy.x, 0f, target.z - enemy.z);
            return d.sqrMagnitude < 1e-8f ? Vector3.zero : d.normalized;
        }

        /// <summary>くじ（0〜1 の数）から、うろうろする敵の向き（水平、長さ1）を決める。</summary>
        public static Vector3 WanderDirection(float random01)
        {
            var angle = random01 * Mathf.PI * 2f;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }

        /// <summary>
        /// ワープ先の候補（ステージ座標の x・z）。机の長方形の中から、くじで選ぶ。ふちから inset だけ内側にする。
        /// その点の一番上の面（机か箱の上）に置くのは、呼ぶ側が光線で決める。
        /// </summary>
        public static Vector2 WarpCandidate(float random01X, float random01Z, float deskWidth, float deskDepth, float inset)
        {
            var hw = Mathf.Max(deskWidth / 2f - inset, 0f);
            var hd = Mathf.Max(deskDepth / 2f - inset, 0f);
            return new Vector2(Mathf.Lerp(-hw, hw, random01X), Mathf.Lerp(-hd, hd, random01Z));
        }

        // 足元が、箱の上面から この範囲にあるとき、「乗っている」（箱の上面の高さを基準に、下は 1cm、上は キャラの身長 3cm + 余裕）
        const float BelowTop = 0.01f;
        const float AboveTop = 0.035f;

        /// <summary>
        /// 箱の上に乗っているか。local は、箱の座標（箱の中心が原点で、1 辺が 1 の立方体）での、キャラの足元。
        /// heightScale は、箱の高さ（m）。上面からの高さを、m に直すために使う。
        /// </summary>
        public static bool IsOnTop(Vector3 local, float heightScale)
        {
            if (Mathf.Abs(local.x) > 0.5f || Mathf.Abs(local.z) > 0.5f)
            {
                return false;
            }
            var above = (local.y - 0.5f) * heightScale;
            return above >= -BelowTop && above <= AboveTop;
        }

        /// <summary>箱の id から、水の流れる向き（箱の座標の x・z の、どれか 1 方向）を決める。同じ id なら、いつも同じ向き。</summary>
        public static Vector2 WaterDirection(string boxId)
        {
            unchecked
            {
                var hash = 17;
                foreach (var ch in boxId ?? "")
                {
                    hash = hash * 31 + ch;
                }
                switch (((hash % 4) + 4) % 4)
                {
                    case 0: return new Vector2(1f, 0f);
                    case 1: return new Vector2(0f, 1f);
                    case 2: return new Vector2(-1f, 0f);
                    default: return new Vector2(0f, -1f);
                }
            }
        }

        /// <summary>しかけの名前（子ども向け）。</summary>
        public static string Name(string gimmick)
        {
            switch (gimmick)
            {
                case Shared.BoxData.GimmickWater: return AppText.GimmickWater;
                case Shared.BoxData.GimmickFire: return AppText.GimmickFire;
                case Shared.BoxData.GimmickHeal: return AppText.GimmickHeal;
                case Shared.BoxData.GimmickJumpPad: return AppText.GimmickJumpPad;
                case Shared.BoxData.GimmickEnemy: return AppText.GimmickEnemy;
                case Shared.BoxData.GimmickWarp: return AppText.GimmickWarp;
                default: return "";
            }
        }
    }
}
