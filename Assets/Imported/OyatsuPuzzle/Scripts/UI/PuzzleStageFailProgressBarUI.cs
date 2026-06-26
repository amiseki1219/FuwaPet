using UnityEngine;

namespace OyatsuPuzzle
{
    // PuzzleStageFailOverlayPanel の StageProgressBg を制御する失敗版コントローラ。
    // クリア版（PuzzleStageProgressBarUI）とは分離。既存のノード（PuzzleStageProgressNodeUI）を
    // そのまま再利用し、状態を SetActive の ON/OFF だけで切り替える（Image.color はスクリプトで変更しない）。
    //
    // 失敗してもステージは進めないため、表示は次の通り：
    //   stage <  current → Cleared （過去にクリア済み）
    //   stage == current → Current （現在地。失敗したステージ。クリア！吹き出しは出さない）
    //   stage >  current → Locked  （未到達）
    public class PuzzleStageFailProgressBarUI : MonoBehaviour
    {
        [Header("Nodes (Stage1..5 の順)")]
        [SerializeField] private PuzzleStageProgressNodeUI[] nodes = new PuzzleStageProgressNodeUI[5];

        // 現在（失敗した）ステージを Current として表示する。
        public void RefreshForFail(int currentStage, int maxStage = 5)
        {
            if (nodes == null) return;

            for (int i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                if (node == null) continue;

                int stage = i + 1;          // ノードのステージ番号(1始まり)
                node.SetNumber(stage);

                if (stage < currentStage)
                {
                    node.SetState(PuzzleStageProgressNodeUI.StageProgressNodeState.Cleared);
                }
                else if (stage == currentStage)
                {
                    // 現在地（失敗ステージ）。ピンクスタンプ＋光は出すが「クリア！」吹き出しは消す。
                    node.SetState(PuzzleStageProgressNodeUI.StageProgressNodeState.Current);
                    HideClearedBubble(node);
                }
                else
                {
                    node.SetState(PuzzleStageProgressNodeUI.StageProgressNodeState.Locked);
                }
            }
        }

        // 「クリア！」吹き出しだけを SetActive(false) で隠す（失敗ステージにクリア表記を出さないため）。
        // 既存のクリア用スクリプトには手を入れず、子オブジェクト名で参照して OFF にするだけ。
        private static void HideClearedBubble(PuzzleStageProgressNodeUI node)
        {
            if (node == null) return;
            var cb = node.transform.Find("ClearedBubble");
            if (cb != null && cb.gameObject.activeSelf) cb.gameObject.SetActive(false);
        }
    }
}
