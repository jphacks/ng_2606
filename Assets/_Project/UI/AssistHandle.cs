using UnityEngine;
using UnityEngine.EventSystems;

namespace DeskStage.UI
{
    /// <summary>
    /// 選んだ箱の上に出す「上下の取っ手」（Step 11）。上下になぞった量を AssistController に渡す。
    /// </summary>
    public class AssistHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] AssistController controller;

        public void SetController(AssistController value) => controller = value;

        public void OnBeginDrag(PointerEventData e) => controller.BeginHeight();

        public void OnDrag(PointerEventData e) => controller.DragHeight(e.position.y - e.pressPosition.y);

        public void OnEndDrag(PointerEventData e) => controller.EndEdit();
    }
}
