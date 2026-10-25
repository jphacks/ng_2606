using DeskStage.Merge;
using DeskStage.Shared;
using DeskStage.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskStage.App
{
    /// <summary>
    /// たしかめ。ほんものと 3D のステージが、同じかを見て、必要なら直し、コース（スタート・ゴール）を置いて、あそぶ。
    ///  ・なおす：箱を足す・動かす・高さを変える・けす・もどす（D の AssistController が指を読む。ボタンの見た目だけ、ここで作る）
    ///  ・コース：「スタートを おく」「ゴールを おく」を押して、机の上をタップ（C の CoursePlacer に依頼）
    ///  ・ほぞん：保存して、いちらんに出す
    ///  ・あそぶ：スタートとゴールが無いときは、理由を出して進まない
    ///  ・箱の上の「とぶ」「ばつ」の印は、D の StepMarksView
    /// </summary>
    public class CheckStageScreen : AppScreen
    {
        enum Mode
        {
            None,
            Assist,
            Course,
        }

        AppButton fixButton;
        AppButton courseButton;
        AppButton saveButton;
        AppButton playButton;
        TMP_Text needCourse;
        TMP_Text hint;
        RectTransform courseGroup;
        AppButton startButton;
        AppButton goalButton;
        RectTransform assistTools;
        TMP_Text assistHint;
        TapCatcher catcher;

        Mode mode;
        CourseItem? armed;

        protected override void Build()
        {
            // 3D が見えるよう、背景は無い。指を受ける板（AssistController）を、いちばん奥に置く
            var area = UiKit.Fill(UiKit.NewRect("AssistArea", Rect));
            area.gameObject.SetActive(false); // Set するまで、Awake を走らせない

            var markHolder = UiKit.Fill(UiKit.NewRect("Marks", Rect));
            markHolder.gameObject.SetActive(false);
            var marks = markHolder.gameObject.AddComponent<StepMarksView>();
            marks.Set(Context.Wording, Theme.Font, UiKit.CircleSprite);
            markHolder.gameObject.SetActive(true);

            var catcherImage = UiParts.Clear(Rect, "CourseCatcher", true);
            catcher = catcherImage.gameObject.AddComponent<TapCatcher>();
            catcher.Tapped = OnCourseTap;
            catcherImage.gameObject.SetActive(false);

            // 上の帯
            UiParts.Bar(Rect, "TopBar", true, 240f);
            var safe = UiKit.Fill(UiKit.NewRect("Safe", Rect));
            var back = UiKit.Button(safe, AppText.Back, UiKit.ButtonStyle.Overlay, new Vector2(330f, Theme.SmallButtonHeight),
                () => OnBack(), "Back", Theme.Body);
            UiKit.Place(back.Rect, new Vector2(0f, 1f), new Vector2(Theme.Margin, -40f), back.Rect.sizeDelta);
            var title = UiKit.FitLabel(safe, AppText.CheckTitle, Theme.H2, Color.white, TextAlignmentOptions.Center, true, "Title");
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(1200f, 100f));
            // 画面の上での直し（なおす）は、ふだんは使わない、ちいさな道具。主な直し方は、ほんものを ならべなおして「もういちど とる」
            fixButton = UiKit.Button(safe, AppText.CheckFix, UiKit.ButtonStyle.Overlay, new Vector2(420f, Theme.SmallButtonHeight),
                null, "Fix", Theme.Body);
            UiKit.Place(fixButton.Rect, new Vector2(1f, 1f), new Vector2(-Theme.Margin, -40f), fixButton.Rect.sizeDelta);

            var mainHint = UiKit.Label(safe, AppText.CheckMainHint, Theme.Small, Theme.WithAlpha(Color.white, 0.92f), TextAlignmentOptions.Center, true, "MainHint");
            mainHint.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.Place(mainHint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(1500f, 56f));

            hint = UiKit.Label(safe, "", Theme.Body, Color.white, TextAlignmentOptions.Center, true, "Hint");
            var hintBack = UiKit.Panel(safe, "HintBack", Theme.WithAlpha(Theme.Navy, 0.78f), 60f);
            UiKit.Place(hintBack.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(1500f, 100f));
            hint.transform.SetParent(hintBack.rectTransform, false);
            UiKit.Fill(hint.rectTransform);
            hintBack.gameObject.SetActive(false);
            hintBackRect = hintBack.gameObject;

            // 下の帯
            UiParts.Bar(Rect, "BottomBar", false, 300f);
            var barSafe = UiKit.Fill(UiKit.NewRect("BarSafe", Rect));
            var rescan = UiKit.Button(barSafe, AppText.CheckRescanButton, UiKit.ButtonStyle.Secondary, new Vector2(620f, Theme.ButtonHeight),
                AskRescan, "Rescan", Theme.H2);
            UiKit.Place(rescan.Rect, new Vector2(0f, 0f), new Vector2(Theme.Margin, 50f), rescan.Rect.sizeDelta);
            courseButton = UiKit.Button(barSafe, AppText.CheckCourse, UiKit.ButtonStyle.Soft, new Vector2(360f, Theme.ButtonHeight),
                ToggleCourse, "Course", Theme.H2);
            UiKit.Place(courseButton.Rect, new Vector2(0f, 0f), new Vector2(Theme.Margin + 660f, 50f), courseButton.Rect.sizeDelta);
            saveButton = UiKit.Button(barSafe, AppText.CheckSave, UiKit.ButtonStyle.Soft, new Vector2(360f, Theme.ButtonHeight),
                () => Context.Flow.SaveStage(), "Save", Theme.H2);
            UiKit.Place(saveButton.Rect, new Vector2(0f, 0f), new Vector2(Theme.Margin + 1060f, 50f), saveButton.Rect.sizeDelta);

            playButton = UiKit.Button(barSafe, AppText.CheckPlay, UiKit.ButtonStyle.Primary, new Vector2(560f, Theme.ButtonHeight),
                () => Context.Flow.PlayFromCheck(), "Play", Theme.H1);
            UiKit.Place(playButton.Rect, new Vector2(1f, 0f), new Vector2(-Theme.Margin, 50f), playButton.Rect.sizeDelta);
            needCourse = UiKit.Label(barSafe, AppText.CheckNeedCourse, Theme.Small, Theme.Orange, TextAlignmentOptions.Right, true, "NeedCourse");
            UiKit.Place(needCourse.rectTransform, new Vector2(1f, 0f), new Vector2(-Theme.Margin, 235f), new Vector2(1100f, 56f));

            // コースの操作（「コース」を押したときだけ）
            courseGroup = UiKit.NewRect("CourseGroup", barSafe);
            UiKit.Place(courseGroup, new Vector2(0.5f, 0f), new Vector2(0f, 330f), new Vector2(1100f, Theme.SmallButtonHeight));
            startButton = UiKit.Button(courseGroup, AppText.CheckSetStart, UiKit.ButtonStyle.Soft, new Vector2(520f, Theme.SmallButtonHeight),
                () => Arm(CourseItem.Start), "SetStart", Theme.Body);
            UiKit.Place(startButton.Rect, new Vector2(0f, 0.5f), Vector2.zero, startButton.Rect.sizeDelta);
            goalButton = UiKit.Button(courseGroup, AppText.CheckSetGoal, UiKit.ButtonStyle.Soft, new Vector2(520f, Theme.SmallButtonHeight),
                () => Arm(CourseItem.Goal), "SetGoal", Theme.Body);
            UiKit.Place(goalButton.Rect, new Vector2(1f, 0.5f), Vector2.zero, goalButton.Rect.sizeDelta);
            courseGroup.gameObject.SetActive(false);

            // なおす の操作（AssistController が、出す・隠すを行う）
            assistTools = UiKit.NewRect("AssistTools", barSafe);
            UiKit.Place(assistTools, new Vector2(0.5f, 0f), new Vector2(0f, 330f), new Vector2(1100f, Theme.SmallButtonHeight));
            var add = UiKit.Button(assistTools, AppText.CheckAddBox, UiKit.ButtonStyle.Soft, new Vector2(520f, Theme.SmallButtonHeight),
                null, "AddBox", Theme.Body);
            UiKit.Place(add.Rect, new Vector2(0f, 0.5f), Vector2.zero, add.Rect.sizeDelta);
            var undo = UiKit.Button(assistTools, AppText.CheckUndo, UiKit.ButtonStyle.Soft, new Vector2(520f, Theme.SmallButtonHeight),
                null, "Undo", Theme.Body);
            UiKit.Place(undo.Rect, new Vector2(1f, 0.5f), Vector2.zero, undo.Rect.sizeDelta);
            var addHintBack = UiKit.Panel(barSafe, "AddHintBack", Theme.WithAlpha(Theme.Orange, 0.95f), 60f);
            UiKit.Place(addHintBack.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(1500f, 100f));
            assistHint = UiKit.Label(addHintBack.rectTransform, AppText.CheckAddBoxHint, Theme.Body, Theme.Ink, TextAlignmentOptions.Center, true, "Hint");
            UiKit.Fill(assistHint.rectTransform);
            assistHint.gameObject.SetActive(false);
            addHintBack.gameObject.AddComponent<PillFollow>().Label = assistHint; // 文字が出ているときだけ、板を出す
            addHintBack.enabled = false;
            assistTools.gameObject.SetActive(false);

            // 選んだ箱のそばに出る「けす」と「上下の取っ手」（位置は AssistController が毎フレーム合わせる）
            var delete = UiKit.Button(area, AppText.CheckDeleteBox, UiKit.ButtonStyle.Danger, new Vector2(240f, Theme.SmallButtonHeight),
                null, "DeleteBox", Theme.Body);
            UiKit.Place(delete.Rect, new Vector2(0.5f, 0.5f), Vector2.zero, delete.Rect.sizeDelta);
            var handleDisc = UiKit.Disc(area, "HeightHandle", Theme.Orange, 190f);
            handleDisc.raycastTarget = true;
            UiKit.Place(handleDisc.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, handleDisc.rectTransform.sizeDelta);
            var handleLabel = UiKit.Label(handleDisc.rectTransform, AppText.CheckHeight, Theme.Body, Theme.Ink, TextAlignmentOptions.Center, true, "L");
            UiKit.Fill(handleLabel.rectTransform);
            var handleButton = handleDisc.gameObject.AddComponent<Button>();
            handleButton.targetGraphic = handleDisc;
            handleButton.transition = Selectable.Transition.None;

            var areaImage = area.gameObject.AddComponent<Image>();
            areaImage.color = Color.clear;
            areaImage.raycastTarget = true;
            area.SetAsFirstSibling();
            delete.Rect.SetAsLastSibling();
            handleDisc.rectTransform.SetAsLastSibling();
            delete.Rect.gameObject.SetActive(false); // 選んだ箱があるときだけ、AssistController が出す
            handleDisc.gameObject.SetActive(false);

            var controller = area.gameObject.AddComponent<AssistController>();
            controller.Set(Context.ViewLook, fixButton.Button, assistTools.gameObject, add.Button, undo.Button, delete.Button,
                handleDisc.rectTransform, assistHint);
            handleDisc.gameObject.AddComponent<AssistHandle>().SetController(controller);
            area.gameObject.SetActive(true);

            // 「なおす」の押し分け（AssistController と同じ押しで、文字と、コースの操作を切り替える）
            fixButton.OnClick(OnFixClicked);
        }

        GameObject hintBackRect;

        public override void OnEnter(object arg)
        {
            mode = Mode.None;
            armed = null;
            catcher.gameObject.SetActive(false);
            courseGroup.gameObject.SetActive(false);
            fixButton.SetText(AppText.CheckFix);
            hintBackRect.SetActive(false);
            GameEvents.CoursePlaced += OnCoursePlaced;
            StagePhotoTexturer.GimmicksChanged += OnGimmicksChanged;
            lastGimmickSummary = "";
            ShowGimmickSummary();
            Refresh();
        }

        string lastGimmickSummary = "";

        void OnGimmicksChanged() => ShowGimmickSummary();

        // 折り紙の色から、しかけを見つけたとき、ひとこと伝える
        void ShowGimmickSummary()
        {
            var summary = Context.Gimmicks.Summary();
            if (summary.Length > 0 && summary != lastGimmickSummary)
            {
                Context.Overlays.Toast(string.Format(AppText.GimmickFound, summary), AppOverlays.ToastKind.Good, 3.4f);
            }
            lastGimmickSummary = summary;
        }

        public override void OnExit()
        {
            GameEvents.CoursePlaced -= OnCoursePlaced;
            StagePhotoTexturer.GimmicksChanged -= OnGimmicksChanged;
            armed = null;
        }

        // 保存していないときは、確認してから出る
        public override void OnBack()
        {
            if (Context.Flow.HasNewStage && !Context.Flow.NewStageSaved)
            {
                Context.Overlays.Confirm(AppText.CheckLeaveConfirm, () => Context.Flow.LeaveToHome(), AppText.Back, AppText.No);
                return;
            }
            Context.Flow.LeaveToHome();
        }

        void AskRescan()
        {
            Context.Overlays.Confirm(AppText.CheckRescanConfirm, () => Context.Flow.BeginCapture(false), AppText.Yes, AppText.No);
        }

        void OnFixClicked()
        {
            if (mode == Mode.Assist)
            {
                mode = Mode.None;
            }
            else
            {
                mode = Mode.Assist;
                CloseCourse();
            }
            fixButton.SetText(mode == Mode.Assist ? AppText.CheckFixClose : AppText.CheckFix);
            fixButton.SetStyle(mode == Mode.Assist ? UiKit.ButtonStyle.Primary : UiKit.ButtonStyle.Overlay);
        }

        void ToggleCourse()
        {
            if (mode == Mode.Course)
            {
                CloseCourse();
                return;
            }
            if (mode == Mode.Assist)
            {
                fixButton.Button.onClick.Invoke(); // 「なおす」を閉じる（AssistController も同じ押しで閉じる）
            }
            mode = Mode.Course;
            courseGroup.gameObject.SetActive(true);
            courseButton.SetStyle(UiKit.ButtonStyle.Primary);
            courseButton.SetText(AppText.CheckCourseClose);
            ShowHint(AppText.CheckCourse == null ? "" : AppText.CheckModeCourseHint);
        }

        void CloseCourse()
        {
            if (mode == Mode.Course)
            {
                mode = Mode.None;
            }
            armed = null;
            catcher.gameObject.SetActive(false);
            courseGroup.gameObject.SetActive(false);
            courseButton.SetStyle(UiKit.ButtonStyle.Soft);
            courseButton.SetText(AppText.CheckCourse);
            ShowHint("");
        }

        void ShowHint(string text)
        {
            hintBackRect.SetActive(!string.IsNullOrEmpty(text));
            hint.text = text;
        }

        void Arm(CourseItem item)
        {
            armed = item;
            catcher.gameObject.SetActive(true);
            // 押せるボタンが、置く場所のタップより手前に出るよう、板をボタンの後ろへ
            catcher.transform.SetSiblingIndex(2);
            ShowHint(item == CourseItem.Start ? AppText.CheckPlaceStartHint : AppText.CheckPlaceGoalHint);
        }

        void OnCourseTap(Vector2 position)
        {
            if (armed == null)
            {
                return;
            }
            var item = armed.Value;
            armed = null;
            catcher.gameObject.SetActive(false);
            UiRequests.RequestPlaceCourseItem(item, position);
        }

        void OnCoursePlaced(CourseItem item, PlacementResult result, string reason)
        {
            var name = item == CourseItem.Start ? AppText.Start_ : AppText.Goal_;
            switch (result)
            {
                case PlacementResult.Rejected:
                    UiSound.Play(UiSound.Cue.Error);
                    Context.Overlays.Toast(AppText.CheckPlaceFailed, AppOverlays.ToastKind.Bad);
                    ShowHint(item == CourseItem.Start ? AppText.CheckPlaceStartHint : AppText.CheckPlaceGoalHint);
                    armed = item;
                    catcher.gameObject.SetActive(true);
                    return;
                case PlacementResult.MayBeUnreachable:
                    UiSound.Play(UiSound.Cue.Success);
                    Context.Overlays.Toast(string.Format(AppText.CheckPlaceUnreachable, name), AppOverlays.ToastKind.Info, 3.2f);
                    break;
                default:
                    UiSound.Play(UiSound.Cue.Success);
                    Context.Overlays.Toast(string.Format(AppText.CheckPlaced, name), AppOverlays.ToastKind.Good);
                    break;
            }
            ShowHint(AppText.CheckModeCourseHint);
        }

        void Update()
        {
            Refresh();
        }

        void Refresh()
        {
            var flow = Context.Flow;
            SetDone(startButton, flow.StartPlaced, AppText.CheckSetStart, AppText.CheckStartDone);
            SetDone(goalButton, flow.GoalPlaced, AppText.CheckSetGoal, AppText.CheckGoalDone);

            var saved = flow.NewStageSaved;
            saveButton.SetText(saved ? AppText.CheckSaved : AppText.CheckSave);
            saveButton.Interactable = !saved;

            var canPlay = flow.StartPlaced && flow.GoalPlaced;
            needCourse.gameObject.SetActive(!canPlay);
        }

        static void SetDone(AppButton button, bool done, string normal, string doneText)
        {
            button.SetText(done ? doneText : normal);
            button.Background.color = done ? Theme.Green : Color.white;
            button.Label.color = done ? Color.white : Theme.Ink;
        }
    }
}
