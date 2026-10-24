using DeskStage.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeskStage.UI
{
    /// <summary>
    /// ならべかたのコツの画面（Step 9）。絵を横にめくって見せ、「スキャンする」で StartScan を出す。
    /// 2回目からは絵をとばして「スキャンする」を大きく出す（「コツを みる」で絵に戻れる）。
    /// 画面いっぱいの透明な板に付け、なぞる操作を受ける。絵の組（pageGroup）はその子に置く。
    /// </summary>
    public class TipsScreen : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public const string SeenKey = "DeskStage.TipsSeen";
        const float SlideSeconds = 0.2f;

        [SerializeField] Wording wording;
        [SerializeField] TipsPages pages;
        [SerializeField] Image pageImage;
        [SerializeField] TMP_Text pageText;
        [SerializeField] TMP_Text dots;
        [SerializeField] Button scan;
        [SerializeField] Button showTips;
        [SerializeField] GameObject pageGroup;

        TipsPager pager;
        System.Collections.Generic.List<TipsPages.Page> list;
        RectTransform scanRect;
        Vector2 scanAnchor;
        Vector2 scanPosition;
        Vector2 scanSize;
        float slide;
        float slideFrom;

        public void Set(Wording table, TipsPages tipPages, Image image, TMP_Text text, TMP_Text pageDots,
            Button scanButton, Button showButton, GameObject group)
        {
            wording = table;
            pages = tipPages;
            pageImage = image;
            pageText = text;
            dots = pageDots;
            scan = scanButton;
            showTips = showButton;
            pageGroup = group;
        }

        void Awake()
        {
            scanRect = (RectTransform)scan.transform;
            scanAnchor = scanRect.anchorMin;
            scanPosition = scanRect.anchoredPosition;
            scanSize = scanRect.sizeDelta;
            scan.onClick.AddListener(OnScan);
            if (showTips != null)
            {
                showTips.onClick.AddListener(() => ShowPages(true));
            }
        }

        void OnEnable()
        {
            list = pages != null ? pages.EnabledPages() : new System.Collections.Generic.List<TipsPages.Page>();
            pager = new TipsPager(list.Count);
            ShowPages(!Seen);
        }

        static bool Seen
        {
            get
            {
                try
                {
                    return PlayerPrefs.GetInt(SeenKey, 0) == 1;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>「まだ見ていない」に戻し、表示中なら絵の見え方に戻す（開発用）。</summary>
        public void ResetSeen()
        {
            PlayerPrefs.DeleteKey(SeenKey);
            if (isActiveAndEnabled)
            {
                ShowPages(true);
            }
        }

        void OnScan()
        {
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
            UiRequests.RequestStartScan();
        }

        // 絵を見せるか、絵をとばして「スキャンする」を大きく出すか
        void ShowPages(bool show)
        {
            pageGroup.SetActive(show && list.Count > 0);
            if (showTips != null)
            {
                showTips.gameObject.SetActive(!show);
            }
            if (show)
            {
                scanRect.anchorMin = scanRect.anchorMax = scanAnchor;
                scanRect.anchoredPosition = scanPosition;
                scanRect.sizeDelta = scanSize;
                pager.Go(0);
                Refresh();
            }
            else
            {
                scanRect.anchorMin = scanRect.anchorMax = new Vector2(0.5f, 0.5f);
                scanRect.anchoredPosition = new Vector2(0f, -60f);
                scanRect.sizeDelta = scanSize * 1.5f;
            }
        }

        void Refresh()
        {
            if (list.Count == 0)
            {
                return;
            }
            var page = list[pager.Index];
            pageImage.sprite = page.Image;
            pageImage.enabled = page.Image != null;
            pageText.text = wording != null ? wording.Get(page.Key) : page.Key;
            dots.text = pager.Dots();
        }

        public void OnBeginDrag(PointerEventData e)
        {
        }

        public void OnDrag(PointerEventData e)
        {
            if (!pageGroup.activeSelf)
            {
                return;
            }
            // 指についてくるように、絵を横にずらす
            var rect = (RectTransform)pageImage.transform;
            rect.anchoredPosition += new Vector2(e.delta.x / transform.lossyScale.x, 0f);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!pageGroup.activeSelf)
            {
                return;
            }
            var dx = e.position.x - e.pressPosition.x;
            var rect = (RectTransform)pageImage.transform;
            if (pager.EndSwipe(dx, Screen.width))
            {
                Refresh();
                // めくった向きの反対側から、新しい絵がすべりこむ
                rect.anchoredPosition = new Vector2(dx < 0f ? rect.rect.width * 0.5f : -rect.rect.width * 0.5f, rect.anchoredPosition.y);
            }
            slideFrom = rect.anchoredPosition.x;
            slide = SlideSeconds;
        }

        void Update()
        {
            if (slide <= 0f)
            {
                return;
            }
            slide = Mathf.Max(0f, slide - Time.unscaledDeltaTime);
            var rect = (RectTransform)pageImage.transform;
            var t = 1f - slide / SlideSeconds;
            rect.anchoredPosition = new Vector2(Mathf.Lerp(slideFrom, 0f, 1f - (1f - t) * (1f - t)), rect.anchoredPosition.y);
        }
    }
}
