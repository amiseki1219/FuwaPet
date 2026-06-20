using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OyatsuPuzzle
{
    public class PuzzleFailScreenUI : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField] private TMP_Text failTitleText;        // 将来「あと少し！」等に差し替え
        [SerializeField] private TMP_Text currentStageText;
        [SerializeField] private TMP_Text remainingPlaysText;

        [Header("Buttons")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button backToPuzzleStartButton;

        [Header("References")]
        [SerializeField] private PuzzleManager          puzzleManager;
        [SerializeField] private PuzzleDailyPlayManager dailyPlayManager;
        [SerializeField] private PuzzleScreenController screenController;

        private void Awake()
        {
            // PersistentListener (Editor API) で登録済みのため実行時の二重登録を防ぐ
        }

        public void Refresh()
        {
            if (failTitleText != null) failTitleText.text = "Fail";

            int stage = PuzzleProgressManager.CurrentStage;
            if (currentStageText != null) currentStageText.text = $"Stage {stage}";

            int remaining = dailyPlayManager != null ? dailyPlayManager.RemainingPlays : 0;
            if (remainingPlaysText != null)
                remainingPlaysText.text = $"Plays left: {remaining}";

            if (retryButton != null)
                retryButton.interactable = remaining > 0;
        }

        public void OnClickRetry()
        {
            Debug.Log("[OyatsuPuzzle] Retry clicked.");

            if (dailyPlayManager == null || !dailyPlayManager.CanPlay())
            {
                Debug.Log("[OyatsuPuzzle] No plays remaining - returning to start.");
                screenController?.ShowStart();
                return;
            }

            Debug.Log("[OyatsuPuzzle] Retry consume play.");
            dailyPlayManager.ConsumePlay();

            int stage = PuzzleProgressManager.CurrentStage;
            Debug.Log($"[OyatsuPuzzle] Restart stage: {stage}");
            puzzleManager?.StartCurrentStage();
            screenController?.ShowGame();
        }

        public void OnClickBackToStart()
        {
            screenController?.ShowStart();
        }
    }
}
