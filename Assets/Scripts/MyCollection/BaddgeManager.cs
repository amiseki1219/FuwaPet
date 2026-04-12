using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class BadgeManager : MonoBehaviour
{
    public static BadgeManager Instance;

    [System.Serializable]
    public class BadgeData
    {
        public string badgeId;
        public int requiredYen;
    }

    [Header("バッジのランク設定リスト")]
    public List<BadgeData> badgeList = new List<BadgeData>();

    [Header("表示する場所")]
    public RawImage targetBadgeImage;

    private void Awake() => Instance = this;

    void Start()
    {
        UpdateBadgeDisplay();
    }

    public string GetCurrentBestBadgeId()
    {
        // ★ここを本番用に直したお！
        // SaveManagerが生きていれば、本当の課金額を取得する
        long currentAmount = 0;
        if (SaveManager.Instance != null)
        {
            currentAmount = SaveManager.Instance.Data.totalBillingAmount;
        }

        // 条件を満たす中で一番高いやつを探す
        var bestBadge = badgeList
            .OrderByDescending(b => b.requiredYen)
            .FirstOrDefault(b => currentAmount >= b.requiredYen);

        if (bestBadge != null)
        {
            return bestBadge.badgeId;
        }

        return "";
    }

    public void UpdateBadgeDisplay()
    {
        string bestId = GetCurrentBestBadgeId();

        if (string.IsNullOrEmpty(bestId))
        {
            if (targetBadgeImage != null) targetBadgeImage.enabled = false;
            return;
        }

        Texture tex = Resources.Load<Texture>("BadgeUI/" + bestId);

        if (targetBadgeImage != null)
        {
            if (tex != null)
            {
                targetBadgeImage.enabled = true;
                targetBadgeImage.texture = tex;
            }
            else
            {
                targetBadgeImage.enabled = false;
            }
        }
    }
}