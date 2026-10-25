using System;
using System.Collections;
using System.Collections.Generic;
using DeskStage.Shared;
using UnityEngine;

namespace DeskStage.App
{
    /// <summary>
    /// 画面の切り替え役。一度に 1 つの画面だけを見せ、見せる画面に合わせて StageContext.Mode を書く
    /// （Mode を書くのは、ここだけ。約束どおり）。
    /// 戻る道のために、通った画面を覚える。Home に戻ったら、覚えを空にする。
    /// 画面は、ふわっと出る（すぐに出したいときは animate を false に）。
    /// </summary>
    public class AppRouter : MonoBehaviour
    {
        readonly Dictionary<AppScreenId, AppScreen> screens = new Dictionary<AppScreenId, AppScreen>();
        readonly Stack<AppScreenId> history = new Stack<AppScreenId>();
        Coroutine fade;

        public AppScreenId Current { get; private set; } = AppScreenId.Title;

        public bool HasCurrent { get; private set; }

        /// <summary>画面を切り替えた。</summary>
        public event Action<AppScreenId> ScreenChanged;

        public void Register(AppScreenId id, AppScreen screen)
        {
            screens[id] = screen;
            screen.gameObject.SetActive(false);
        }

        public AppScreen Get(AppScreenId id) => screens[id];

        /// <summary>画面を出す。remember が true なら、いまの画面を「戻る」で戻れるよう覚える。</summary>
        public void Show(AppScreenId id, object arg = null, bool remember = true, bool animate = true)
        {
            if (HasCurrent && remember && Current != id)
            {
                history.Push(Current);
            }
            if (id == AppScreenId.Home || id == AppScreenId.Title)
            {
                history.Clear();
            }
            Switch(id, arg, animate);
        }

        /// <summary>1 つ前の画面へ戻る。覚えが無ければ Home へ。</summary>
        public void Back(object arg = null)
        {
            var target = history.Count > 0 ? history.Pop() : AppScreenId.Home;
            Switch(target, arg, true);
        }

        void Switch(AppScreenId id, object arg, bool animate)
        {
            if (HasCurrent)
            {
                var old = screens[Current];
                old.OnExit();
                old.gameObject.SetActive(false);
            }
            Current = id;
            HasCurrent = true;
            StageContext.Mode = FlowRules.ModeFor(id);

            var next = screens[id];
            next.gameObject.SetActive(true);
            next.OnEnter(arg);
            if (fade != null)
            {
                StopCoroutine(fade);
            }
            if (animate && isActiveAndEnabled)
            {
                fade = StartCoroutine(FadeIn(next));
            }
            else
            {
                next.Group.alpha = 1f;
                next.transform.localScale = Vector3.one;
            }
            ScreenChanged?.Invoke(id);
        }

        static IEnumerator FadeIn(AppScreen screen)
        {
            var group = screen.Group;
            var t = 0f;
            group.alpha = 0f;
            screen.transform.localScale = Vector3.one * 0.98f;
            while (t < Theme.FadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / Theme.FadeSeconds);
                var eased = 1f - (1f - k) * (1f - k);
                group.alpha = eased;
                screen.transform.localScale = Vector3.one * Mathf.Lerp(0.98f, 1f, eased);
                yield return null;
            }
            group.alpha = 1f;
            screen.transform.localScale = Vector3.one;
        }

        void Update()
        {
            // 端末の戻る操作（Android の戻るキーなど）。いまの画面に任せる
            if (HasCurrent && UnityEngine.InputSystem.Keyboard.current != null
                && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                screens[Current].OnBack();
            }
        }
    }
}
