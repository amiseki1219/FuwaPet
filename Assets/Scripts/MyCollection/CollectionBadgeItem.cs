using UnityEngine;
using UnityEngine.UI;

public class BadgeCollectionItem : MonoBehaviour
{
    [Header("バッジの設定")]
    public string myBadgeId;       // ファイル名（Badge_1 など）
    public int requiredAmount;     // 解放金額（0, 1000, 5000...）

    [Header("UIパーツ")]
    public Image badgeImage;       // 暗くする絵
    public GameObject lockIcon;    // 鍵アイコン

    // パネルが開くたびに自動チェック！
    void OnEnable()
    {
        RefreshDisplay();
    }

    public void RefreshDisplay()
    {

        if (SaveManager.Instance == null) return;
        long currentTotal = SaveManager.Instance.Data.totalBillingAmount;

        // 2. 「今の金額」が「必要金額」以上なら解放！
        // 例：10000円持ってれば、1000円のバッジも5000円のバッジも True になるお！
        bool isUnlocked = currentTotal >= requiredAmount;

        // 3. 見た目の切り替え
        if (isUnlocked)
        {
            // ★解放済み：明るく！鍵なし！
            if (badgeImage != null) badgeImage.color = Color.white;
            if (lockIcon != null) lockIcon.SetActive(false);
        }
        else
        {
            // ★未解放：暗く！鍵あり！
            if (badgeImage != null) badgeImage.color = new Color(0.3f, 0.3f, 0.3f);
            if (lockIcon != null) lockIcon.SetActive(true);
        }
    }

}
