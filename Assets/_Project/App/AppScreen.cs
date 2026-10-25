using UnityEngine;

namespace DeskStage.App
{
    /// <summary>
    /// 画面の土台。画面は、自分の部品を Build で作り、見えるときに OnEnter、見えなくなるときに OnExit を受ける。
    /// 画面の切り替えは AppRouter が行う（ふわっと出る動きも、そこ）。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class AppScreen : MonoBehaviour
    {
        public AppContext Context { get; private set; }

        public CanvasGroup Group { get; private set; }

        public RectTransform Rect => (RectTransform)transform;

        /// <summary>画面を作る（1 度だけ）。部品は、この画面の子として作る。</summary>
        public void Init(AppContext context)
        {
            Context = context;
            Group = GetComponent<CanvasGroup>();
            Build();
        }

        protected abstract void Build();

        /// <summary>画面が見えるようになった。arg は、前の画面からの受け渡し。</summary>
        public virtual void OnEnter(object arg)
        {
        }

        /// <summary>画面が見えなくなる。</summary>
        public virtual void OnExit()
        {
        }

        /// <summary>この画面で「戻る」を押したとき（端末の戻る操作や、画面の「もどる」）。既定は、1 つ前の画面へ。</summary>
        public virtual void OnBack() => Context.Router.Back();
    }
}
