using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[Serializable]
public class NoticeData
{
    public string id;
    public string title;
    public string body;
    public string date;
    public string category; // "notice" or "campaign"
    public Sprite thumbnail;
    public bool isRead;
}

public class NoticeItem : MonoBehaviour
{
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private GameObject newBadge;
    [SerializeField] private GameObject categoryNotice;   // ピンク「おしらせ」Image
    [SerializeField] private GameObject categoryCampaign; // 黄色「キャンペーン」Image
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TextMeshProUGUI dateText;

    public void Setup(NoticeData data)
    {
        if (titleText != null)   titleText.text = data.title;
        if (bodyText != null)    bodyText.text  = data.body;
        if (dateText != null)    dateText.text  = data.date;

        if (thumbnailImage != null && data.thumbnail != null)
            thumbnailImage.sprite = data.thumbnail;

        if (newBadge != null)
            newBadge.SetActive(!data.isRead);

        bool isNotice   = data.category == "notice";
        bool isCampaign = data.category == "campaign";
        categoryNotice?.SetActive(isNotice);
        categoryCampaign?.SetActive(isCampaign);
    }
}
