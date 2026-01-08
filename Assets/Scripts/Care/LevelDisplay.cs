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
        // --- ここが「エラー爆発」を防ぐ最強のガード！ ---
        // 1. 心臓部(GameContext)がない
        // 2. ペットのステータス(PetStatus)がまだ準備できてない
        // このどちらかでも当てはまるなら、何もしないで帰る（return）よ！
        if (GameContext.Instance == null || GameContext.Instance.PetStatus == null)
        {
            return;
        }

        // 準備が整ったときだけ、レベルを表示するよ
        int currentLevel = GameContext.Instance.PetStatus.Level;
        levelText.text = "Lv. " + currentLevel.ToString();
    }
}