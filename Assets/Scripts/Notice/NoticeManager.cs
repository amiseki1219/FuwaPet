using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class NoticeManager : MonoBehaviour
{
    [SerializeField] private GameObject noticePanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Transform scrollContent;
    [SerializeField] private GameObject noticeItemPrefab;
    [SerializeField] private BadgeManager badgeManager;

    private static readonly List<NoticeData> dummyNotices = new()
    {
        new NoticeData
        {
            id       = "notice_001",
            title    = "アップデートのお知らせ",
            body     = "アプリをアップデートしました。新機能をお楽しみください。",
            date     = "2026/05/01",
            category = "notice",
        },
        new NoticeData
        {
            id       = "notice_002",
            title    = "メンテナンスのお知らせ",
            body     = "5月10日（日）午前3:00〜5:00にメンテナンスを実施します。",
            date     = "2026/05/06",
            category = "notice",
        },
        new NoticeData
        {
            id       = "notice_003",
            title    = "ごはんショップ追加のお知らせ",
            body     = "ごはんショップに新しいメニューが追加されました。ぜひ試してみてください。",
            date     = "2026/05/07",
            category = "notice",
        },
        new NoticeData
        {
            id       = "campaign_001",
            title    = "初回限定キャンペーン開催中！",
            body     = "今だけ！初回ログインで🪙300 + 🎫15枚プレゼント！",
            date     = "2026/05/01 10:00",
            category = "campaign",
            isRead   = false,
        },
        new NoticeData
        {
            id       = "campaign_002",
            title    = "ゴールデンウィークキャンペーン",
            body     = "期間中にログインするだけで豪華アイテムをプレゼント🎁",
            date     = "2026/05/03 12:00",
            category = "campaign",
            isRead   = false,
        },
    };

    private void Start()
    {
        closeButton?.onClick.AddListener(HidePanel);
    }

    public void ShowPanel()
    {
        Debug.Log("ShowPanel called");
        Debug.Log($"ダミーデータ件数: {dummyNotices.Count}");
        if (noticePanel != null)
            noticePanel.SetActive(true);
        GenerateNotices();
    }

    public void HidePanel()
    {
        if (noticePanel != null)
            noticePanel.SetActive(false);

        foreach (var data in dummyNotices)
            MarkAsRead(data.id);

        SaveManager.Instance?.Save();
        badgeManager?.OnNoticePanelClosed();
    }

    private void GenerateNotices()
    {
        if (scrollContent == null || noticeItemPrefab == null) return;
        var debugIds = SaveManager.Instance?.Data?.readNoticeIds;
        Debug.Log($"readNoticeIds: [{string.Join(", ", debugIds ?? new System.Collections.Generic.List<string>())}]");

        foreach (Transform child in scrollContent)
            Destroy(child.gameObject);

        var readIds = SaveManager.Instance?.Data?.readNoticeIds ?? new List<string>();

        var sorted = dummyNotices
            .OrderByDescending(d => DateTime.ParseExact(
                d.date,
                new[] { "yyyy/MM/dd HH:mm", "yyyy/MM/dd" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None))
            .ToList();

        foreach (var data in sorted)
        {
            Debug.Log($"NoticeItem生成: {data.title}");
            data.isRead = readIds.Contains(data.id);
            var go = Instantiate(noticeItemPrefab, scrollContent);
            Debug.Log($"生成されたオブジェクト: {go.name}, parent: {go.transform.parent?.name}");
            go.GetComponent<NoticeItem>()?.Setup(data);
        }
    }

    public static System.Collections.Generic.List<string> GetAllNoticeIds()
    {
        var ids = new System.Collections.Generic.List<string>();
        foreach (var d in dummyNotices) ids.Add(d.id);
        return ids;
    }

    public void MarkAsRead(string id)
    {
        if (SaveManager.Instance == null) return;
        var ids = SaveManager.Instance.Data.readNoticeIds;
        if (!ids.Contains(id))
        {
            ids.Add(id);
            SaveManager.Instance.Save();
        }
    }
}
