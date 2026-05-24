using UnityEngine;

public class BadgeManager : MonoBehaviour
{
    [Header("バッジGameObject")]
    [SerializeField] private GameObject questBadge;
    [SerializeField] private GameObject noticeBadge;
    [SerializeField] private GameObject shopBadge;
    [SerializeField] private GameObject gachaBadge;
    [SerializeField] private GameObject collectionBadge;

    [Header("バージョン管理（新着チェック用）")]
    [SerializeField] private string currentShopVersion       = "2026.05.07";
    [SerializeField] private string currentGachaVersion      = "2026.05.07";
    [SerializeField] private string currentCollectionVersion = "2026.05.07";

    private void Start()   => RefreshAllBadges();
    private void OnEnable() => RefreshAllBadges();

    // ─── 全バッジ更新 ─────────────────────────────────────────

    public void RefreshAllBadges()
    {
        CheckQuestBadge();
        CheckNoticeBadge();
        CheckShopBadge();
        CheckGachaBadge();
        CheckCollectionBadge();
    }

    // ─── 各バッジ判定 ─────────────────────────────────────────

    private void CheckQuestBadge()
    {
        if (questBadge == null || QuestManager.Instance == null) return;

        bool[] done;
        bool[] claimed;
        if (QuestManager.Instance.IsTutorialPhase)
        {
            done    = QuestManager.Instance.GetTutorialQuestsDone();
            claimed = QuestManager.Instance.GetTutorialQuestsClaimed();
        }
        else
        {
            done    = QuestManager.Instance.GetDailyQuestsDone();
            claimed = QuestManager.Instance.GetDailyQuestsClaimed();
        }

        // 全クエストが done=true かつ claimed=true の場合のみ非表示
        bool allComplete = true;
        for (int i = 0; i < done.Length; i++)
        {
            if (!done[i] || !claimed[i]) { allComplete = false; break; }
        }

        questBadge.SetActive(!allComplete);
    }

    private void CheckNoticeBadge()
    {
        if (noticeBadge == null) return;
        var readIds = SaveManager.Instance?.Data?.readNoticeIds;
        var allIds = NoticeManager.GetAllNoticeIds();
        bool hasUnread = false;
        foreach (var id in allIds)
        {
            if (readIds == null || !readIds.Contains(id)) { hasUnread = true; break; }
        }
        Debug.Log($"[BadgeManager] readNoticeIds count: {readIds?.Count ?? -1}");
        Debug.Log($"[BadgeManager] allIds count: {allIds.Count}");
        Debug.Log($"[BadgeManager] hasUnread: {hasUnread}");
        noticeBadge.SetActive(hasUnread);
    }

    private void CheckShopBadge()
    {
        if (shopBadge == null) return;
        // 現時点は常に非表示。アップデート時に currentShopVersion を更新して有効化
        shopBadge.SetActive(false);
    }

    private void CheckGachaBadge()
    {
        if (gachaBadge == null) return;
        // 現時点は常に非表示。アップデート時に currentGachaVersion を更新して有効化
        gachaBadge.SetActive(false);
    }

    private void CheckCollectionBadge()
    {
        if (collectionBadge == null) return;
        // 現時点は常に非表示。アップデート時に currentCollectionVersion を更新して有効化
        collectionBadge.SetActive(false);
    }

    // ─── 外部から呼ぶ公開メソッド ─────────────────────────────

    public void OnQuestPanelClosed()    => CheckQuestBadge();
    public void OnNoticePanelClosed()   => CheckNoticeBadge();
    public void HideNoticeBadge()       { if (noticeBadge != null) noticeBadge.SetActive(false); }

    public void OnShopVisited()
    {
        var data = SaveManager.Instance?.Data;
        if (data == null) return;
        data.lastShopVersion = currentShopVersion;
        SaveManager.Instance.Save();
        CheckShopBadge();
    }

    public void OnGachaVisited()
    {
        var data = SaveManager.Instance?.Data;
        if (data == null) return;
        data.lastGachaVersion = currentGachaVersion;
        SaveManager.Instance.Save();
        CheckGachaBadge();
    }

    public void OnCollectionVisited()
    {
        var data = SaveManager.Instance?.Data;
        if (data == null) return;
        data.lastCollectionVersion = currentCollectionVersion;
        SaveManager.Instance.Save();
        CheckCollectionBadge();
    }
}
