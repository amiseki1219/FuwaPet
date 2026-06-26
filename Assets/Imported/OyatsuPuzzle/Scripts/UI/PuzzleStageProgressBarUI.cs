using UnityEngine;
using UnityEngine.UI;

namespace OyatsuPuzzle
{
    // StageProgressBg 全体（5ノード）を制御する。
    // 結果画面から RefreshForResult(...) を呼んで、各ノードの状態を更新する。
    public class PuzzleStageProgressBarUI : MonoBehaviour
    {
        [Header("Nodes (Stage1..5 の順)")]
        [SerializeField] private PuzzleStageProgressNodeUI[] nodes = new PuzzleStageProgressNodeUI[5];

        

        // 結果画面用：clearedStage=今クリアしたステージ / nextStage=次に挑むステージ。
        // 全クリア（nextStage>maxStage）時は 1..max を Cleared にし、Next は出さない。
        public void RefreshForResult(int clearedStage, int nextStage, int maxStage = 5)
        {
            bool allClear = nextStage > maxStage;

            if (nodes != null)
            {
                for (int i = 0; i < nodes.Length; i++)
                {
                    var node = nodes[i];
                    if (node == null) continue;

                    int stage = i + 1;          // ノードのステージ番号(1始まり)
                    node.SetNumber(stage);

                    PuzzleStageProgressNodeUI.StageProgressNodeState state;
                    if (allClear)
                    {
                        // 全クリアは例外：全ノード Cleared
                        state = PuzzleStageProgressNodeUI.StageProgressNodeState.Cleared;
                    }
                    else if (stage < clearedStage)
                    {
                        // 過去にクリア済み → 金スタンプ＋「クリア！」吹き出し
                        state = PuzzleStageProgressNodeUI.StageProgressNodeState.Cleared;
                    }
                    else if (stage == clearedStage)
                    {
                        // 今回クリアしたステージ → ピンクスタンプ＋光＋「クリア！」吹き出し
                        state = PuzzleStageProgressNodeUI.StageProgressNodeState.Current;
                    }
                    else if (stage == nextStage)
                    {
                        state = PuzzleStageProgressNodeUI.StageProgressNodeState.Next;
                    }
                    else
                    {
                        state = PuzzleStageProgressNodeUI.StageProgressNodeState.Locked;
                    }

                    node.SetState(state);
                }
            }
        }
    }
}
