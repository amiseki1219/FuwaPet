using System.Collections.Generic;
using UnityEngine;

namespace OyatsuPuzzle
{
    // ゲームセッション全体を管理するMonoBehaviour
    public class PuzzleManager : MonoBehaviour
    {
        public PuzzleSession CurrentSession { get; private set; }
        public string LastRewardText { get; private set; }

        // 表示用の状態（結果画面の進行表示・全クリア判定に使う。報酬/進行/プレイ回数ロジックには影響しない）。
        public int  LastClearedStage { get; private set; } // 直近にクリアしたステージ番号
        public bool IsAllClear       { get; private set; } // 直近クリアで全ステージ達成したか

        public void StartCurrentStage()
        {
            int stage = PuzzleProgressManager.CurrentStage;
            var data = PuzzleStageRegistry.GetStage(stage);
            CurrentSession = new PuzzleSession(data);
            PuzzleSessionStateManager.MarkSessionStarted(stage);
            Debug.Log($"[OyatsuPuzzle] Stage started: Stage {stage} ({data.boardSize}x{data.boardSize}) moves:{data.maxMoves}");
            foreach (var g in CurrentSession.Goals)
                Debug.Log($"[OyatsuPuzzle] Goal added: {g.pieceType.ToEnglishName()} 0 / {g.requiredCount}");
        }

        public void FinishClear()
        {
            if (CurrentSession == null) return;
            int clearedStage = CurrentSession.StageData.stageNumber;
            LastRewardText = RewardManager.GiveReward(CurrentSession.StageData.reward, clearedStage);

            // 表示用に「直近クリアステージ」と「全クリアか」を記録（CurrentStage は AdvanceStage で頭打ちになり、
            // Stage5クリア後も 5 のままになるため、結果画面はこの値を使う）。
            LastClearedStage = clearedStage;
            IsAllClear       = clearedStage >= PuzzleStageRegistry.StageCount;

            PuzzleProgressManager.AdvanceStage();
            PuzzleSessionStateManager.MarkSessionCompleted();

            Debug.Log($"[OyatsuPuzzle] Stage {clearedStage} cleared.");
        }

        public void FinishFail()
        {
            PuzzleSessionStateManager.MarkSessionCompleted();
        }

        public void ClearSession()
        {
            CurrentSession  = null;
            LastRewardText  = null;
        }
    }
}
