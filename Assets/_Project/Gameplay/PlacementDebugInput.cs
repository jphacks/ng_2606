using DeskStage.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeskStage.Gameplay
{
    /// <summary>
    /// 練習用シーンでコースの配置（Step 12）を試すための仮の操作。D のたしかめ画面の代わりに PlaceCourseItem を出す。
    /// 1（スタート）・2（ゴール）・3（チェックポイント）を押しながら、Game ビューを左クリックすると置く。結果はログに出す。
    /// </summary>
    public class PlacementDebugInput : MonoBehaviour
    {
        void OnEnable() => GameEvents.CoursePlaced += OnCoursePlaced;

        void OnDisable() => GameEvents.CoursePlaced -= OnCoursePlaced;

        void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            if (keyboard == null || mouse == null || !mouse.leftButton.wasPressedThisFrame)
            {
                return;
            }
            Vector2 position = mouse.position.ReadValue();
            if (keyboard.digit1Key.isPressed) UiRequests.RequestPlaceCourseItem(CourseItem.Start, position);
            else if (keyboard.digit2Key.isPressed) UiRequests.RequestPlaceCourseItem(CourseItem.Goal, position);
            else if (keyboard.digit3Key.isPressed) UiRequests.RequestPlaceCourseItem(CourseItem.Checkpoint, position);
        }

        void OnCoursePlaced(CourseItem item, PlacementResult result, string reason)
        {
            Debug.Log($"[Gameplay] {item} を置く：{result}{(string.IsNullOrEmpty(reason) ? "" : "（" + reason + "）")}");
        }
    }
}
