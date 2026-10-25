namespace DeskStage.App
{
    /// <summary>
    /// 新しい UI の文言。ぜんぶ、ここに置く（子ども向け：ひらがな、短く、「〜してね」）。
    /// 画面のコードに、文を直接書かない。
    /// </summary>
    public static class AppText
    {
        // ---- 共通 ----
        public const string Back = "もどる";
        public const string Yes = "うん";
        public const string No = "やめる";
        public const string Close = "とじる";
        public const string Retry = "もういちど";
        public const string Ok = "OK";

        // ---- タイトル ----
                public const string TitleBig = "ならべて！ステージ";
        public const string TitlePlain = "ならべて！ステージ";
        public const string TitleRich = "ならべて！<color=#E8453C>ステージ</color>";
        public const string TitleTagline = "家にあるものが、そのままステージに";
        public const string Start = "はじめる";

        // ---- ホーム（ステージいちらん）----
        public const string HomeTitle = "ステージ いちらん";
        public const string HomeNew = "あたらしく つくる";
        public const string HomeNewHint = "つくえの うえを\nしゃしんに とるよ";
        public const string HomeEmpty = "まだ ステージが ないよ。\nあたらしく つくってみよう！";
        public const string HomePlay = "あそぶ";
        public const string HomeDelete = "けす";
        public const string HomeDeleteConfirm = "ほんとうに けす？\nもとには もどせないよ";
        public const string HomeDeleted = "けしたよ";
        public const string HomeLoading = "ステージを よんでいるよ";
        public const string HomeLoadFailed = "ステージを よみこめなかったよ";
        public const string HomeCharacters = "キャラ";
        public const string HomeBackdrop = "はいけい";
        public const string HomeSample = "おてほん";
        public const string HomeUnreadable = "よめない";
        public const string HomeSelectHint = "ステージを えらんでね";

        // ---- ならべかた ----
        public const string HowToTitle = "つくえに ならべよう";
        public const string HowToStep1 = "うえが たいらな\nものを ならべよう";
        public const string HowToStep2 = "たかい ところには、\nひくい ものを となりに";
        public const string HowToStep3 = "しゃしんを 2まい\nとるよ";
        public const string HowToPrivacy = "しゃしんは、ステージを つくる ために\nインターネットの サービスに おくるよ。おうちの ひとと いっしょに みてね";
        public const string HowToGo = "とりはじめる";
        public const string HowToNext = "つぎへ";

        // ---- さつえい ----
        public const string CaptureStep1 = "まうえから";
        public const string CaptureStep2 = "ななめから";
        public const string CaptureHint1 = "つくえの まうえから、\nものを ぜんぶ うつして\nとってね";
        public const string CaptureHint2 = "つぎは てまえから ななめに、\nたかさが わかるように とってね";
        public const string CapturePreparing = "カメラを じゅんびしているよ";
        public const string CaptureDark = "くらいよ。へやを あかるくして、もういちど とってね";
        public const string CaptureBlur = "ぶれたよ。じっと して、もういちど とってね";
        public const string CaptureShoot = "とる";

        // ---- つくっている ----
        public const string Generating = "ステージを つくっているよ";
        public const string GeneratingSeconds = "{0} びょう";
        public const string GeneratingLong = "もうすこし まってね。\nちょっと じかんが かかっているよ";
        public const string GeneratingCancel = "やめる";
        public const string GeneratingKeep = "まつ";
        public const string GeneratingPhotoCaption = "さっき とった しゃしん";
        public static readonly string[] GeneratingTips =
        {
            "しゃしんから はこを みつけているよ",
            "はこの たかさを はかっているよ",
            "つくえの おおきさを しらべているよ",
            "ほんものの しゃしんを はっているよ",
        };

        // ---- しっぱい ----
        public const string ErrorTitle = "うまく いかなかったよ";
        public const string ErrorCameraDenied = "カメラが つかえないよ。\nせっていで カメラを ゆるして ね";
        public const string ErrorCameraOpenSettings = "せっていを ひらく";
        public const string ErrorNetwork = "インターネットに つながらないよ。\nつながってから、もういちど やってみてね";
        public const string ErrorServer = "ステージを つくれなかったよ。\nすこし まってから、もういちど やってみてね";
        public const string ErrorTimeout = "じかんが かかりすぎたよ。\nもういちど やってみてね";
        public const string ErrorNoBoxes = "はこが みつからなかったよ。\nうえが たいらな ものを ならべて、\nあかるい ところで とってね";
        public const string ErrorLoad = "ステージを よみこめなかったよ";
        public const string ErrorDetail = "くわしく：{0}";

        // ---- たしかめ ----
        public const string CheckTitle = "ほんものと おなじかな？";
        public const string CheckFix = "なおす";
        public const string CheckFixClose = "なおしおわる";
        public const string CheckAddBox = "＋ はこ";
        public const string CheckAddBoxHint = "はこを おきたい ところを タップしてね";
        public const string CheckUndo = "もどす";
        public const string CheckCourse = "コース";
        public const string CheckCourseClose = "コースを とじる";
        public const string CheckSetStart = "スタートを おく";
        public const string CheckSetGoal = "ゴールを おく";
        public const string CheckStartDone = "スタート おいた";
        public const string CheckGoalDone = "ゴール おいた";
        public const string CheckPlaceStartHint = "つくえの うえを タップして、スタートを おいてね";
        public const string CheckPlaceGoalHint = "つくえの うえを タップして、ゴールを おいてね";
        public const string CheckPlaced = "{0}を おいたよ";
        public const string CheckPlaceFailed = "そこには おけないよ。べつの ところを タップしてね";
        public const string CheckPlaceUnreachable = "{0}を おいたよ（キャラが とどかないかも）";
        public const string CheckSave = "ほぞん";
        public const string CheckSaved = "ほぞん ずみ";
        public const string CheckSaveOk = "ほぞん したよ";
        public const string CheckSaveFailed = "ほぞん できなかったよ";
        public const string CheckPlay = "あそぶ";
        public const string CheckNeedCourse = "スタートと ゴールを おくと あそべるよ";
        public const string CheckRescan = "もういちど とる";
        public const string CheckRescanConfirm = "いまの ステージは なくなるよ。\nもういちど とる？";
        public const string CheckLeaveConfirm = "ほぞん していないよ。\nもどると なくなるよ。もどる？";
        public const string CheckMarkJump = "とぶ";
        public const string CheckMarkWall = "ばつ";
        public const string CheckWallHint = "赤い ばつは のぼれないよ。ひくい ものを となりに おくと のぼれるよ";
        public const string Start_ = "スタート";
        public const string Goal_ = "ゴール";

        // ---- あそぶ ----
        public const string PlayJump = "ジャンプ";
        public const string PlayPause = "いちじていし";
        public const string PlayResume = "つづける";
        public const string PlayRetry = "もういちど";
        public const string PlayQuit = "やめる";
        public const string PlayCheckpoint = "チェックポイント！";
        public const string PlayFell = "おちちゃった！ もういちど！";
        public const string PlayHot = "スマホが あつく なったよ。すこし やすもう";
        public const string PlayTime = "タイム";

        // ---- クリア ----
        public const string ClearTitle = "ゴール！";
        public const string ClearNewRecord = "しんきろく！";
        public const string ClearTime = "タイム　{0}";
        public const string ClearRetry = "もういちど あそぶ";
        public const string ClearList = "いちらんへ";
        public const string ClearSave = "ほぞんして いちらんへ";
        public const string ClearFix = "コースを なおす";

        // ---- キャラ ----
        public const string CharTitle = "キャラを えらぶ";
        public const string CharDefault = "いつもの こ";
        public const string CharNew = "あたらしく とる";
        public const string CharDelete = "えらんだ こを けす";
        public const string CharCaptureHint = "にんぎょうを わくに あわせてね\nあたまから あしまで\nぜんぶ いれてね";
        public const string CharTake = "とる";
        public const string CharReviewAsk = "この こに する？";
        public const string CharReviewOk = "これに する";
        public const string CharRetake = "とりなおす";
        public const string CharChosen = "えらんだよ";
        public const string CharDeleted = "けしたよ";
        public const string CharCannotDeleteDefault = "いつもの こは けせないよ";
        public const string CharCutFailed = "うまく きりぬけなかったよ。";

        // ---- はいけい ----
        public const string BackdropTitle = "ステージの はいけい";
        public const string BackdropNone = "はいけいは まだ ないよ。\nステージの うしろは なにも でないよ";
        public const string BackdropCurrent = "いまの はいけいは これだよ";
        public const string BackdropNew = "あたらしく とる";
        public const string BackdropClear = "なくす";
        public const string BackdropHint = "ステージの うしろに\nみせたい けしきを うつしてね\n（かべ、まど、へやの ようす など）";
        public const string BackdropAsk = "この けしきに する？";
        public const string BackdropSaved = "はいけいを ほぞんしたよ。\nつぎの ステージから うしろに でるよ";
        public const string BackdropCleared = "はいけいを なくしたよ";

        // ---- つなぎの文（画面をまたぐ確認など）----
        public const string CaptureLeaveConfirm = "とった しゃしんは なくなるよ。\nもどる？";
        public const string GeneratingCancelConfirm = "つくるのを やめる？";
        public const string PlayPausedTitle = "ひとやすみ";
        public const string PlayQuitConfirm = "あそぶのを やめて、\nいちらんへ もどる？";
        public const string CaptureTookTop = "1まいめ ＯＫ！";
        public const string CaptureLimitedDark = "くらいよ。あかるい ところで とってね";
        public const string CaptureLimitedBlur = "ぶれたよ。じっと して とってね";
        public const string PlayGoalFirst = "まず スタートと ゴールを おいてね";
        public const string PlayLoadFailed = "ステージを ひらけなかったよ";
        public const string PlayStageReady = "じゅんびできたよ";
        public const string CheckRescanButton = "もういちど とる";
        public const string CheckHeight = "たかさ";
        public const string CheckDeleteBox = "けす";
        public const string CheckModeCourseHint = "つくえの うえを タップして おいてね";
        public const string CharDeleteConfirm = "えらんでいる こを けすよ。\nもとには もどせないよ";
        public const string CharNamed = "キャラ";
        public const string BackdropFailed = "うまく とれなかったよ。\nもういちど やってみてね";
        public const string BackdropClearConfirm = "はいけいを なくすよ。\nいい？";
        public const string BackdropPick = "きれいな はいけいから えらぶ";
        public const string BackdropCurrentPreset = "いまの はいけい：{0}";
        public const string BackdropCurrentPhoto = "いまの はいけい：しゃしん";

        // ---- どれくらい のぼれるか（撮る前）----
        public const string ReachTitle = "どれくらいの たかさまで のぼれる？";
        public const string ReachWalkBadge = "あるいて";
        public const string ReachJumpBadge = "ジャンプ";
        public const string ReachWallBadge = "のぼれない";
        public const string ReachWalk = "{0}cm より ひくい ものは\nあるいて のぼれるよ";
        public const string ReachJump = "{0}cm までは ジャンプで のぼれるよ\n（2だんジャンプ！）";
        public const string ReachWall = "{0}cm より たかい ものは のぼれないよ\nとなりに ひくい ものを おくと のぼれるよ";
        public const string ReachExamples = "めやす：ノート 1cm、ティッシュの はこ 9cm、ペットボトル 20cm";
        public const string ReachHero = "キャラ（3cm）";
        public const string ReachGo = "とりはじめる";

        // ---- しかけ（折り紙の色）----
        public const string GimmickWater = "みず";
        public const string GimmickFire = "ほのお";
        public const string GimmickHeal = "かいふく";
        public const string GimmickFound = "しかけを みつけたよ：{0}";
        public const string GimmickHit = "あちち！ ハートが へったよ";
        public const string GimmickHealed = "かいふく！";
        public const string GimmickHint = "おりがみを はこの うえ いっぱいに はると、\nしかけに なるよ\n（あお：みず　あか：ほのお　みどり：かいふく）";
        public const string ReachGimmickBadge = "しかけ";
        public const string GameOverTitle = "ゲームオーバー";
        public const string GameOverHint = "ハートが なくなっちゃった";
        public const string GameOverRetry = "もういちど";
        public const string GameOverList = "いちらんへ";
        public const string GameOverFix = "コースを なおす";
    }
}
