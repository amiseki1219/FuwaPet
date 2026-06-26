using UnityEngine;
using TMPro;

namespace OyatsuPuzzle
{
    // ステージ進行バーの1ノード（1ステージ分）を制御する。
    // 状態(Cleared/Current/Next/Locked)に応じて、肉球スタンプ画像・光・吹き出しを ON/OFF する。
    // 画像/吹き出し背景は各 Image 枠に後から差し替えられる（このスクリプトは表示のON/OFFのみ担当）。
    public class PuzzleStageProgressNodeUI : MonoBehaviour
    {
        public enum StageProgressNodeState
        {
            Cleared, // クリア済み（金色スタンプ）
            Current, // 今回クリア／現在地（ピンクスタンプ＋光）
            Next,    // 次に挑戦（Lockedと同じグレースタンプ＋「つぎはココ！」吹き出し）
            Locked,  // 未到達（薄グレースタンプ）
        }

        [Header("Number")]
        [SerializeField] private TMP_Text stageNumberText;

        [Header("Stamp Images (状態別の肉球スタンプ。画像はここに割り当て)")]
        [SerializeField] private GameObject clearedStampImage; // 金色
        [SerializeField] private GameObject currentStampImage; // ピンク
        [SerializeField] private GameObject lockedStampImage;  // 薄グレー（Next状態でも流用）

        [Header("Glow (現在ステージの光)")]
        [SerializeField] private GameObject currentGlowImage;

        [Header("Bubbles (吹き出し。文字は吹き出し画像に内蔵。Labelは持たない)")]
        [SerializeField] private GameObject clearedBubble; // 「クリア！」
        [SerializeField] private GameObject nextBubble;    // 「つぎはココ！」

        // 状態を反映して各GameObjectをON/OFFする。
        public void SetState(StageProgressNodeState state)
        {
            Toggle(clearedStampImage, state == StageProgressNodeState.Cleared);
            Toggle(currentStampImage, state == StageProgressNodeState.Current);
            // Next 専用スタンプは廃止。Next 状態は Locked と同じグレー肉球スタンプを流用する。
            Toggle(lockedStampImage,  state == StageProgressNodeState.Locked || state == StageProgressNodeState.Next);

            Toggle(currentGlowImage,  state == StageProgressNodeState.Current);

            // ClearedBubble「クリア！」は Cleared と Current（今回クリア）の両方で表示。
            Toggle(clearedBubble, state == StageProgressNodeState.Cleared || state == StageProgressNodeState.Current);
            Toggle(nextBubble,    state == StageProgressNodeState.Next);
            // CurrentBubble / LockedBubble は廃止。Locked は吹き出しなし。
        }

        // ステージ番号テキストを設定（StageNumberText は常時表示）。
        public void SetNumber(int stageNumber)
        {
            if (stageNumberText != null) stageNumberText.text = stageNumber.ToString();
        }

        private static void Toggle(GameObject go, bool on)
        {
            if (go != null && go.activeSelf != on) go.SetActive(on);
        }
    }
}
