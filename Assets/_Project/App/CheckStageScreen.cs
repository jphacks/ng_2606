using System.Collections.Generic;
using DeskStage.Gameplay;
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
    ///  ・箱の上の段の印（D の StepMarksView）は出さない。「とぶ」は箱を隠し、「のぼれない」の判定は外れることが多いため（2026-11-09）
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
        AppButton rulesButton;
        GameObject rulesPanel;
        readonly List<TMP_Text> ruleValues = new List<TMP_Text>();
        RectTransform assistTools;
        TMP_Text assistHint;
        RectTransform flowLayer;
        GameObject missionPanel;
        TMP_Text missionTitle;
        readonly List<TMP_Text> missionLines = new List<TMP_Text>();
        readonly List<AppButton> flowButtons = new List<AppButton>();
        readonly List<Image> flowLines = new List<Image>();
        readonly List<Image> flowDots = new List<Image>();
        readonly List<TMP_Text> flowArrows = new List<TMP_Text>();
        readonly List<Rect> placedButtons = new List<Rect>();
        readonly List<string> flowIds = new List<string>();
        readonly List<string> warpIds = new List<string>();
        TapCatcher catcher;
        GameObject topBar;
        GameObject topSafe;
        GameObject bottomBar;
        GameObject bottomSafe;
        GameObject placeGroup;
        TMP_Text placeHint;

        Mode mode;
        CourseItem? armed;

        protected override void Build()
        {
            // 3D が見えるよう、背景は無い。指を受ける板（AssistController）を、いちばん奥に置く
            var area = UiKit.Fill(UiKit.NewRect("AssistArea", Rect));
            area.gameObject.SetActive(false); // Set するまで、Awake を走らせない

            // 水の箱の上に出る「ながれの むき」のボタン（ふだんの状態で。押すたびに 90 度まわる）。帯より奥に置く
            flowLayer = UiKit.Fill(UiKit.NewRect("FlowButtons", Rect));

            var catcherImage = UiParts.Clear(Rect, "CourseCatcher", true);
            catcher = catcherImage.gameObject.AddComponent<TapCatcher>();
            catcher.Tapped = OnCourseTap;
            catcherImage.gameObject.SetActive(false);

            // 上の帯
            topBar = UiParts.Bar(Rect, "TopBar", true, 240f).gameObject;
            var safe = UiKit.Fill(UiKit.NewRect("Safe", Rect));
            topSafe = safe.gameObject;
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
            // 上の説明（MainHint）の下に、すき間を空けて置く（2026-11-11：重なっていた）
            UiKit.Place(hintBack.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(1500f, 100f));
            hint.transform.SetParent(hintBack.rectTransform, false);
            UiKit.Fill(hint.rectTransform);
            hintBack.raycastTarget = false; // ひとことの板は、タップをじゃましない
            hint.raycastTarget = false;
            hintBack.gameObject.SetActive(false);
            hintBackRect = hintBack.gameObject;

            // 下の帯
            bottomBar = UiParts.Bar(Rect, "BottomBar", false, 300f).gameObject;
            var barSafe = UiKit.Fill(UiKit.NewRect("BarSafe", Rect));
            bottomSafe = barSafe.gameObject;
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
            UiKit.Place(needCourse.rectTransform, new Vector2(1f, 0f), new Vector2(-Theme.Margin - 10f, 262f), new Vector2(1100f, 56f)); // 「あそぶ」の上に、すき間を空けて

            // コースの操作（「コース」を押したときだけ）：スタートとゴールを並べ、しばりは少し離して右に置く（置く物と、条件を分ける）
            courseGroup = UiKit.NewRect("CourseGroup", barSafe);
            UiKit.Place(courseGroup, new Vector2(0.5f, 0f), new Vector2(0f, 330f), new Vector2(1600f, Theme.SmallButtonHeight));
            startButton = UiKit.Button(courseGroup, AppText.CheckSetStart, UiKit.ButtonStyle.Soft, new Vector2(480f, Theme.SmallButtonHeight),
                () => Arm(CourseItem.Start), "SetStart", Theme.Body);
            UiKit.Place(startButton.Rect, new Vector2(0f, 0.5f), Vector2.zero, startButton.Rect.sizeDelta);
            goalButton = UiKit.Button(courseGroup, AppText.CheckSetGoal, UiKit.ButtonStyle.Soft, new Vector2(480f, Theme.SmallButtonHeight),
                () => Arm(CourseItem.Goal), "SetGoal", Theme.Body);
            UiKit.Place(goalButton.Rect, new Vector2(0f, 0.5f), new Vector2(510f, 0f), goalButton.Rect.sizeDelta);
            rulesButton = UiKit.Button(courseGroup, AppText.Rules, UiKit.ButtonStyle.Secondary, new Vector2(420f, Theme.SmallButtonHeight),
                OpenRules, "Rules", Theme.Body);
            UiKit.Place(rulesButton.Rect, new Vector2(1f, 0.5f), Vector2.zero, rulesButton.Rect.sizeDelta);
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

            BuildRulesPanel();
            BuildMissionPanel();

            // 置く場所をタップしている間は、ほかの帯とボタンを隠して、画面ぜんぶをタップできるようにする。出るのは、ひとことと「やめる」だけ
            var group = UiKit.Fill(UiKit.NewRect("PlaceGroup", Rect));
            placeGroup = group.gameObject;
            var placeBack = UiKit.Panel(group, "PlaceHintBack", Theme.WithAlpha(Theme.Navy, 0.82f), 60f);
            // 画面の上は、置く場所として空けておく。ひとことは、下の「やめる」の上に出す
            UiKit.Place(placeBack.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 60f + Theme.SmallButtonHeight + 24f), new Vector2(1500f, 100f));
            placeBack.raycastTarget = false;
            placeHint = UiKit.Label(placeBack.rectTransform, "", Theme.Body, Color.white, TextAlignmentOptions.Center, true, "Hint");
            UiKit.Fill(placeHint.rectTransform);
            placeHint.raycastTarget = false; // 文字の上も、タップして置ける
            var cancel = UiKit.Button(group, AppText.No, UiKit.ButtonStyle.Secondary, new Vector2(420f, Theme.SmallButtonHeight),
                CancelPlacing, "CancelPlace", Theme.Body);
            UiKit.Place(cancel.Rect, new Vector2(0.5f, 0f), new Vector2(0f, 60f), cancel.Rect.sizeDelta);
            placeGroup.SetActive(false);
        }

        GameObject hintBackRect;

        public override void OnEnter(object arg)
        {
            mode = Mode.None;
            armed = null;
            SetPlacing(false, null);
            courseGroup.gameObject.SetActive(false);
            rulesPanel.SetActive(false);
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
                var text = string.Format(AppText.GimmickFound, summary);
                if (Context.Gimmicks.WarpCount == 1)
                {
                    text += "\n" + AppText.WarpNeedTwo; // ピンクが 1 つだけなら、ワープしない
                }
                Context.Overlays.Toast(text, AppOverlays.ToastKind.Good, 3.8f);
            }
            lastGimmickSummary = summary;
        }

        public override void OnExit()
        {
            GameEvents.CoursePlaced -= OnCoursePlaced;
            StagePhotoTexturer.GimmicksChanged -= OnGimmicksChanged;
            armed = null;
            SetPlacing(false, null);
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
            ShowRuleValues(); // 「しばり」のボタンの文字を、いまのしばりに合わせる
            courseButton.SetStyle(UiKit.ButtonStyle.Primary);
            courseButton.SetText(AppText.CheckCourseClose);
            ShowHint(AppText.CheckCourseChoose);
        }

        void CloseCourse()
        {
            if (mode == Mode.Course)
            {
                mode = Mode.None;
            }
            armed = null;
            SetPlacing(false, null);
            courseGroup.gameObject.SetActive(false);
            rulesPanel.SetActive(false);
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
            SetPlacing(true, item);
        }

        // 置く場所をタップしている間（on）は、帯とボタンを隠す。やめる・置き終わったら、もどす
        void SetPlacing(bool on, CourseItem? item)
        {
            topBar.SetActive(!on);
            topSafe.SetActive(!on);
            bottomBar.SetActive(!on);
            bottomSafe.SetActive(!on);
            catcher.gameObject.SetActive(on);
            placeGroup.SetActive(on);
            if (on)
            {
                Context.Overlays.HideToast(); // 「スタートを おいたよ」などが、上に残って じゃまをしないように
                placeHint.text = item == CourseItem.Start ? AppText.CheckPlaceStartHint : AppText.CheckPlaceGoalHint;
                // タップを受ける板が、ほかのどれよりも手前。ただし「やめる」だけは、その上
                catcher.transform.SetAsLastSibling();
                placeGroup.transform.SetAsLastSibling();
            }
        }

        void CancelPlacing()
        {
            armed = null;
            SetPlacing(false, null);
            ShowHint(AppText.CheckCourseChoose);
        }

        void OnCourseTap(Vector2 position)
        {
            if (armed == null)
            {
                return;
            }
            var item = armed.Value;
            armed = null;
            SetPlacing(false, null);
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
                    armed = item;
                    SetPlacing(true, item);
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
            ShowHint(AppText.CheckCourseChoose);
        }

        void Update()
        {
            Refresh();
            PlaceFlowButtons();
            RefreshMission();
        }

        StageLook stageLook;

        // 机の板を、遊ぶ画面と同じ、中身の詰まった土台にする（D の StageLook は、この場面では、ふちの線だけの素材にする）。
        // 2026-11-10：机の外の地面を消したので、ふちの線だけでは、土台が消えたように見えたため。StageLook が貼り直したあとに上書きする
        void LateUpdate()
        {
            if (stageLook == null) stageLook = FindFirstObjectByType<StageLook>();
            var solid = stageLook != null && stageLook.Look != null ? stageLook.Look.DeskPlay : null;
            var root = StageContext.StageRoot;
            var desk = root != null ? root.Find("Desk") : null;
            var renderer = desk != null ? desk.GetComponent<Renderer>() : null;
            if (solid != null && renderer != null && renderer.sharedMaterial != solid)
            {
                renderer.sharedMaterial = solid;
            }
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

        // ---- お題（Docs/specs/home-modes の REQ-6・7） ----

        // 左の、お題の板：お題の名前と、条件ごとの「できた」「まだ」（4 行まで）
        void BuildMissionPanel()
        {
            var panel = UiKit.Panel(Rect, "Mission", Theme.WithAlpha(Theme.Navy, 0.82f), 40f);
            panel.raycastTarget = false;
            UiKit.Place(panel.rectTransform, new Vector2(0f, 0.5f), new Vector2(Theme.Margin, 40f), new Vector2(720f, 330f));
            panel.rectTransform.pivot = new Vector2(0f, 0.5f);
            missionPanel = panel.gameObject;
            missionTitle = UiKit.FitLabel(panel.rectTransform, "", Theme.H2, Theme.Orange, TextAlignmentOptions.Left, true, "Title");
            UiKit.Area(missionTitle.rectTransform, new Vector2(0f, 0.7f), new Vector2(1f, 1f), 28f);
            missionLines.Clear();
            for (var i = 0; i < 2; i++)
            {
                var line = UiKit.FitLabel(panel.rectTransform, "", Theme.Body, Color.white, TextAlignmentOptions.Left, false, "Line" + i);
                UiKit.Area(line.rectTransform, new Vector2(0f, 0.38f - i * 0.32f), new Vector2(1f, 0.68f - i * 0.32f), 28f);
                missionLines.Add(line);
            }
            missionPanel.SetActive(false);
        }

        void RefreshMission()
        {
            var mission = Context.Flow.CurrentMission;
            var show = mission != null && mode == Mode.None && armed == null;
            missionPanel.SetActive(show);
            if (!show) return;
            missionTitle.text = string.Format(AppText.MissionCurrent, mission.Name);
            for (var i = 0; i < missionLines.Count; i++)
            {
                var has = i < mission.Conditions.Length;
                missionLines[i].gameObject.SetActive(has);
                if (!has) continue;
                var c = mission.Conditions[i];
                var met = MissionRules.IsMet(c, ScanContext.WorkingBoxes, StageContext.CurrentStage);
                missionLines[i].text = (met ? "◯ " : "× ") + c.Text + "　" + (met ? AppText.MissionOk : AppText.MissionNotYet);
                missionLines[i].color = met ? Theme.Hex("8EF0BE") : Color.white;
            }
        }

        // ---- しばり（Docs/specs/course-rules） ----

        static readonly int[] JumpChoices = { -1, 0, 1, 3, 5, 10 };
        static readonly int[] TimeChoices = { 0, 30, 60, 90, 120 };

        // しばりを選ぶ板：4 行（名前・値・左右のボタン）と「きめた」。後ろは押せない
        void BuildRulesPanel()
        {
            var dim = UiParts.Clear(Rect, "RulesPanel", true);
            dim.color = Theme.WithAlpha(Theme.Navy, 0.55f);
            rulesPanel = dim.gameObject;
            var card = UiKit.Panel(dim.rectTransform, "Card", Theme.Card, Theme.CardRadius, shadow: true);
            UiKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 1020f));
            var title = UiKit.Label(card.rectTransform, AppText.RulesTitle, Theme.H1, Theme.Ink, TextAlignmentOptions.Center, true, "Title");
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1200f, 110f));

            var names = new[] { AppText.RulesJump, AppText.RulesDouble, AppText.RulesOneHit, AppText.RulesTime };
            ruleValues.Clear();
            for (var i = 0; i < names.Length; i++)
            {
                var row = i;
                // 1 行：名前（左）・＜・値・＞（右）。高さをそろえ、文字は縦の真ん中に置く（重ならないよう、幅は決めた中で縮める）
                var y = -190f - i * 155f;
                var rowHeight = Theme.SmallButtonHeight;
                var name = UiKit.FitLabel(card.rectTransform, names[i], Theme.H2, Theme.Ink, TextAlignmentOptions.Left, true, "Name" + i);
                UiKit.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(80f, y), new Vector2(500f, rowHeight));
                var prev = UiKit.Button(card.rectTransform, "＜", UiKit.ButtonStyle.Secondary, new Vector2(140f, rowHeight),
                    () => StepRule(row, -1), "Prev" + i, Theme.H2);
                UiKit.Place(prev.Rect, new Vector2(0f, 1f), new Vector2(620f, y), prev.Rect.sizeDelta);
                var value = UiKit.FitLabel(card.rectTransform, "", Theme.H2, Theme.Orange, TextAlignmentOptions.Center, true, "Value" + i);
                UiKit.Place(value.rectTransform, new Vector2(0f, 1f), new Vector2(780f, y), new Vector2(480f, rowHeight));
                ruleValues.Add(value);
                var next = UiKit.Button(card.rectTransform, "＞", UiKit.ButtonStyle.Secondary, new Vector2(140f, rowHeight),
                    () => StepRule(row, +1), "Next" + i, Theme.H2);
                UiKit.Place(next.Rect, new Vector2(0f, 1f), new Vector2(1280f, y), next.Rect.sizeDelta);
            }

            var done = UiKit.Button(card.rectTransform, AppText.RulesDone, UiKit.ButtonStyle.Primary, new Vector2(560f, Theme.ButtonHeight),
                () => rulesPanel.SetActive(false), "Done", Theme.H1);
            UiKit.Place(done.Rect, new Vector2(0.5f, 0f), new Vector2(0f, 40f), done.Rect.sizeDelta);
            rulesPanel.SetActive(false);
        }

        static CourseRules CurrentRules()
        {
            var stage = StageContext.CurrentStage;
            if (stage == null) return null;
            if (stage.rules == null) stage.rules = new CourseRules();
            return stage.rules;
        }

        void OpenRules()
        {
            if (CurrentRules() == null) return;
            ShowHint("");
            rulesPanel.transform.SetAsLastSibling();
            rulesPanel.SetActive(true);
            ShowRuleValues();
        }

        // 行の値を、左右へ 1 つ動かす（端まで行ったら、反対の端へ）
        void StepRule(int row, int delta)
        {
            var r = CurrentRules();
            if (r == null) return;
            switch (row)
            {
                case 0: r.maxJumps = Step(JumpChoices, r.maxJumps, delta); break;
                case 1: r.noDoubleJump = !r.noDoubleJump; break;
                case 2: r.oneHit = !r.oneHit; break;
                default: r.timeLimitSeconds = Step(TimeChoices, Mathf.RoundToInt(r.timeLimitSeconds), delta); break;
            }
            UiSound.Play(UiSound.Cue.Click);
            ShowRuleValues();
        }

        static int Step(int[] choices, int current, int delta)
        {
            var i = System.Array.IndexOf(choices, current);
            if (i < 0) i = 0;
            return choices[(i + delta + choices.Length) % choices.Length];
        }

        void ShowRuleValues()
        {
            var r = CurrentRules() ?? new CourseRules();
            ruleValues[0].text = r.maxJumps < 0 ? AppText.RulesJumpFree : r.maxJumps == 0 ? AppText.RulesJumpNone : string.Format(AppText.RulesJumpCount, r.maxJumps);
            ruleValues[1].text = r.noDoubleJump ? AppText.RulesDoubleOff : AppText.RulesDoubleOn;
            ruleValues[2].text = r.oneHit ? AppText.RulesOn : AppText.RulesOff;
            ruleValues[3].text = r.timeLimitSeconds > 0f ? string.Format(AppText.RulesSeconds, Mathf.RoundToInt(r.timeLimitSeconds)) : AppText.RulesOff;
            rulesButton.SetText(CourseRuleState.HasAny(r) ? AppText.RulesSet : AppText.Rules);
        }

        const float SideButtonWidth = 300f;
        const float SideGap = 40f;

        // 水の箱とワープの箱それぞれの横に、ボタンを出す（ふだんの状態だけ。なおす・コースの間と、置く場所を選ぶ間は隠す）
        //  ・水：「ながれの むき」（押すたびに 90 度まわる）
        //  ・ワープ：「1 ⇔ 2」（押すたびに、つながる相手が次のピンクに変わる。Docs/specs/warp-pairs）
        // 箱の上に重ねるとステージが見えないので、画面で箱が占める範囲の外（画面の端に近い側）に置き、
        // 箱の上面の真ん中と細い線でつなぐ。ボタン同士が重なるときは、下へずらす
        void PlaceFlowButtons()
        {
            var show = mode == Mode.None && armed == null;
            flowIds.Clear();
            warpIds.Clear();
            if (show)
            {
                Context.Gimmicks.WaterBoxIds(flowIds);
                Context.Gimmicks.WarpBoxIds(warpIds);
                if (warpIds.Count < 2) warpIds.Clear(); // 1 つだけなら、つなぐ相手がいない
            }
            var cam = Camera.main;
            var root = StageContext.StageRoot;
            var used = 0;
            placedButtons.Clear();
            for (var k = 0; k < flowIds.Count + warpIds.Count; k++)
            {
                var isWarp = k >= flowIds.Count;
                var id = isWarp ? warpIds[k - flowIds.Count] : flowIds[k];
                var box = root != null ? root.Find(id) : null;
                if (box == null || cam == null)
                {
                    continue;
                }
                // 坂は、斜めの面の真ん中（箱の座標で y = 0）、箱は上面の真ん中
                var ramp = box.GetComponent<MeshCollider>() != null;
                if (!ToLocal(cam, box.TransformPoint(new Vector3(0f, ramp ? 0f : 0.5f, 0f)), out var anchor)
                    || !BoxSpan(cam, box, out var minX, out var maxX))
                {
                    continue;
                }
                if (used == flowButtons.Count)
                {
                    AddSideButton();
                }
                var button = flowButtons[used];
                var line = flowLines[used];
                var dot = flowDots[used];
                used++;
                var target = id;
                button.SetText(isWarp ? Context.Gimmicks.WarpLabel(id) : AppText.CheckFlow);
                button.Button.onClick.RemoveAllListeners();
                if (isWarp) button.Button.onClick.AddListener(() => Context.Gimmicks.CycleWarp(target));
                else button.Button.onClick.AddListener(() => Context.Gimmicks.CycleFlow(target));

                // 画面の左半分の箱は左に、右半分の箱は右に出す（画面からはみ出すなら、反対側へ）
                var half = flowLayer.rect.size * 0.5f;
                var size = button.Rect.sizeDelta;
                var left = anchor.x < 0f;
                var x = left ? minX - SideGap - size.x * 0.5f : maxX + SideGap + size.x * 0.5f;
                if (Mathf.Abs(x) + size.x * 0.5f > half.x)
                {
                    left = !left;
                    x = left ? minX - SideGap - size.x * 0.5f : maxX + SideGap + size.x * 0.5f;
                }
                x = Mathf.Clamp(x, -half.x + size.x * 0.5f, half.x - size.x * 0.5f);
                var spot = new Rect(x - size.x * 0.5f, anchor.y - size.y * 0.5f, size.x, size.y);
                for (var tries = 0; tries < 8 && placedButtons.Exists(r => r.Overlaps(spot)); tries++)
                {
                    spot.y -= size.y + 16f;
                }
                placedButtons.Add(spot);
                button.Rect.anchorMin = button.Rect.anchorMax = new Vector2(0.5f, 0.5f);
                button.Rect.anchoredPosition = spot.center;
                button.gameObject.SetActive(true);

                // 箱の上面の真ん中から、ボタンの箱に近い端まで線を引く
                var end = new Vector2(left ? spot.xMax : spot.xMin, spot.center.y);
                var along = end - anchor;
                line.rectTransform.anchoredPosition = (anchor + end) * 0.5f;
                line.rectTransform.sizeDelta = new Vector2(along.magnitude, 6f);
                line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg);
                line.gameObject.SetActive(true);
                dot.rectTransform.anchoredPosition = anchor;
                dot.gameObject.SetActive(true);

                // 水：上面の真ん中に、青い丸と、いまの流れの向きを画面の上で指す白い矢印（2026-11-15：ボタンの文字だけでは、
                // いまどちら向きか分からず、押すたびに何が変わったかも見えにくかった）。ワープは、小さな白い点のまま
                var arrow = flowArrows[used - 1];
                var size = isWarp ? FlowDotSize : FlowArrowDotSize;
                dot.rectTransform.sizeDelta = new Vector2(size, size);
                dot.color = isWarp ? Color.white : Theme.Blue;
                arrow.gameObject.SetActive(!isWarp && FlowAngle(cam, box, id, anchor, out var angle) && SetArrow(arrow, angle));
            }
            for (var i = used; i < flowButtons.Count; i++)
            {
                flowButtons[i].gameObject.SetActive(false);
                flowLines[i].gameObject.SetActive(false);
                flowDots[i].gameObject.SetActive(false);
            }
        }

        void AddSideButton()
        {
            // 線と点は、ボタンより奥に描く
            var line = UiKit.Panel(flowLayer, "FlowLine", Color.white, 3f);
            line.raycastTarget = false;
            line.rectTransform.anchorMin = line.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            line.rectTransform.SetAsFirstSibling();
            flowLines.Add(line);
            var dot = UiKit.Disc(flowLayer, "FlowDot", Color.white, FlowDotSize);
            dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            flowDots.Add(dot);
            var arrow = UiKit.Label(dot.rectTransform, "▲", FlowArrowDotSize * 0.5f, Color.white, TextAlignmentOptions.Center, true, "FlowArrow");
            UiKit.Fill(arrow.rectTransform);
            arrow.raycastTarget = false;
            flowArrows.Add(arrow);
            flowButtons.Add(UiKit.Button(flowLayer, AppText.CheckFlow, UiKit.ButtonStyle.Soft, new Vector2(SideButtonWidth, Theme.SmallButtonHeight),
                null, "Flow", Theme.Body));
        }

        const float FlowDotSize = 26f;
        const float FlowArrowDotSize = 76f;

        // 水の流れの向きを、画面の上の角度（度。右が 0、反時計回り）にする。流れは、箱の座標の (x, z) の向き（GimmickRules.WaterDirection）。
        // 箱の大きさで向きがゆがまないよう、回転だけで世界の向きにする
        bool FlowAngle(Camera cam, Transform box, string id, Vector2 anchor, out float angle)
        {
            angle = 0f;
            if (!Context.Gimmicks.IsWater(id, out var flow))
            {
                return false;
            }
            var d = GimmickRules.WaterDirection(id, flow);
            var world = box.rotation * new Vector3(d.x, 0f, d.y);
            var center = box.TransformPoint(new Vector3(0f, box.GetComponent<MeshCollider>() != null ? 0f : 0.5f, 0f));
            if (!ToLocal(cam, center + world.normalized * 0.03f, out var tip) || (tip - anchor).sqrMagnitude < 1f)
            {
                return false;
            }
            angle = Mathf.Atan2(tip.y - anchor.y, tip.x - anchor.x) * Mathf.Rad2Deg;
            return true;
        }

        // 「▲」は上（90 度）を指すので、その分だけ戻して回す
        static bool SetArrow(TMP_Text arrow, float angle)
        {
            arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);
            return true;
        }

        bool ToLocal(Camera cam, Vector3 world, out Vector2 local)
        {
            local = Vector2.zero;
            var screen = cam.WorldToScreenPoint(world);
            return screen.z > 0f && RectTransformUtility.ScreenPointToLocalPointInRectangle(flowLayer, screen, null, out local);
        }

        // 箱の 8 つの角を画面に写し、左右の端（flowLayer の座標）を求める
        bool BoxSpan(Camera cam, Transform box, out float minX, out float maxX)
        {
            minX = float.MaxValue;
            maxX = float.MinValue;
            for (var c = 0; c < 8; c++)
            {
                var corner = new Vector3((c & 1) == 0 ? -0.5f : 0.5f, (c & 2) == 0 ? -0.5f : 0.5f, (c & 4) == 0 ? -0.5f : 0.5f);
                if (!ToLocal(cam, box.TransformPoint(corner), out var p))
                {
                    return false;
                }
                minX = Mathf.Min(minX, p.x);
                maxX = Mathf.Max(maxX, p.x);
            }
            return true;
        }

        static void SetDone(AppButton button, bool done, string normal, string doneText)
        {
            button.SetText(done ? doneText : normal);
            button.Background.color = done ? Theme.Green : Color.white;
            button.Label.color = done ? Color.white : Theme.Ink;
        }
    }
}
