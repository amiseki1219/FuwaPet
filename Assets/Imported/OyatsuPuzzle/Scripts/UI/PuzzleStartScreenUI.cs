using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OyatsuPuzzle
{
    public class PuzzleStartScreenUI : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private Button   backButton;
        [SerializeField] private Button   plusButton;

        [Header("Stage Title")]
        [SerializeField] private TMP_Text stepBadgeText;
        [SerializeField] private TMP_Text subtitleText;

        [Header("Support Message")]
        [SerializeField] private TMP_Text supportMessageText;

        [Header("Remaining Plays Card")]
        [SerializeField] private TMP_Text remainingPlaysValueText;
        [SerializeField] private TMP_Text remainingPlaysNoteText;

        [Header("Stage Progress")]
        [SerializeField] private TMP_Text stageProgressLabel;
        [SerializeField] private Image[]  stageDotImages = new Image[5];
        [Tooltip("ClearOverlay と同デザインの肉球進行バー（StartPanel用）。割り当て時はこちらを更新する。")]
        [SerializeField] private PuzzleStageStartProgressBarUI startStageProgressBarUI;

        [Header("Challenge Section")]
        [SerializeField] private TMP_Text challengeTitleText;

        [Header("Goal Card")]
        [SerializeField] private TMP_Text goalTitleText;
        [SerializeField] private TMP_Text goalItemText;
        [SerializeField] private TMP_Text goalCountText;
        [SerializeField] private TMP_Text goalNoteText;

        [Header("Reward List")]
        [SerializeField] private TMP_Text rewardListTitleText;
        [SerializeField] private TMP_Text rewardRow1Text;
        [SerializeField] private TMP_Text rewardRow2Text;
        [SerializeField] private TMP_Text rewardRow3Text;
        [SerializeField] private TMP_Text rewardRow4Text;
        [SerializeField] private TMP_Text rewardRow5Text;

        [Header("Root Objects")]
        [SerializeField] private GameObject normalStartRoot;

        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button helpButton;
        [SerializeField] private Button homeButton;

        [Header("Debug Root (Editor Only)")]
        [SerializeField] private GameObject debugButtonsRoot;

        [Header("Debug Buttons")]
        [SerializeField] private Button debugResetPlaysButton;
        [SerializeField] private Button debugResetStageButton;
        [SerializeField] private Button debugResetAllButton;
        [SerializeField] private Button debugSetStage4Button;
        [SerializeField] private Button debugSetStage5Button;

        [Header("References")]
        [SerializeField] private PuzzleManager          puzzleManager;
        [SerializeField] private PuzzleDailyPlayManager dailyPlayManager;
        [SerializeField] private PuzzleScreenController screenController;
        [SerializeField] private PuzzleStartPanelTextController textController;
        [SerializeField] private PuzzleStartPanelImageController startPanelImageController;

        private void Awake()
        {
#if UNITY_EDITOR
            if (debugButtonsRoot != null) debugButtonsRoot.SetActive(true);
#else
            if (debugButtonsRoot != null) debugButtonsRoot.SetActive(false);
#endif
        }

        public void Refresh()
        {
            if (!ValidateReferences()) return;

            PuzzleDailyResetManager.CheckDailyReset(puzzleManager);
            PuzzleSessionStateManager.CheckAndClearInterrupted();

            int stage     = PuzzleProgressManager.CurrentStage;
            int remaining = dailyPlayManager.RemainingPlays;
            int maxPlays  = dailyPlayManager.MaxPlays;
            var data      = PuzzleStageRegistry.GetStage(stage);

            Debug.Log("[OyatsuPuzzle] StartPanel Refresh.");
            Debug.Log($"[OyatsuPuzzle] currentStage={stage}");

            ShowNormalState(stage, remaining, maxPlays, data);
        }

        private void ShowNormalState(int stage, int remaining, int maxPlays, StageDataSO data)
        {
            if (normalStartRoot != null) normalStartRoot.SetActive(true);
            if (startButton     != null) startButton.gameObject.SetActive(true);

            if (titleText != null) titleText.text = "Play Time";
            if (coinText  != null) coinText.text  = "1,250";

            if (stepBadgeText != null) stepBadgeText.text = $"STEP {stage}";
            if (subtitleText  != null) subtitleText.text  = "Poko Snack Puzzle";

            if (remainingPlaysValueText != null)
                remainingPlaysValueText.text = $"{remaining}";
            if (remainingPlaysNoteText != null)
                remainingPlaysNoteText.text = remaining > 0
                    ? $"今日はあと{remaining}回あそべるよ♪"
                    : "今日はもう遊びきったよ♪";

            if (stageProgressLabel != null) stageProgressLabel.text = $"STEP {stage}";

            RefreshStageDots(stageDotImages, stage, allCleared: false);

            // ClearOverlay と同デザインの肉球進行バー（StartPanel用）を現在ステージで更新。
            // currentStage = これから挑戦するステージ。全クリア済み（CurrentStage 頭打ちで判別不可）の場合は
            // IsAllCleared フラグで全ノード Cleared にする（残りプレイ回数とは無関係）。
            if (startStageProgressBarUI != null)
                startStageProgressBarUI.RefreshForStart(stage, PuzzleStageRegistry.StageCount, PuzzleProgressManager.IsAllCleared);

            if (challengeTitleText != null) challengeTitleText.text = "Today's Challenge";

            // ステージ別の GoalText / SupportMessageText は PuzzleStartPanelTextController が管理（二重管理回避）
            if (textController != null) textController.ApplyStage(stage);

            // CharacterImage / GoalImage の切り替え（キャラはSaveData、目標画像は現在ステージ）
            startPanelImageController?.Apply(stage);

            if (rewardListTitleText != null) rewardListTitleText.text = "Stage Rewards";

            SetRewardRows();

            if (startButton != null) startButton.interactable = remaining > 0;
        }

        private void RefreshStageDots(Image[] dots, int currentStage, bool allCleared)
        {
            bool anyNull = dots == null || dots.Length < 5;
            if (!anyNull)
                for (int i = 0; i < dots.Length; i++)
                    if (dots[i] == null) { anyNull = true; break; }

            if (anyNull) return;

            var active   = new Color(0.95f, 0.42f, 0.55f);
            var cleared  = new Color(1.00f, 0.70f, 0.80f);
            var inactive = new Color(0.88f, 0.80f, 0.73f);

            for (int i = 0; i < dots.Length; i++)
            {
                if (dots[i] == null) continue;

                Color col;
                if (allCleared)
                    col = active;
                else if (i + 1 < currentStage)
                    col = cleared;
                else if (i + 1 == currentStage)
                    col = active;
                else
                    col = inactive;

                dots[i].color = col;
            }
        }

        public void OnClickStart()
        {
            if (dailyPlayManager == null || !dailyPlayManager.CanPlay())
            {
                Debug.Log("[OyatsuPuzzle] No plays remaining today.");
                Refresh();
                return;
            }
            dailyPlayManager.ConsumePlay();
            if (puzzleManager    != null) puzzleManager.StartCurrentStage();
            if (screenController != null) screenController.ShowGame();
        }

        public void OnClickBack()  => Debug.Log("[OyatsuPuzzle] Back clicked");
        public void OnClickHome()  => Debug.Log("[OyatsuPuzzle] Home clicked");
        public void OnClickHelp()  => Debug.Log("[OyatsuPuzzle] Help clicked");
        public void OnClickPlus()  => Debug.Log("[OyatsuPuzzle] Plus clicked");

        public void OnClickDebugResetPlays()
        {
            Debug.Log("[OyatsuPuzzle] Debug reset plays clicked.");
            if (dailyPlayManager != null)
                dailyPlayManager.DebugResetPlays();
            Refresh();
            Debug.Log("[OyatsuPuzzle] Start screen refreshed.");
        }

        public void OnClickDebugResetStage()
        {
            Debug.Log("[OyatsuPuzzle] Debug reset stage clicked.");
#if UNITY_EDITOR
            PuzzleProgressManager.DebugResetStage();
#endif
            Refresh();
            Debug.Log("[OyatsuPuzzle] Start screen refreshed.");
        }

        public void OnClickDebugResetAll()
        {
            Debug.Log("[OyatsuPuzzle] Debug reset all clicked.");
#if UNITY_EDITOR
            PuzzleProgressManager.DebugResetStage();
#endif
            PuzzleRewardClaimManager.ResetAll();
            Stage4RandomRewardManager.ResetToday();
            PuzzleSessionStateManager.ResetAll();
            if (dailyPlayManager != null)
                dailyPlayManager.DebugResetPlays();
            if (puzzleManager != null)
                puzzleManager.ClearSession();
            if (startButton != null) startButton.gameObject.SetActive(true);
            Refresh();
            Debug.Log("[OyatsuPuzzle] Start screen refreshed.");
        }

        public void OnClickDebugSetStage4()
        {
            Debug.Log("[OyatsuPuzzle] Debug set stage clicked: 4");
#if UNITY_EDITOR
            PuzzleProgressManager.DebugSetStage(4);
#endif
            Refresh();
            Debug.Log("[OyatsuPuzzle] Start screen refreshed.");
        }

        public void OnClickDebugSetStage5()
        {
            Debug.Log("[OyatsuPuzzle] Debug set stage clicked: 5");
#if UNITY_EDITOR
            PuzzleProgressManager.DebugSetStage(5);
#endif
            Refresh();
            Debug.Log("[OyatsuPuzzle] Start screen refreshed.");
        }

        private bool ValidateReferences()
        {
            bool ok = true;
            if (remainingPlaysValueText == null)
            { Debug.LogError("PuzzleStartScreenUI: remainingPlaysValueText not assigned", this); ok = false; }
            if (puzzleManager == null)
            { Debug.LogError("PuzzleStartScreenUI: puzzleManager not assigned", this); ok = false; }
            if (dailyPlayManager == null)
            { Debug.LogError("PuzzleStartScreenUI: dailyPlayManager not assigned", this); ok = false; }
            return ok;
        }

        private void SetRewardRows()
        {
            TMP_Text[] rows  = { rewardRow1Text, rewardRow2Text, rewardRow3Text, rewardRow4Text, rewardRow5Text };
            string[]   texts =
            {
                "1  Free Coin +50",
                "2  Niboshi x1",
                "3  Free Coin +50",
                "4  Random Reward",
                "5  Free Coin +150 + Trust +50pt",
            };
            for (int i = 0; i < rows.Length; i++)
                if (rows[i] != null)
                    rows[i].text = i < texts.Length ? texts[i] : "";
        }
    }
}
