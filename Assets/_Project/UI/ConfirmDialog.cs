using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.UI
{
    /// <summary>
    /// 「いい？」と確かめる小さな画面（Step 10。Step 13・14 の確かめにも使う）。
    /// 後ろを半透明でおおい、文と「うん」「やめる」を出す。
    /// </summary>
    public class ConfirmDialog : MonoBehaviour
    {
        [SerializeField] Wording wording;
        [SerializeField] TMP_Text message;
        [SerializeField] Button yes;
        [SerializeField] Button no;

        Action onYes;

        public void Set(Wording table, TMP_Text text, Button yesButton, Button noButton)
        {
            wording = table;
            message = text;
            yes = yesButton;
            no = noButton;
        }

        void Awake()
        {
            yes.onClick.AddListener(() =>
            {
                var action = onYes;
                Close();
                action?.Invoke();
            });
            no.onClick.AddListener(Close);
        }

        /// <summary>Wording のキーの文で確かめる。「うん」なら onConfirm を呼ぶ。</summary>
        public void Show(string messageKey, Action onConfirm)
        {
            onYes = onConfirm;
            message.text = wording != null ? wording.Get(messageKey) : messageKey;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Close()
        {
            onYes = null;
            gameObject.SetActive(false);
        }
    }
}
