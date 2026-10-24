using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.Gameplay
{
    /// <summary>
    /// たしかめ画面で、子どもがタップした本物の物の上に、スタート・ゴール・チェックポイントを置く（C_gameplay.md の Step 12）。
    /// D の PlaceCourseItem を受け、AR のカメラからタップした位置へ光線を飛ばす。相手は Terrain（B の箱と机の板）だけ。
    /// 保存は B が行う。C はステージデータを書き換えて、目印を出し直すだけ。
    /// </summary>
    public class CoursePlacer : MonoBehaviour
    {
        [Tooltip("置くときに使うカメラ。空か、隠れているときは Camera.main（A の AR のカメラ）")]
        public Camera placementCamera;

        public CourseMarkers courseMarkers;

        [Tooltip("行けるかの判定に使う段の境目（StepRules）。空なら初期値")]
        public CharacterSettings settings;

        [Tooltip("光線の長さ（m）")]
        public float maxDistance = 3f;

        void OnEnable()
        {
            UiRequests.PlaceCourseItem += OnPlaceCourseItem;
        }

        void OnDisable()
        {
            UiRequests.PlaceCourseItem -= OnPlaceCourseItem;
        }

        void OnPlaceCourseItem(CourseItem item, Vector2 screenPosition)
        {
            var stage = StageContext.CurrentStage;
            var cam = placementCamera != null && placementCamera.isActiveAndEnabled ? placementCamera : Camera.main;
            if (stage == null || cam == null)
            {
                Debug.LogWarning("[Gameplay] ステージデータかカメラがないので、コースの物を置けません");
                return;
            }

            // 1〜3：タップした位置へ光線を飛ばし、机か箱の上向きの面に当たったかを見る
            var ray = cam.ScreenPointToRay(screenPosition);
            bool hit = Physics.Raycast(ray, out var info, maxDistance, Layers.TerrainMask, QueryTriggerInteraction.Ignore);
            string reason = CoursePlacement.CheckSurface(hit, hit ? info.normal : Vector3.zero);
            if (reason != null)
            {
                GameEvents.RaiseCoursePlaced(item, PlacementResult.Rejected, reason);
                return;
            }

            // 4・5：ステージ座標に直して書く。スタートの向きは、そのときのスマホの水平な向き（置いた子どもから見て奥）
            var root = StageContext.StageRoot;
            Vector3 local = root != null ? root.InverseTransformPoint(info.point) : info.point;
            Vector3 forward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
            Vector3 localForward = root != null ? root.InverseTransformDirection(forward) : forward;
            float yaw = CoursePlacement.YawFromDirection(localForward);

            // 6：スタートとゴールは1つずつ（置き直すと動く）。チェックポイントは最大5個、近くをタップしたら消す
            var (result, why) = CoursePlacement.Apply(stage, item, local, yaw);

            // 7：スタートからゴールへ、Wall の段を通らないと行けないときも置くのは止めず、「とどかないかも」を返す
            if (result == PlacementResult.Placed && item != CourseItem.Checkpoint)
            {
                var rules = settings != null ? settings.CreateStepRules() : new StepRules();
                if (!CourseReachability.CanReach(stage, rules, Vec3.To(stage.spawn.position), Vec3.To(stage.goal.position)))
                {
                    result = PlacementResult.MayBeUnreachable;
                    why = CoursePlacement.ReasonMayBeUnreachable;
                }
            }

            // 8：置いた場所に目印を出し直す
            if (courseMarkers != null)
            {
                courseMarkers.Rebuild();
            }
            GameEvents.RaiseCoursePlaced(item, result, why);
        }
    }
}
