using TMPro;
using UnityEngine;

public class CurrencyHUD : MonoBehaviour
{
    [SerializeField] TMP_Text coinText;
    [SerializeField] TMP_Text trustText;

    void Update()
    {
        if (GameData.Instance == null) return;
        coinText.text = $"Coin: {GameData.Instance.Coin}";
        trustText.text = $"Trust: {GameData.Instance.Trust}";
    }
}
