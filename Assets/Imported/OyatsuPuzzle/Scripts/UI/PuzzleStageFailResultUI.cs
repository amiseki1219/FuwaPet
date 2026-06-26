using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OyatsuPuzzle
{
    // PuzzleFailPanel の後に表示する全画面の「ステージ失敗」結果画面。
    // PuzzleStageClearOverlayPanel の失敗版（クリア用 PuzzleStageClearResultUI とは分離・非共通化）。
    // 表示内容は Refresh() で更新（PuzzleScreenController.ShowStageFailResult から呼ばれる）。
    // ボタンの onClick は Awake で実行時にひもづける。
    //
    // 失敗してもステージは進めない。現在ステージ（=失敗したステージ）にそのまま再挑戦できる。
    public class PuzzleStageFailResultUI : MonoBehaviour
    {
        [Header("Labels")]
        [Tooltip("タイトル（あと少し！）。")]
        [SerializeField] private TMP_Text titleText;
        [Tooltip("現在ステージ表示（Stage N）。")]
        [SerializeField] private TMP_Text currentStageText;
        [Tooltip("残りプレイ回数表示（2 / 5 など）。")]
        [SerializeField] private TMP_Text remainingPlaysText;
        [Tooltip("再挑戦説明（Stage N からもう一回チャレンジできるよ）。")]
        [SerializeField] private TMP_Text retryDescText;

        [Header("Encouragement (タイプライター)")]
        [Tooltip("表示するたびにランダムな応援メッセージをタイプライター表示する。")]
        [SerializeField] private PuzzleFailTypewriterTextUI encouragementTypewriter;

        [Header("Stage Progress")]
        [Tooltip("StageProgressBg に付けた失敗版の進行バー。現在ステージを Current 表示にする。")]
        [SerializeField] private PuzzleStageFailProgressBarUI stageProgressBarUI;

        [Header("Buttons")]
        [Tooltip("もう一回あそぶ（現在ステージに再挑戦）。")]
        [SerializeField] private Button retryButton;
        [Tooltip("ホームへ / スタート画面に戻る。")]
        [SerializeField] private Button backToStartButton;

        [Header("References")]
        [SerializeField] private PuzzleManager          puzzleManager;
        [SerializeField] private PuzzleDailyPlayManager dailyPlayManager;
        [SerializeField] private PuzzleScreenController  screenController;

        private bool _wired;

        private void Awake()
        {
            WireButtons();
        }

        private void WireButtons()
        {
            if (_wired) return;
            if (retryButton       != null) retryButton.onClick.AddListener(OnClickRetry);
            if (backToStartButton != null) backToStartButton.onClick.AddListener(OnClickBackToStart);
            _wired = true;
        }

        // 結果画面を開くたびに呼ぶ。失敗ステージはそのまま（進めない）なので CurrentStage を使う。
        public void Refresh()
        {
            int stage     = PuzzleProgressManager.CurrentStage;
            int remaining = dailyPlayManager != null ? dailyPlayManager.RemainingPlays : 0;
            int maxPlays  = dailyPlayManager != null ? dailyPlayManager.MaxPlays : 0;

            if (titleText        != null) titleText.text        = "あと少し！";
            if (currentStageText != null)
            {
                // 「ステージ」ブラウン＋太字 / 数字ピンク＋太字（同サイズ・空白なし・1行）。
                currentStageText.richText = true;
                currentStageText.text =
                    $"<color=#7A5136><b>ステージ</b></color><color=#F26373><b>{stage}</b></color>";
            }
            if (remainingPlaysText != null)
            {
                // 残り回数ピンク＋太字＋大きめ / 「 / 5」ブラウン＋太字（1行）。値は表示のみ。
                remainingPlaysText.richText = true;
                remainingPlaysText.text =
                    $"<color=#F26373><b><size=140%>{remaining}</size></b></color><color=#7A5136><b> / {maxPlays}</b></color>";
            }
            if (retryDescText    != null)
            {
                retryDescText.richText = true;
                // 残りプレイ回数が無ければ翌日案内（ステージに依らず）。RetryButton も remaining<=0 で
                // 非活性になるため、「また明日なのに押せる」矛盾も「ステージ○からなのに押せない」矛盾も起きない。
                // 通常フローでは Stage5 挑戦時に残り0になるため、Stage5失敗で自然に「また明日」になる。
                if (remaining <= 0)
                {
                    retryDescText.text = "<color=#A8704F>また明日チャレンジしてね♫</color>";
                }
                else
                {
                    // 「ステージN」だけピンク・太字・大きめ、残りはブラウン・通常サイズ（空白なし・1行）。
                    retryDescText.text =
                        $"<color=#F26373><b><size=120%>ステージ{stage}</size></b></color>" +
                        $"<color=#A8704F>からもう一回チャレンジできるよ</color>";
                }
            }

            // 進行バー：現在ステージを Current 表示（失敗してもステージは進めない）。
            if (stageProgressBarUI != null) stageProgressBarUI.RefreshForFail(stage);

            // 残りプレイが無ければ「もう一回あそぶ」は押せない。
            if (retryButton != null) retryButton.interactable = remaining > 0;

            // 応援メッセージをランダムでタイプライター表示。
            if (encouragementTypewriter != null) encouragementTypewriter.PlayRandom();
        }

        // もう一回あそぶ：現在ステージ（失敗したステージ）に再挑戦する。Stage は進めない。
        // 既存の PuzzleFailScreenUI.OnClickRetry と同じプレイ回数ロジックを踏襲（ロジック自体は変更しない）。
        public void OnClickRetry()
        {
            Debug.Log("[OyatsuPuzzle] Fail result: retry clicked.");

            if (dailyPlayManager == null || !dailyPlayManager.CanPlay())
            {
                Debug.Log("[OyatsuPuzzle] No plays remaining - returning to start.");
                screenController?.ShowStart();
                return;
            }

            dailyPlayManager.ConsumePlay();

            int stage = PuzzleProgressManager.CurrentStage;
            Debug.Log($"[OyatsuPuzzle] Restart failed stage: {stage}");
            puzzleManager?.StartCurrentStage();
            screenController?.ShowGame();
        }

        // ホームへ / スタート画面に戻る。
        public void OnClickBackToStart()
        {
            screenController?.ShowStart();
        }
    }
}
