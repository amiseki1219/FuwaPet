using TMPro;
using UnityEngine;
using Game.Core; // これを追加！

public class CurrencyHUD : MonoBehaviour
{
    [SerializeField] TMP_Text coinText;
    [SerializeField] TMP_Text lunaStoneText; // 有償コイン用

    void Update()
    {
        // どちらかのテキスト枠が空っぽなら止める
        if (coinText == null || lunaStoneText == null) return;

        if (GameData.Instance == null)
        {
            coinText.text = "---";
            lunaStoneText.text = "---";
            return;
        }

        // 無償コインを表示
        coinText.text = $"{GameData.Instance.Coin}";

        // 有償コイン（ダイヤ）を表示！
        // GameDataにさっき追加した「PaidCoin」を見に行くよ
        lunaStoneText.text = $"{GameData.Instance.LunaStone}";
    }
}