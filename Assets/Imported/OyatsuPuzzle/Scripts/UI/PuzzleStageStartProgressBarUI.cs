using UnityEngine;

namespace OyatsuPuzzle
{
    // PuzzleStartPanel の StageProgressBg を制御する開始画面版コントローラ。
    // ClearOverlay 用の PuzzleStageProgressBarUI とは分離（共通化しない＝ClearOverlay に副作用を出さない）。
    // 既存ノード（PuzzleStageProgressNodeUI）をそのまま再利用し、状態は SetActive の ON/OFF だけで切り替える
    // （Image.color はスクリプトで変更しない）。
    //
    // currentStage = これから挑戦するステージ（PuzzleProgressManager.CurrentStage）。
    //   stage <  current → Cleared（クリア済み：金スタンプ＋「クリア！」）
    //   stage == current → Next   （これから挑戦：グレースタンプ＋「つぎはココ！」）
    //   stage >  current → Locked （未到達：薄グレースタンプ）
    //   全クリア（current > max） → 全ノード Cleared
    public class PuzzleStageStartProgressBarUI : MonoBehaviour
    {
        [Header("Nodes (Stage1..5 の順)")]
        [SerializeField] private PuzzleStageProgressNodeUI[] nodes = new PuzzleStageProgressNodeUI[5];

        // 開始画面の進行状態を反映する。
        // allClear=true（または currentStage>maxStage）の場合は全ノード Cleared にする。
        public void RefreshForStart(int currentStage, int maxStage = 5, bool allClear = false)
        {
            if (nodes == null) return;

            bool isAllClear = allClear || currentStage > maxStage;

            for (int i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                if (node == null) continue;

                int stage = i + 1;          // ノードのステージ番号(1始まり)
                node.SetNumber(stage);

                PuzzleStageProgressNodeUI.StageProgressNodeState state;
                if (isAllClear)
                    state = PuzzleStageProgressNodeUI.StageProgressNodeState.Cleared;
                else if (stage < currentStage)
                    state = PuzzleStageProgressNodeUI.StageProgressNodeState.Cleared;
                else if (stage == currentStage)
                    state = PuzzleStageProgressNodeUI.StageProgressNodeState.Next;
                else
                    state = PuzzleStageProgressNodeUI.StageProgressNodeState.Locked;

                node.SetState(state);
            }
        }
    }
}
