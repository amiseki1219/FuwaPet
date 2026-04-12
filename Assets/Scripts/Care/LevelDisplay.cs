using UnityEngine;
using TMPro;
using Game.Core;

public class LevelDisplay : MonoBehaviour
{
    private TextMeshProUGUI levelText;

    void Awake()
    {
        levelText = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (GameContext.Instance == null || GameContext.Instance.PetStatus == null)
        {
            return;
        }

        int currentLevel = GameContext.Instance.PetStatus.Level;
        levelText.text = "Lv. " + currentLevel.ToString();

        // ★ ここでセーブデータにも「今のレベル」を教えてあげる！
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            SaveManager.Instance.Data.playerLevel = currentLevel;
        }
    }
}