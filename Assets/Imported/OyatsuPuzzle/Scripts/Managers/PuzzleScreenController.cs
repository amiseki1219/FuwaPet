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
        [Tooltip("PuzzleClearPanel の後に出す全画面のステージクリア結果画面（報酬・次ステージ・ボタン）。")]
        [SerializeField] private GameObject stageClearOverlayPanel;
        [Tooltip("PuzzleFailPanel の後に出す全画面のステージ失敗結果画面（再挑戦・スタート戻り・ボタン）。")]
        [SerializeField] private GameObject stageFailOverlayPanel;

        [Header("UI Controllers")]
        [SerializeField] private PuzzleStartScreenUI startScreenUI;
        [SerializeField] private PuzzleGameScreenUI  gameScreenUI;
        [SerializeField] private PuzzleClearScreenUI clearScreenUI;
        [SerializeField] private PuzzleFailScreenUI  failScreenUI;
        [SerializeField] private PuzzleStageClearResultUI stageClearResultUI;
        [SerializeField] private PuzzleStageFailResultUI  stageFailResultUI;

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
            // クリア画面は透明背景。背面にクリア直後の PuzzleGamePanel を見せたいので
            // HideAll() は使わず、gamePanel は表示のまま clearPanel を上に重ねる。
            // start/fail/overlay は重ならないよう非表示にする（gamePanel を消すのは ShowStart 側）。
            if (startPanel != null) startPanel.SetActive(false);
            if (failPanel  != null) failPanel.SetActive(false);
            if (stageClearOverlayPanel != null) stageClearOverlayPanel.SetActive(false); // 一時演出は閉じる
            if (gamePanel  != null) gamePanel.SetActive(true);   // 表示維持（クリア直後の盤面を見せる）
            if (clearPanel != null) clearPanel.SetActive(true);
            if (clearScreenUI != null) clearScreenUI.Refresh();
        }

        // PuzzleClearPanel の後に出すステージクリア結果画面（報酬・次ステージ・ボタン）。
        // 盤面・背景を背面に見せたいので gamePanel は表示のまま、その上に overlay を重ねる。
        // 盤面操作は overlay 背景 Image の Raycast ＋ ゲーム側 _inputLocked で二重にブロックされる。
        public void ShowStageClearResult()
        {
            if (startPanel != null) startPanel.SetActive(false);
            if (gamePanel  != null) gamePanel.SetActive(true);    // 盤面は背面に表示維持
            if (clearPanel != null) clearPanel.SetActive(false);
            if (failPanel  != null) failPanel.SetActive(false);
            if (stageFailOverlayPanel  != null) stageFailOverlayPanel.SetActive(false);
            if (stageClearOverlayPanel != null) stageClearOverlayPanel.SetActive(true);
            if (stageClearResultUI != null) stageClearResultUI.Refresh();
        }

        // 失敗時の透明オーバーレイ（PuzzleFailPanel）。クリアの ShowClear と同様、背面に盤面を見せたまま
        // 上に重ねて涙演出を出す。演出完了後に ShowStageFailResult へ進む。
        public void ShowFail()
        {
            if (startPanel != null) startPanel.SetActive(false);
            if (clearPanel != null) clearPanel.SetActive(false);
            if (stageClearOverlayPanel != null) stageClearOverlayPanel.SetActive(false);
            if (stageFailOverlayPanel  != null) stageFailOverlayPanel.SetActive(false);
            if (gamePanel != null) gamePanel.SetActive(true);   // 表示維持（失敗直後の盤面を見せる）
            if (failPanel != null) failPanel.SetActive(true);
            if (failScreenUI != null) failScreenUI.Refresh();   // 新FailPanelには無いので null 安全に skip
        }

        // PuzzleFailPanel の後に出すステージ失敗結果画面（再挑戦・スタート戻り・ボタン）。
        // 盤面・背景を背面に見せたいので gamePanel は表示のまま、その上に overlay を重ねる。
        // 盤面操作は overlay 背景 Image の Raycast ＋ ゲーム側 _inputLocked で二重にブロックされる。
        public void ShowStageFailResult()
        {
            if (startPanel != null) startPanel.SetActive(false);
            if (gamePanel  != null) gamePanel.SetActive(true);    // 盤面は背面に表示維持
            if (clearPanel != null) clearPanel.SetActive(false);
            if (failPanel  != null) failPanel.SetActive(false);
            if (stageClearOverlayPanel != null) stageClearOverlayPanel.SetActive(false);
            if (stageFailOverlayPanel  != null) stageFailOverlayPanel.SetActive(true);
            if (stageFailResultUI != null) stageFailResultUI.Refresh();
        }

        private void HideAll()
        {
            if (startPanel != null) startPanel.SetActive(false);
            if (gamePanel  != null) gamePanel.SetActive(false);
            if (clearPanel != null) clearPanel.SetActive(false);
            if (failPanel  != null) failPanel.SetActive(false);
            if (stageClearOverlayPanel != null) stageClearOverlayPanel.SetActive(false);
            if (stageFailOverlayPanel  != null) stageFailOverlayPanel.SetActive(false);
        }
    }
}
