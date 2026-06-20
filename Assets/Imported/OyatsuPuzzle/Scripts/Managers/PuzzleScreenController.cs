using UnityEngine;

namespace OyatsuPuzzle
{
    // Panel切り替えを一元管理する
    public class PuzzleScreenController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject startPanel;
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private GameObject clearPanel;
        [SerializeField] private GameObject failPanel;

        [Header("UI Controllers")]
        [SerializeField] private PuzzleStartScreenUI startScreenUI;
        [SerializeField] private PuzzleGameScreenUI  gameScreenUI;
        [SerializeField] private PuzzleClearScreenUI clearScreenUI;
        [SerializeField] private PuzzleFailScreenUI  failScreenUI;

        private void Start()
        {
            ShowStart();
        }

        public void ShowStart()
        {
            HideAll();
            if (startPanel != null) startPanel.SetActive(true);
            if (startScreenUI != null) startScreenUI.Refresh();
        }

        public void ShowGame()
        {
            HideAll();
            if (gamePanel != null) gamePanel.SetActive(true);
            if (gameScreenUI != null) gameScreenUI.Refresh();
        }

        public void ShowClear()
        {
            HideAll();
            if (clearPanel != null) clearPanel.SetActive(true);
            if (clearScreenUI != null) clearScreenUI.Refresh();
        }

        public void ShowFail()
        {
            HideAll();
            if (failPanel != null) failPanel.SetActive(true);
            if (failScreenUI != null) failScreenUI.Refresh();
        }

        private void HideAll()
        {
            if (startPanel != null) startPanel.SetActive(false);
            if (gamePanel  != null) gamePanel.SetActive(false);
            if (clearPanel != null) clearPanel.SetActive(false);
            if (failPanel  != null) failPanel.SetActive(false);
        }
    }
}
