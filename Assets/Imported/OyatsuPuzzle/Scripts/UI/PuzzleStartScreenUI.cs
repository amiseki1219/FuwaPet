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
        [SerializeField] private GameObject allClearRoot;

        [Header("AllClear Texts")]
        [SerializeField] private TMP_Text allClearTitleText;
        [SerializeField] private TMP_Text allClearSubText;
        [SerializeField] private TMP_Text allClearDetailText;
        [SerializeField] private TMP_Text allClearRewardText;
        [SerializeField] private TMP_Text allClearNoteText;
        [SerializeField] private TMP_Text allClearPlaysText;

        [Header("AllClear Stage Progress")]
        [SerializeField] private Image[]  allClearDotImages = new Image[5];

        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button helpButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button toPuzzleTopButton;

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

            bool allCleared = PuzzleAllClearManager.IsAllClearedToday;
            Debug.Log($"[OyatsuPuzzle] AllClearToday={allCleared}");

            if (allCleared)
            {
                Debug.Log("[OyatsuPuzzle] Showing AllClearRoot.");
                Debug.Log("[OyatsuPuzzle] Hiding NormalStartRoot.");
                ShowAllClearState(remaining, maxPlays);
                return;
            }

            Debug.Log("[OyatsuPuzzle] Showing NormalStartRoot.");
            Debug.Log("[OyatsuPuzzle] Hiding AllClearRoot.");
            ShowNormalState(stage, remaining, maxPlays, data);
        }

        private void ShowNormalState(int stage, int remaining, int maxPlays, StageDataSO data)
        {
            if (normalStartRoot != null) normalStartRoot.SetActive(true);
            if (allClearRoot    != null) allClearRoot.SetActive(false);
            if (startButton     != null) startButton.gameObject.SetActive(true);

            if (titleText != null) titleText.text = "Play Time";
            if (coinText  != null) coinText.text  = "1,250";

            if (stepBadgeText != null) stepBadgeText.text = $"STEP {stage}";
            if (subtitleText  != null) subtitleText.text  = "Poko Snack Puzzle";

            if (supportMessageText != null) supportMessageText.text = "Collect snacks!";

            if (remainingPlaysValueText != null)
                remainingPlaysValueText.text = $"Plays {remaining} / {maxPlays}";
            if (remainingPlaysNoteText != null)
                remainingPlaysNoteText.text = $"{remaining} plays left today";

            if (stageProgressLabel != null) stageProgressLabel.text = $"STEP {stage}";

            RefreshStageDots(stageDotImages, stage, allCleared: false);

            if (challengeTitleText != null) challengeTitleText.text = "Today's Challenge";

            if (goalTitleText != null) goalTitleText.text = $"Stage {stage} Goal";

            if (data.goals != null && data.goals.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var g in data.goals)
                {
                    sb.AppendLine($"{g.pieceType.ToEnglishName()} x{g.requiredCount}");
                    Debug.Log($"[OyatsuPuzzle] StartPanel goal: {g.pieceType.ToEnglishName()} x{g.requiredCount}");
                }
                if (goalItemText  != null) goalItemText.text  = sb.ToString().TrimEnd();
                if (goalCountText != null) goalCountText.text = "";
                if (goalNoteText  != null) goalNoteText.text  = "";
            }

            if (rewardListTitleText != null) rewardListTitleText.text = "Stage Rewards";

            SetRewardRows();

            if (startButton != null) startButton.interactable = remaining > 0;
        }

        private void ShowAllClearState(int remaining, int maxPlays)
        {
            if (normalStartRoot    != null) normalStartRoot.SetActive(false);
            if (allClearRoot       != null) allClearRoot.SetActive(true);
            if (startButton        != null) startButton.gameObject.SetActive(false);
            if (toPuzzleTopButton  != null) toPuzzleTopButton.gameObject.SetActive(true);

            if (allClearTitleText  != null) allClearTitleText.text  = "All Clear!";
            if (allClearSubText    != null) allClearSubText.text    = "Today's reward completed!";
            if (allClearDetailText != null) allClearDetailText.text = "All 5 stages cleared.";
            if (allClearRewardText != null) allClearRewardText.text = "Reward: Free Coin +150 + Trust +50pt";
            if (allClearNoteText   != null) allClearNoteText.text   = "Come back tomorrow!";
            if (allClearPlaysText  != null) allClearPlaysText.text  = "All done today!";

            RefreshStageDots(allClearDotImages, currentStage: 5, allCleared: true);
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

        public void OnClickToPuzzleTop()
        {
            Debug.Log("[OyatsuPuzzle] To Puzzle Top clicked.");
            Debug.Log("[OyatsuPuzzle] AllClearToday remains true.");

            if (allClearRoot      != null) allClearRoot.SetActive(false);
            if (toPuzzleTopButton != null) toPuzzleTopButton.gameObject.SetActive(false);
            if (startButton       != null) startButton.gameObject.SetActive(false);
            if (normalStartRoot   != null) normalStartRoot.SetActive(true);

            if (startButton != null)
            {
                startButton.interactable = false;
                Debug.Log("[OyatsuPuzzle] Start button disabled because all stages cleared today.");
            }

            Debug.Log("[OyatsuPuzzle] Showing completed start state.");

            int stage     = PuzzleProgressManager.CurrentStage;
            int remaining = dailyPlayManager != null ? dailyPlayManager.RemainingPlays : 0;
            int maxPlays  = dailyPlayManager != null ? dailyPlayManager.MaxPlays : 5;

            if (remainingPlaysValueText != null)
                remainingPlaysValueText.text = "All done today!";
            if (remainingPlaysNoteText != null)
                remainingPlaysNoteText.text = "Come back tomorrow!";

            RefreshStageDots(stageDotImages, currentStage: 5, allCleared: true);
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
            PuzzleAllClearManager.ResetAllClear();
            Refresh();
            Debug.Log("[OyatsuPuzzle] Start screen refreshed.");
        }

        public void OnClickDebugResetAll()
        {
            Debug.Log("[OyatsuPuzzle] Debug reset all clicked.");
#if UNITY_EDITOR
            PuzzleProgressManager.DebugResetStage();
#endif
            PuzzleAllClearManager.ResetAllClear();
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
