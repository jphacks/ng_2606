using DeskStage.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DeskStage.Merge
{
    /// <summary>
    /// たしかめ画面で、スタートとゴールを置く（合流のつなぎ）。
    /// ボタンを押すと、次に机の上をタップした所へ、UiRequests.PlaceCourseItem で置く（受けるのは C の CoursePlacer）。
    /// タップを受けている間は、D の補助の画面（assistArea）を一時的に外す（全画面でタップを受けているため）。
    /// D の「コースの物を置く」画面ができるまでの代わり。
    /// </summary>
    public class CoursePlaceView : MonoBehaviour
    {
        [SerializeField] Button startButton;
        [SerializeField] Button goalButton;
        [SerializeField] TMP_Text hint;
        [SerializeField] GameObject assistArea;

        CourseItem? armed;

        void Awake()
        {
            if (startButton != null) startButton.onClick.AddListener(() => Arm(CourseItem.Start));
            if (goalButton != null) goalButton.onClick.AddListener(() => Arm(CourseItem.Goal));
        }

        void OnEnable()
        {
            GameEvents.CoursePlaced += OnPlaced;
            Disarm();
            if (hint != null) hint.text = "";
        }

        void OnDisable()
        {
            GameEvents.CoursePlaced -= OnPlaced;
            Disarm();
        }

        void Arm(CourseItem item)
        {
            armed = item;
            if (assistArea != null) assistArea.SetActive(false);
            if (hint != null)
            {
                hint.text = item == CourseItem.Start
                    ? "つくえの うえを タップして スタートを おいてね"
                    : "つくえの うえを タップして ゴールを おいてね";
            }
        }

        void Disarm()
        {
            armed = null;
            if (assistArea != null) assistArea.SetActive(true);
        }

        void Update()
        {
            if (armed == null)
            {
                return;
            }
            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame)
            {
                return;
            }
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return; // ボタンの上のタップは、置く場所にしない
            }

            var item = armed.Value;
            Disarm();
            UiRequests.RequestPlaceCourseItem(item, pointer.position.ReadValue());
        }

        void OnPlaced(CourseItem item, PlacementResult result, string reason)
        {
            if (hint == null)
            {
                return;
            }
            var name = item == CourseItem.Start ? "スタート" : item == CourseItem.Goal ? "ゴール" : "チェックポイント";
            switch (result)
            {
                case PlacementResult.Rejected:
                    hint.text = $"{name}を おけなかったよ：{reason}";
                    break;
                case PlacementResult.MayBeUnreachable:
                    hint.text = $"{name}を おいたよ（とどかないかも：{reason}）";
                    break;
                default:
                    hint.text = $"{name}を おいたよ";
                    break;
            }
        }
    }
}
