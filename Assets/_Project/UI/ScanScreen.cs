using DeskStage.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeskStage.UI
{
    /// <summary>
    /// スキャン画面（Step 8）。画面いっぱいの透明な板に付ける（ボタンより奥に置く）。
    /// ・机を探しているときに画面をタップしたら SetDesk を出す
    /// ・画面の上に「みつけた はこ：3」と箱の数を大きく出す
    /// ・「できた」で FinishScan を出す（画面の切り替えはボタンに付けた ScreenRouter が行う）
    /// 3Dの演出は ScanEffects が行う。
    /// </summary>
    public class ScanScreen : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] Wording wording;
        [SerializeField] TMP_Text boxCount;
        [SerializeField] Button done;
        [SerializeField] ScanEffects effects;

        int shownCount = -1;

        public void Set(Wording table, TMP_Text count, Button doneButton, ScanEffects fx)
        {
            wording = table;
            boxCount = count;
            done = doneButton;
            effects = fx;
        }

        void Awake()
        {
            if (done != null)
            {
                done.onClick.AddListener(UiRequests.RequestFinishScan);
            }
        }

        void OnEnable()
        {
            shownCount = -1;
            if (effects != null)
            {
                effects.enabled = true;
            }
            Refresh();
        }

        void OnDisable()
        {
            // スキャン画面を出たら、上面の光などの演出を片付ける
            if (effects != null)
            {
                effects.enabled = false;
            }
        }

        void Update() => Refresh();

        void Refresh()
        {
            var count = ScanContext.WorkingBoxes.Count;
            if (count == shownCount || boxCount == null || wording == null)
            {
                return;
            }
            shownCount = count;
            boxCount.text = string.Format(wording.Get("scan.boxCount"), count);
        }

        public void OnPointerClick(PointerEventData e)
        {
            // 机が決まった後のタップは A が無視する決まりだが、こちらでも机を探しているときだけ出す
            if (StageContext.ScanStatus == ScanStatus.FindingDesk)
            {
                UiRequests.RequestSetDesk(e.position);
            }
        }
    }
}
