using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// TutorialQuestPanel（チュートリアル用・5行）にアタッチする
public class TutorialQuestUIManager : MonoBehaviour
{
    [Header("ヘッダー")]
    [SerializeField] private TextMeshProUGUI timeLimitText;

    [Header("目標Panel")]
    [SerializeField] private TextMeshProUGUI progressBadgeText;
    [SerializeField] private Image progressBarFill;
    [SerializeField] private TextMeshProUGUI remainingText;
    [SerializeField] private Button bonusReceiveButton;

    [Header("各クエスト行")]
    [SerializeField] private QuestRowRefs settingRow;
    [SerializeField] private QuestRowRefs petRow;
    [SerializeField] private QuestRowRefs chatRow;
    [SerializeField] private QuestRowRefs shopRow;
    [SerializeField] private QuestRowRefs eatRow;

    [Header("残高表示")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI ticketText;

    [Header("ボーナス行（GoalImage）")]
    [SerializeField] private TextMeshProUGUI bonusCoinText;
    [SerializeField] private TextMeshProUGUI bonusTicketText;

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

        if (bonusCoinText   != null) bonusCoinText.text   = "×100";
        if (bonusTicketText != null) bonusTicketText.text = "×5";

        RefreshUI();
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
        SetShortcut(chatRow,    () => GetSceneLoader()?.GoToChat());
        SetShortcut(eatRow,     () => GetSceneLoader()?.GotoCare());
        SetShortcut(petRow,     () => GetSceneLoader()?.GotoCare());
        SetShortcut(shopRow,    () =>
        {
            QuestManager.Instance?.NotifyShopOpened();
            GetSceneLoader()?.GoToShop();
        });
        SetShortcut(settingRow, () =>
        {
            QuestManager.Instance?.NotifySettingOpened();
            GetSceneLoader()?.GoToSetting();
        });
    }

    private void SetShortcut(QuestRowRefs row, UnityEngine.Events.UnityAction action)
    {
        if (row?.shortcutButton == null) return;
        row.shortcutButton.onClick.AddListener(action);
    }

    private void SetupReceiveButtons()
    {
        SetReceive(chatRow,    QuestId.TutorialChat);
        SetReceive(eatRow,     QuestId.TutorialFeed);
        SetReceive(petRow,     QuestId.TutorialNade);
        SetReceive(shopRow,    QuestId.TutorialShop);
        SetReceive(settingRow, QuestId.TutorialSetting);
    }

    private void SetReceive(QuestRowRefs row, QuestId id)
    {
        if (row?.receiveButton == null) return;
        row.receiveButton.onClick.AddListener(() =>
        {
            Debug.Log($"[TutorialQuestUIManager] 受け取りボタン押下: {id}");
            QuestManager.Instance?.ClaimReward(id);
            RefreshUI();
        });
    }

    // ─── UI更新 ──────────────────────────────────────────────

    public void RefreshUI()
    {
        if (QuestManager.Instance == null) return;

        bool[] done    = QuestManager.Instance.GetTutorialQuestsDone();
        bool[] claimed = QuestManager.Instance.GetTutorialQuestsClaimed();

        // TutorialChat=0, TutorialFeed=1, TutorialNade=2, TutorialShop=3, TutorialSetting=4
        UpdateRow(chatRow,    done[0], claimed[0], done[0] ? 1f : 0f, done[0] ? "1" : "0");
        UpdateRow(eatRow,     done[1], claimed[1], done[1] ? 1f : 0f, done[1] ? "1" : "0");
        UpdateRow(petRow,     done[2], claimed[2], done[2] ? 1f : 0f, done[2] ? "1" : "0");
        UpdateRow(shopRow,    done[3], claimed[3], done[3] ? 1f : 0f, done[3] ? "1" : "0");
        UpdateRow(settingRow, done[4], claimed[4], done[4] ? 1f : 0f, done[4] ? "1" : "0");

        // 残高
        if (coinText   != null) coinText.text   = GameData.Instance?.Coin.ToString() ?? "0";
        if (ticketText != null) ticketText.text = (SaveManager.Instance?.Data?.talkTicketCount ?? 0).ToString();

        int completedCount = CountTrue(done);
        UpdateProgressSummary(completedCount, 5, QuestManager.Instance.IsTutorialBonusClaimed());
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
            row.shortcutButton?.gameObject.SetActive(false);
            row.receiveButton?.gameObject.SetActive(false);
            return;
        }

        if (claimed)
        {
            row.shortcutButton?.gameObject.SetActive(false);
            row.receiveButton?.gameObject.SetActive(false);
        }
        else if (done)
        {
            row.shortcutButton?.gameObject.SetActive(false);
            row.receiveButton?.gameObject.SetActive(true);
        }
        else
        {
            row.shortcutButton?.gameObject.SetActive(true);
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
