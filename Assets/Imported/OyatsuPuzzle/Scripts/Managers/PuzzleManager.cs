using System.Collections.Generic;
using UnityEngine;

namespace OyatsuPuzzle
{
    // ゲームセッション全体を管理するMonoBehaviour
    public class PuzzleManager : MonoBehaviour
    {
        public PuzzleSession CurrentSession { get; private set; }
        public string LastRewardText { get; private set; }

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
            PuzzleProgressManager.AdvanceStage();
            PuzzleSessionStateManager.MarkSessionCompleted();

            Debug.Log($"[OyatsuPuzzle] Stage {clearedStage} cleared.");

            if (clearedStage >= PuzzleStageRegistry.StageCount)
                PuzzleAllClearManager.MarkAllClearedToday();
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
