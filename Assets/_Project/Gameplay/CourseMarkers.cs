using System.Collections.Generic;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Gameplay
{
    /// <summary>
    /// スタート・ゴール・チェックポイントの目印と、触れた判定（C_gameplay.md の Step 14）。
    /// 目印は仮の見た目（ゴール＝高さ3cmの旗、チェックポイント＝平らな円盤、スタート＝矢印）で、当たり判定は付けない。
    /// 触れた判定は物理のトリガーを使わず、毎フレーム距離を測る。
    /// </summary>
    public class CourseMarkers : MonoBehaviour
    {
        public GameFlow gameFlow;
        public CharacterSettings settings;

        Transform markerRoot;
        // このプレイで、もう触れたチェックポイントの番号
        readonly HashSet<int> passed = new HashSet<int>();

        void OnEnable()
        {
            StageContext.StageBuilt += Rebuild;
            GameEvents.GameStarted += OnGameStarted;
        }

        void OnDisable()
        {
            StageContext.StageBuilt -= Rebuild;
            GameEvents.GameStarted -= OnGameStarted;
        }

        /// <summary>
        /// ステージデータの spawn、goal、checkpoints の位置に目印を出し直す。Step 12 でコースの物を置いたときにも呼ぶ。
        /// </summary>
        public void Rebuild()
        {
            if (markerRoot != null)
            {
                Destroy(markerRoot.gameObject);
            }
            var stage = StageContext.CurrentStage;
            if (stage == null)
            {
                return;
            }
            markerRoot = new GameObject("CourseMarkers").transform;
            markerRoot.SetParent(StageContext.StageRoot, false);

            CreateStartArrow(Vec3.To(stage.spawn.position), stage.spawn.yawDegrees);
            CreateGoalFlag(Vec3.To(stage.goal.position), stage.goal.radius);
            for (int i = 0; i < stage.checkpoints.Length; i++)
            {
                var cp = stage.checkpoints[i];
                CreateDisk($"Checkpoint_{i}", Vec3.To(cp.position), cp.radius, new Color(0.3f, 0.6f, 1f));
            }
        }

        void OnGameStarted()
        {
            // スタートからやり直すときは、チェックポイントの通過も最初から
            passed.Clear();
        }

        void Update()
        {
            var stage = StageContext.CurrentStage;
            if (gameFlow == null || gameFlow.State != GameState.Playing || gameFlow.Motor == null || stage == null)
            {
                return;
            }

            // キャラの体の中心と各点の距離を、どちらもステージ座標にそろえて測る
            var root = StageContext.StageRoot;
            Vector3 feet = gameFlow.Motor.transform.position;
            Vector3 body = feet + Vector3.up * settings.centerY;
            if (root != null)
            {
                body = root.InverseTransformPoint(body);
            }

            for (int i = 0; i < stage.checkpoints.Length; i++)
            {
                var cp = stage.checkpoints[i];
                if (!CourseMath.IsTouching(body, Vec3.To(cp.position), cp.radius, settings.radius))
                {
                    continue;
                }
                // 戻り先は「最後に踏んだ」チェックポイント。一度通った所を踏み直しても更新する
                gameFlow.LastCheckpoint = i;
                // 通過の知らせは、初めて触れたときだけ
                if (passed.Add(i))
                {
                    GameEvents.RaiseCheckpointPassed(i);
                }
            }

            if (CourseMath.IsTouching(body, Vec3.To(stage.goal.position), stage.goal.radius, settings.radius))
            {
                gameFlow.ReachGoal();
            }
        }

        // ---- 仮の見た目 ----

        // スタート：進む向きを指す平たい矢印
        void CreateStartArrow(Vector3 position, float yaw)
        {
            var arrow = new GameObject("Start").transform;
            arrow.SetParent(markerRoot, false);
            arrow.localPosition = position + Vector3.up * 0.0005f;
            arrow.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var color = new Color(1f, 0.85f, 0.2f);
            CreatePart(PrimitiveType.Cube, arrow, new Vector3(0f, 0f, -0.004f), new Vector3(0.006f, 0.001f, 0.016f), Quaternion.identity, color);
            CreatePart(PrimitiveType.Cube, arrow, new Vector3(0f, 0f, 0.006f), new Vector3(0.01f, 0.001f, 0.01f), Quaternion.Euler(0f, 45f, 0f), color);
        }

        // ゴール：高さ3cmの旗と、触れる範囲の円盤
        void CreateGoalFlag(Vector3 position, float radius)
        {
            // ゴールは金色（2026-11-07。見つけやすいように。前は赤）
            var gold = new Color(1f, 0.78f, 0.12f);
            var goal = CreateDisk("Goal", position, radius, gold);
            // 棒（高さ3cm）と、てっぺんの旗
            // 旗：棒も旗も金色で、光る質感にする。旗は大きめ（2.4cm × 1.6cm）にして、遠くからでも見えるように
            CreatePart(PrimitiveType.Cylinder, goal, new Vector3(0f, 0.02f, 0f), new Vector3(0.003f, 0.02f, 0.003f), Quaternion.identity, gold, true);
            CreatePart(PrimitiveType.Cube, goal, new Vector3(0.012f, 0.032f, 0f), new Vector3(0.024f, 0.016f, 0.0015f), Quaternion.identity, gold, true);
        }

        // 触れる範囲を示す平らな円盤
        Transform CreateDisk(string name, Vector3 position, float radius, Color color)
        {
            var disk = new GameObject(name).transform;
            disk.SetParent(markerRoot, false);
            disk.localPosition = position;
            CreatePart(PrimitiveType.Cylinder, disk, new Vector3(0f, 0.0005f, 0f), new Vector3(radius * 2f, 0.0005f, radius * 2f), Quaternion.identity, color);
            return disk;
        }

        // 当たり判定を外した形を作る
        static Shader markerShader;

        // 目印の素材のシェーダー：URP の Lit（箱の素材が使っているので、ビルドに入っている）。無ければ Unlit、それも無ければ Sprites/Default
        static Shader MarkerShader()
        {
            if (markerShader == null)
            {
                markerShader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Sprites/Default");
            }
            return markerShader;
        }

        static void CreatePart(PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 scale, Quaternion rotation, Color color, bool shiny = false)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = rotation;
            go.transform.localScale = scale;
            // Unity の標準の素材（CreatePrimitive が付ける物）は、このアプリ（URP）のビルドでは描けず、濃いピンクになる。
            // ビルドに入っている URP の素材で作り直す（2026-11-07：ゴールがピンクのままだった）
            var material = new Material(MarkerShader()) { color = color };
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (shiny)
            {
                // 金属の光沢（URP の Lit の項目。無い素材では何も起きない）
                material.SetFloat("_Metallic", 0.75f);
                material.SetFloat("_Smoothness", 0.8f);
            }
        }
    }

    /// <summary>コースの物の計算（触れた判定）。EditMode でテストする。</summary>
    public static class CourseMath
    {
        /// <summary>
        /// キャラの体の中心と点の距離が「半径＋キャラの半径」以下なら触れたとする。
        /// </summary>
        public static bool IsTouching(Vector3 bodyCenter, Vector3 point, float radius, float characterRadius)
        {
            float reach = radius + characterRadius;
            return (bodyCenter - point).sqrMagnitude <= reach * reach;
        }
    }
}
