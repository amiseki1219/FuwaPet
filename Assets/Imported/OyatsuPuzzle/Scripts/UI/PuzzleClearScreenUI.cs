using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OyatsuPuzzle
{
    public class PuzzleClearScreenUI : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField] private TMP_Text clearTitleText;   // 将来「できた！」「すごい！」等に差し替え
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private TMP_Text nextInfoText;

        [Header("Buttons")]
        [SerializeField] private Button backToPuzzleStartButton;

        [Header("References")]
        [SerializeField] private PuzzleManager          puzzleManager;
        [SerializeField] private PuzzleScreenController screenController;

        private void Awake()
        {
            // PersistentListener (Editor API) で登録済みのため実行時の二重登録を防ぐ
        }

        public void Refresh()
        {
            if (clearTitleText != null) clearTitleText.text = "Clear!";

            if (rewardText != null)
            {
                string reward = puzzleManager?.LastRewardText ?? "";
                rewardText.text = string.IsNullOrEmpty(reward) ? "No Reward" : $"Reward\n{reward}";
            }

            if (nextInfoText != null)
            {
                int next = PuzzleProgressManager.CurrentStage;
                nextInfoText.text = next <= PuzzleStageRegistry.StageCount
                    ? $"Next: Stage {next}"
                    : "All Stages Clear!";
            }
        }

        public void OnClickBackToStart()
        {
            screenController?.ShowStart();
        }
    }
}
