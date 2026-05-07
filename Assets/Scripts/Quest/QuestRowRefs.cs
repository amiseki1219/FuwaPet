using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[Serializable]
public class QuestRowRefs
{
    public GameObject root;
    public Image progressBar;                    // fillAmount で進捗表示
    public TextMeshProUGUI progressBadgeText;    // 達成バッジ（例: "1/1"、"2/3"）
    public Button receiveButton;                // 完了・未受取時の受取ボタン（ピンク）
    public Button shortcutButton;              // 未完了時のショートカットボタン
}
