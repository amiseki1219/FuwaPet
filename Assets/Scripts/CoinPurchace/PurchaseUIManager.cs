using UnityEngine;
using TMPro; // TextMeshProを使うための魔法

public class PurchaseUIManager : MonoBehaviour
{
    // どこからでもこのマネージャーを呼べるようにする魔法（シングルトン）
    public static PurchaseUIManager Instance;

    [Header("ポップアップのUI設定")]
    public GameObject popupPanel;      // ポップアップの親パネル
    public TextMeshProUGUI messageText; // 「〇〇を購入しました」と出すテキスト

    private void Awake()
    {
        if (Instance == null) Instance = this;
        // 最初はポップアップを隠しておくお
        if (popupPanel != null) popupPanel.SetActive(false);
    }

    // ポップアップを出す命令だお！
    public void ShowCompletePopup(string itemName)
    {
        if (messageText != null)
        {
            messageText.text = $"{itemName}を\n購入しました";
        }

        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
        }
    }

    // 閉じるボタンから呼ぶ関数
    public void ClosePopup()
    {
        popupPanel.SetActive(false);
    }
}