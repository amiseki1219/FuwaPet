using UnityEngine;
using TMPro;

namespace OyatsuPuzzle
{
    // PuzzleStartPanel 専用のテキスト管理。
    // ステージ別の GoalText（見出し）/ SupportMessageText（具体的な目標内容）を Inspector で管理し、
    // 現在ステージに応じて反映する。GoalText が未バインドでもエラーにならない。
    public class PuzzleStartPanelTextController : MonoBehaviour
    {
        [System.Serializable]
        public class StageTextEntry
        {
            public int stage;
            [TextArea] public string goalText;
            [TextArea] public string supportMessageText;
        }

        [Header("Target Texts")]
        [Tooltip("見出し用。未バインド(None)でもOK。後で作成したらバインドしてください。")]
        [SerializeField] private TextMeshProUGUI goalText;
        [Tooltip("具体的な目標内容用。既存の SupportMessageText をバインドします。")]
        [SerializeField] private TextMeshProUGUI supportMessageText;

        [Header("Stage Texts (Stage1〜Stage5)")]
        [SerializeField] private StageTextEntry[] stageTexts =
        {
            new StageTextEntry
            {
                stage = 1,
                goalText = "ステージ1の目標",
                supportMessageText = "にぼしを5個集めよう！",
            },
            new StageTextEntry
            {
                stage = 2,
                goalText = "ステージ2の目標",
                supportMessageText = "にぼし5個\nビスケット3個を集めよう！",
            },
            new StageTextEntry
            {
                stage = 3,
                goalText = "ステージ3の目標",
                supportMessageText = "コインピースを8個集めよう！",
            },
            new StageTextEntry
            {
                stage = 4,
                goalText = "ステージ4の目標",
                supportMessageText = "にぼし6個＋にんじん4個＋ビスケット4個集めよう！",
            },
            new StageTextEntry
            {
                stage = 5,
                goalText = "ステージ5の目標",
                supportMessageText = "プリン＋ハートマカロン＋いちごケーキ3個ずつ集めよう！",
            },
        };

        // 現在ステージに対応するテキストを反映する。
        public void ApplyStage(int stage)
        {
            if (stageTexts == null) return;

            StageTextEntry entry = null;
            foreach (var e in stageTexts)
            {
                if (e != null && e.stage == stage) { entry = e; break; }
            }
            if (entry == null) return;

            if (goalText != null) goalText.text = entry.goalText;
            if (supportMessageText != null) supportMessageText.text = entry.supportMessageText;
        }
    }
}
///////////