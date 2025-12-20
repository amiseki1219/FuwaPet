using UnityEngine;

public class ConversationManager : MonoBehaviour
{
    public static ConversationManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public string GetReply()
    {
        int trust = GameData.Instance.Trust;

        if (trust < 10)
            return GetLowTrustReply();
        else if (trust < 30)
            return GetMidTrustReply();
        else
            return GetHighTrustReply();
    }

    public struct ReplyData
    {
        public string text;
        public string emotion; // "Happy", "Sad", "Normal" など
    }

    // ---- Trust 低 ----
    string GetLowTrustReply()
    {
        string[] lines =
        {
            "……なに？",
            "べつに。",
            "あんまり話したくない。",
        };
        return RandomLine(lines);
    }

    // ---- Trust 中 ----
    string GetMidTrustReply()
    {
        string[] lines =
        {
            "どうしたの？",
            "ふーん、そうなんだ。",
            "まあまあ、元気だよ。",
        };
        return RandomLine(lines);
    }

    // ---- Trust 高 ----
    string GetHighTrustReply()
    {
        string[] lines =
        {
            "ねえねえ、聞いてよ！",
            "きみと話すの、好き。",
            "今日も一緒にいよう？",
        };
        return RandomLine(lines);
    }

    string RandomLine(string[] lines)
    {
        return lines[Random.Range(0, lines.Length)];
    }
}
