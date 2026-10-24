using DeskStage.Shared;
using TMPro;
using UnityEngine;

namespace DeskStage.UI
{
    /// <summary>
    /// スキャン画面の上の中央に、スキャン状態に合った1行の案内を出す（Step 5）。
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class GuidanceView : MonoBehaviour
    {
        [SerializeField] Wording wording;

        readonly GuidanceLogic logic = new GuidanceLogic();
        TMP_Text text;
        string shownKey;

        public void SetWording(Wording value) => wording = value;

        /// <summary>
        /// 机の面が見つかったかを伝える。共通コードにこの知らせがまだないため、受け口だけを用意している。
        /// （A と相談して知らせが決まったら、そのイベントにつなぐ）
        /// </summary>
        public void SetDeskFound(bool found)
        {
            logic.DeskFound = found;
            Refresh();
        }

        void OnEnable()
        {
            text = GetComponent<TMP_Text>();
            logic.SetStatus(StageContext.ScanStatus, StageContext.ScanReason);
            StageContext.ScanStatusChanged += OnStatusChanged;
            Refresh();
        }

        void OnDisable() => StageContext.ScanStatusChanged -= OnStatusChanged;

        void Update()
        {
            logic.Tick(Time.unscaledDeltaTime);
            Refresh();
        }

        void OnStatusChanged(ScanStatus status, ScanReason reason)
        {
            logic.SetStatus(status, reason);
            Refresh();
        }

        void Refresh()
        {
            var key = logic.CurrentKey;
            if (key == shownKey)
            {
                return;
            }
            shownKey = key;
            text.text = key == null || wording == null ? "" : wording.Get(key);
        }
    }
}
