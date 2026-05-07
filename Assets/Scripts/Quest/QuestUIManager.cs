using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// QuestPanel（デイリー用・4行）にアタッチする
public class QuestUIManager : MonoBehaviour
{
    [Header("ヘッダー")]
    [SerializeField] private TextMeshProUGUI resetTimerText;

    [Header("残高表示")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI ticketText;

    [Header("目標Panel")]
    [SerializeField] private TextMeshProUGUI progressBadgeText;
    [SerializeField] private Image progressBarFill;
    [SerializeField] private TextMeshProUGUI remainingText;
    [SerializeField] private Button bonusReceiveButton;

    [Header("各クエスト行")]
    [SerializeField] private QuestRowRefs loginRow;
    [SerializeField] private QuestRowRefs petRow;
    [SerializeField] private QuestRowRefs chatRow;
    [SerializeField] private QuestRowRefs eatRow;

    [Header("シーン遷移")]
    [SerializeField] private SceneLoader sceneLoader;

    [Header("閉じるボタン")]
    [SerializeField] private Button closeButton;

    // ─── ライフサイクル ──────────────────────────────────────

    private void Start()
    {
        SetupShortcutButtons();
        SetupReceiveButtons();
        bonusReceiveButton?.onClick.AddListener(OnReceiveBonus);
        closeButton?.onClick.AddListener(OnClose);

        RefreshUI();
        StartCoroutine(TimerCoroutine());
    }

    private void OnEnable()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestProgressChanged += RefreshUI;
        RefreshUI();
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestProgressChanged -= RefreshUI;
    }

    // ─── ボタン初期設定 ──────────────────────────────────────

    private void SetupShortcutButtons()
    {
        SetShortcut(petRow,  "なでにいく",       () => GetSceneLoader()?.GotoCare());
        SetShortcut(chatRow, "おはなししにいく",  () => GetSceneLoader()?.GoToChat());
        SetShortcut(eatRow,  "ご飯をあげにいく", () => GetSceneLoader()?.GotoCare());
    }

    private void SetShortcut(QuestRowRefs row, string label, UnityEngine.Events.UnityAction action)
    {
        if (row?.shortcutButton == null) return;
        row.shortcutButton.onClick.AddListener(action);
    }

    private void SetupReceiveButtons()
    {
        SetReceive(loginRow, QuestId.DailyLogin);
        SetReceive(petRow,   QuestId.DailyNade);
        SetReceive(chatRow,  QuestId.DailyChat);
        SetReceive(eatRow,   QuestId.DailyFeed);
    }

    private void SetReceive(QuestRowRefs row, QuestId id)
    {
        if (row?.receiveButton == null) return;
        row.receiveButton.onClick.AddListener(() =>
        {
            Debug.Log($"[QuestUIManager] 受け取りボタン押下: {id}");
            QuestManager.Instance?.ClaimReward(id);
            RefreshUI();
        });
    }

    // ─── UI更新 ──────────────────────────────────────────────

    public void RefreshUI()
    {
        if (QuestManager.Instance == null) return;

        bool[] done    = QuestManager.Instance.GetDailyQuestsDone();
        bool[] claimed = QuestManager.Instance.GetDailyQuestsClaimed();
        int nadeCount  = QuestManager.Instance.GetDailyNadeCount();

        // ログイン（常時表示・受け取り済みで非活性）
        UpdateRow(loginRow, done[0], claimed[0], done[0] ? 1f : 0f, done[0] ? "1" : "0");
        if (loginRow?.receiveButton != null)
        {
            loginRow.receiveButton.gameObject.SetActive(true);
            loginRow.receiveButton.interactable = !claimed[0];
        }

        // なでなで（進捗あり: 0〜3 の分子）
        float nadeFill = Mathf.Clamp01(nadeCount / 3f);
        UpdateRow(petRow, done[3], claimed[3], nadeFill, nadeCount.ToString(), total: 3);

        // 会話
        UpdateRow(chatRow, done[1], claimed[1], done[1] ? 1f : 0f, done[1] ? "1" : "0");

        // ごはん
        UpdateRow(eatRow, done[2], claimed[2], done[2] ? 1f : 0f, done[2] ? "1" : "0");

        // 残高
        if (coinText   != null) coinText.text   = GameData.Instance?.Coin.ToString() ?? "0";
        if (ticketText != null) ticketText.text = (SaveManager.Instance?.Data?.talkTicketCount ?? 0).ToString();

        // 進捗サマリー
        int completedCount = CountTrue(done);
        UpdateProgressSummary(completedCount, 4, QuestManager.Instance.IsDailyBonusClaimed());
    }

    private void UpdateRow(QuestRowRefs row, bool done, bool claimed,
        float progressFill, string progressLabel, bool noButton = false, int total = 1)
    {
        if (row?.root == null) return;

        if (row.progressBar != null)
            row.progressBar.fillAmount = progressFill;

        if (row.progressBadgeText != null)
            row.progressBadgeText.text = $"{progressLabel}/{total}";

        if (noButton)
        {
            row.receiveButton?.gameObject.SetActive(false);
            return;
        }

        if (claimed)
        {
            row.receiveButton?.gameObject.SetActive(false);
        }
        else if (done)
        {
            row.receiveButton?.gameObject.SetActive(true);
        }
        else
        {
            row.receiveButton?.gameObject.SetActive(false);
        }
    }

    private void UpdateProgressSummary(int completed, int total, bool bonusClaimed)
    {
        if (progressBadgeText != null)
            progressBadgeText.text = completed.ToString();

        if (progressBarFill != null)
            progressBarFill.fillAmount = total > 0 ? (float)completed / total : 0f;

        if (remainingText != null)
        {
            int remaining = total - completed;
            remainingText.text = remaining > 0
                ? $"あと{remaining}こでプレゼント！"
                : "全クリア達成！";
        }

        if (bonusReceiveButton != null)
        {
            bonusReceiveButton.gameObject.SetActive(true);
            bonusReceiveButton.interactable = completed >= total && !bonusClaimed;
        }
    }

    // ─── ボーナス受取 ────────────────────────────────────────

    private void OnReceiveBonus()
    {
        QuestManager.Instance?.ClaimBonusReward();
        RefreshUI();
    }

    // ─── カウントダウンタイマー ──────────────────────────────

    private IEnumerator TimerCoroutine()
    {
        while (true)
        {
            UpdateTimer();
            yield return new WaitForSeconds(1f);
        }
    }

    private void UpdateTimer()
    {
        if (resetTimerText == null) return;
        DateTime jst = DateTime.UtcNow.AddHours(9);
        DateTime nextReset = new DateTime(jst.Year, jst.Month, jst.Day, 3, 0, 0);
        if (jst >= nextReset) nextReset = nextReset.AddDays(1);
        TimeSpan span = nextReset - jst;
        resetTimerText.text = $"あと{(int)span.TotalHours}:{span.Minutes:D2}でリセット";
    }

    // ─── 閉じる ──────────────────────────────────────────────

    public void OnClose() => gameObject.SetActive(false);

    // ─── ヘルパー ────────────────────────────────────────────

    private int CountTrue(bool[] arr)
    {
        int count = 0;
        foreach (var b in arr) if (b) count++;
        return count;
    }

    private SceneLoader GetSceneLoader()
    {
        if (sceneLoader != null) return sceneLoader;
        sceneLoader = FindFirstObjectByType<SceneLoader>();
        return sceneLoader;
    }
}
