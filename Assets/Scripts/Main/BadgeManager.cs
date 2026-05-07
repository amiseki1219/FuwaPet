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

        bool show = false;
        if (QuestManager.Instance.IsTutorialPhase)
        {
            var done    = QuestManager.Instance.GetTutorialQuestsDone();
            var claimed = QuestManager.Instance.GetTutorialQuestsClaimed();
            for (int i = 0; i < done.Length; i++)
            {
                if (done[i] && !claimed[i]) { show = true; break; }
            }
        }
        else
        {
            var done    = QuestManager.Instance.GetDailyQuestsDone();
            var claimed = QuestManager.Instance.GetDailyQuestsClaimed();
            for (int i = 0; i < done.Length; i++)
            {
                if (done[i] && !claimed[i]) { show = true; break; }
            }
        }

        questBadge.SetActive(show);
    }

    private void CheckNoticeBadge()
    {
        if (noticeBadge == null) return;
        var readIds = SaveManager.Instance?.Data?.readNoticeIds;
        // 未読IDがあるか確認（NoticeManager.allNoticeIds 経由）
        var allIds = NoticeManager.GetAllNoticeIds();
        bool hasUnread = false;
        foreach (var id in allIds)
        {
            if (readIds == null || !readIds.Contains(id)) { hasUnread = true; break; }
        }
        noticeBadge.SetActive(hasUnread);
    }

    private void CheckShopBadge()
    {
        if (shopBadge == null) return;
        var data = SaveManager.Instance?.Data;
        bool show = data == null || data.lastShopVersion != currentShopVersion;
        shopBadge.SetActive(show);
    }

    private void CheckGachaBadge()
    {
        if (gachaBadge == null) return;
        var data = SaveManager.Instance?.Data;
        bool show = data == null || data.lastGachaVersion != currentGachaVersion;
        gachaBadge.SetActive(show);
    }

    private void CheckCollectionBadge()
    {
        if (collectionBadge == null) return;
        var data = SaveManager.Instance?.Data;
        bool show = data == null || data.lastCollectionVersion != currentCollectionVersion;
        collectionBadge.SetActive(show);
    }

    // ─── 外部から呼ぶ公開メソッド ─────────────────────────────

    public void OnQuestPanelClosed()    => CheckQuestBadge();
    public void OnNoticePanelClosed()   => CheckNoticeBadge();

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
