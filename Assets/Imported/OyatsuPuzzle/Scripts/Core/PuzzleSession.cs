using System;
using System.Collections.Generic;
using UnityEngine;

namespace OyatsuPuzzle
{
    // 1回のパズルゲームセッションの状態を管理する
    public class PuzzleSession
    {
        public StageDataSO StageData { get; }
        public PuzzleBoard Board { get; }
        public int RemainingMoves { get; private set; }
        public List<PieceGoal> Goals { get; }

        public bool IsCleared => Goals.TrueForAll(g => g.IsCleared);
        public bool IsFailed => RemainingMoves <= 0 && !IsCleared;
        public bool IsActive => !IsCleared && !IsFailed;

        public event Action<int> OnMovesChanged;          // 残り手数
        public event Action<List<PieceGoal>> OnGoalsChanged;
        public event Action OnCleared;
        public event Action OnFailed;

        public PuzzleSession(StageDataSO data)
        {
            StageData = data;
            RemainingMoves = data.maxMoves;
            Goals = data.CreateGoals();
            Board = new PuzzleBoard(data.boardSize, data.stageNumber);
            Board.OnPiecesCleared += HandlePiecesCleared;
        }

        // 入れ替え試行。有効手なら手数を消費し、状態を更新する。
        public bool TrySwap(int r1, int c1, int r2, int c2)
        {
            if (!IsActive) return false;

            bool valid = Board.TrySwap(r1, c1, r2, c2);
            if (!valid) return false;

            RemainingMoves--;
            OnMovesChanged?.Invoke(RemainingMoves);

            if (IsCleared)
                OnCleared?.Invoke();
            else if (IsFailed)
                OnFailed?.Invoke();

            return true;
        }

        public void NotifyGoalsChanged() => OnGoalsChanged?.Invoke(Goals);

        // Direct move consumption used by PuzzleGameScreenUI (Grid-direct swap path).
        public void ConsumeMove()
        {
            if (RemainingMoves > 0) RemainingMoves--;
            OnMovesChanged?.Invoke(RemainingMoves);
        }

#if UNITY_EDITOR
        public void DebugDecreaseMove()
        {
            if (RemainingMoves > 0) RemainingMoves--;
            OnMovesChanged?.Invoke(RemainingMoves);
        }

        public void DebugSetMovesToZero()
        {
            RemainingMoves = 0;
            OnMovesChanged?.Invoke(RemainingMoves);
        }
#endif

        private void HandlePiecesCleared(List<(int row, int col, PieceType type)> cleared)
        {
            foreach (var (_, _, type) in cleared)
            {
                foreach (var goal in Goals)
                {
                    if (goal.pieceType == type && !goal.IsCleared)
                    {
                        goal.clearedCount++;
                    }
                }
            }
            OnGoalsChanged?.Invoke(Goals);
        }
    }
}
