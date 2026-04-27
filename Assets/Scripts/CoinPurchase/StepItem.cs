using UnityEngine;
using UnityEngine.UI;

public class ShopItem : MonoBehaviour
{
    [Header("アイテムの設定")]
    public string packName; // 「初回限定パック」「パックA」「パックB」など名前を入れてね
    public int addLunaStone;
    public int addTalkTicket;
    public int addSitterTicket;
    public int addCloudCandy;
    public string addDecoId;

    [Header("UIの設定（任意）")]
    public Button purchaseButton; // 購入ボタンを紐付けておくと便利だお

    // Unityのスタート時に一度ボタンの状態をチェックするお
    void Start()
    {
        CheckPurchaseLimit();
    }

    // ボタンからこの関数を呼ぶように設定してね！
    public void Buy()
    {
        // --- 1. 購入制限のチェック ---
        if (!CanIByThis())
        {
            Debug.Log($"{packName} は今は買えないお！");
            return;
        }

        // --- 2. アイテムを付与（バックエンド処理） ---
        // Luna Stoneを増やす
        GameData.Instance.AddLunaStone(addLunaStone);

        // その他のアイテムがあれば増やす
        if (addTalkTicket > 0) GameData.Instance.AddTalkTicket(addTalkTicket);
        if (addSitterTicket > 0) GameData.Instance.AddSitterTicket(addSitterTicket);
        if (addCloudCandy > 0) GameData.Instance.AddCloudCandy(addCloudCandy);

        // デコアイテムがあれば追加
        if (!string.IsNullOrEmpty(addDecoId)) GameData.Instance.AddDeco(addDecoId);

        // --- 3. 購入した記録を残す ---
        if (packName == "初回限定パック") GameData.Instance.OnBuyFirstTimePack();
        else if (packName == "パックA") GameData.Instance.OnBuyPackA();
        else if (packName == "パックB") GameData.Instance.OnBuyPackB();

        PurchaseUIManager.Instance.ShowCompletePopup(packName);

        Debug.Log($"{packName} を購入完了だっぴ！✨");

        // --- 4. 買った後にボタンの状態を更新 ---
        CheckPurchaseLimit();
    }

    // 今買えるかどうかを判定する関数だお
    private bool CanIByThis()
    {
        if (packName == "初回限定パック") return GameData.Instance.CanBuyFirstTimePack();
        if (packName == "パックA") return GameData.Instance.CanBuyPackA();
        if (packName == "パックB") return GameData.Instance.CanBuyPackB();

        return true; // 普通の石などはいつでも買えるお！
    }

    // 買えない場合にボタンを押せなくする演出だお
    public void CheckPurchaseLimit()
    {
        if (purchaseButton != null)
        {
            bool canBuy = CanIByThis();
            purchaseButton.interactable = canBuy; // 買えない時はボタンをグレーにするお
        }
    }
}