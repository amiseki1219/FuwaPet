using UnityEngine;
using Game.Core;

public class ConversationManager : MonoBehaviour
{
    public static ConversationManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public string GetReply()
    {
        var pet = GameContext.Instance.PetStatus;

        // 例：空腹が低いと不機嫌
        if (pet.Hunger <= 20) return RandomLine(new[]
        {
            "おなかすいた…",
            "ごはん…ほしい…",
            "ちょっとイライラ…"
        });

        // 機嫌が低い
        if (pet.Mood <= 20) return RandomLine(new[]
        {
            "いま、きぶんじゃない…",
            "あとでね…",
            "むぅ…"
        });

        // ふつう〜ごきげん
        return RandomLine(new[]
        {
            "ねえねえ！",
            "きょうもいっしょ！",
            "なでて〜"
        });
    }

    string RandomLine(string[] lines) => lines[Random.Range(0, lines.Length)];
}
