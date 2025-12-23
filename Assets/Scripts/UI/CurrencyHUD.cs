using TMPro;
using UnityEngine;

public class CurrencyHUD : MonoBehaviour
{
    [SerializeField] TMP_Text coinText;

    void Update()
    {
        if (GameData.Instance == null) return;
        coinText.text = $"Coin: {GameData.Instance.Coin}";
    }
}
