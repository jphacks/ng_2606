using UnityEngine;

namespace DeskStage.App
{
    /// <summary>箱のしかけの決まり（どこに乗ったか、水が流す向き、回復の間隔など）。3D に触れない計算だけ。EditMode テストで確かめる。</summary>
    public static class GimmickRules
    {
        /// <summary>水が流す速さ（m/秒）。歩く速さ（0.15）の 3 分の 1。「少し」流される。</summary>
        public const float WaterSpeed = 0.05f;

        /// <summary>水の坂が流す速さ（m/秒）。歩く速さ（0.15）より速いので、流れにさからっては進めない（下り向きなら下りるだけ、上り向きなら流れに乗ってのぼれる）。</summary>
        public const float WaterRampSpeed = 0.25f;

        // 水の坂は、ジャンプで浮いている間も流す（跳んでのぼれないように）。2 段ジャンプの高さ（約 24cm）より上まで
        const float AboveRampWater = 0.30f;

        /// <summary>
        /// 水の坂から跳んで、浮いている間に流す速さ（m/秒）。歩く速さ（0.15）と同じにして、跳んでものぼれないが、強く運ばれて飛ばされないようにする（2026-11-07）。
        /// </summary>
        public const float WaterRampAirSpeed = 0.15f;

        /// <summary>水に流される速さの合計の上限（m/秒）。水の箱が重なっていても、これより速く流さない（吹っ飛ばないように）。</summary>
        public const float MaxWaterPush = WaterRampSpeed;

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

        /// <summary>敵の見た目の大きさ（m。直径）。当たりの大きさ（EnemyRadius）より大きく見せて、見つけやすくする（2026-11-07：2.4cm → 3.6cm）。</summary>
        public const float EnemyVisualSize = 0.036f;

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
        public static bool IsOnTop(Vector3 local, float heightScale, bool ramp = false)
        {
            if (Mathf.Abs(local.x) > 0.5f || Mathf.Abs(local.z) > 0.5f)
            {
                return false;
            }
            // 坂は、斜めの面（箱の座標で y = x）からの高さを見る
            var surfaceY = ramp ? local.x : 0.5f;
            var above = (local.y - surfaceY) * heightScale;
            return above >= -BelowTop && above <= AboveTop;
        }

        /// <summary>
        /// 坂の上（斜めの面の上、ジャンプで浮いている間も含む）にいるか。水の坂が流すかどうかに使う。local は箱の座標、heightScale は箱の高さ（m）。
        /// </summary>
        public static bool IsOverRamp(Vector3 local, float heightScale) => IsOverRamp(local, heightScale, false);

        /// <summary>
        /// 坂の上にいるか。grounded（地面に立っている）なら、斜めの面のすぐ上（乗っている高さ）のときだけ。
        /// 坂の上の空中でも流すのは、跳んで浮いている間だけにする（坂に重なった、ほかの箱の上に立っているときに、強く流されて吹っ飛ばないように。2026-11-07）
        /// </summary>
        public static bool IsOverRamp(Vector3 local, float heightScale, bool grounded)
        {
            if (grounded)
            {
                return IsOnTop(local, heightScale, true);
            }
            if (Mathf.Abs(local.x) > 0.5f || Mathf.Abs(local.z) > 0.5f)
            {
                return false;
            }
            var above = (local.y - local.x) * heightScale;
            return above >= -BelowTop && above <= AboveRampWater;
        }

        /// <summary>
        /// 水の流れる向き（箱の座標の x・z の、どれか 1 方向）。flow が 1〜4 なら、選んだ向き（+x、+z、-x、-z）。
        /// 0 なら、箱の id から決める（同じ id なら、いつも同じ向き）。
        /// </summary>
        public static Vector2 WaterDirection(string boxId, int flow = 0)
        {
            switch (WaterIndex(boxId, flow))
            {
                case 0: return new Vector2(1f, 0f);
                case 1: return new Vector2(0f, 1f);
                case 2: return new Vector2(-1f, 0f);
                default: return new Vector2(0f, -1f);
            }
        }

        /// <summary>水の向きの番号（0〜3 ＝ +x、+z、-x、-z）。flow が 1〜4 なら、選んだ向き。0 なら id から。</summary>
        public static int WaterIndex(string boxId, int flow = 0)
        {
            if (flow >= 1 && flow <= 4)
            {
                return flow - 1;
            }
            unchecked
            {
                var hash = 17;
                foreach (var ch in boxId ?? "")
                {
                    hash = hash * 31 + ch;
                }
                return ((hash % 4) + 4) % 4;
            }
        }

        /// <summary>押すたびに、次の向き（90 度まわる）にする。いま見えている向きから、次へ。戻りは flow（1〜4）。</summary>
        public static int NextFlow(string boxId, int flow)
        {
            return (WaterIndex(boxId, flow) + 1) % 4 + 1;
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
