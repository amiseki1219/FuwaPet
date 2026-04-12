using UnityEngine;
using TMPro;

public class LevelDisplay : MonoBehaviour
{
    private TextMeshProUGUI levelText;

    void Awake()
    {
        levelText = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.Data == null) return;

        int currentLevel = SaveManager.Instance.Data.playerLevel;
        if (currentLevel <= 0) currentLevel = 1;
        levelText.text = "Lv. " + currentLevel.ToString();
    }
}