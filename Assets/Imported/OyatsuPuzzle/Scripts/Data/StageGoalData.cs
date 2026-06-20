using System;
using System.Collections.Generic;

namespace OyatsuPuzzle
{
    [Serializable]
    public class PieceGoal
    {
        public PieceType pieceType;
        public int requiredCount;
        public int clearedCount;

        public bool IsCleared => clearedCount >= requiredCount;
        public int Remaining => Math.Max(0, requiredCount - clearedCount);

        public PieceGoal(PieceType type, int count)
        {
            pieceType = type;
            requiredCount = count;
            clearedCount = 0;
        }
    }

    [Serializable]
    public class RewardData
    {
        public RewardType rewardType;
        public int amount;
        public PieceType pieceReward; // RewardType.Piece のとき
        public int trustPoints;       // Stage5 信頼度

        public enum RewardType
        {
            FreeCoin,
            Piece,
            RandomPiece,
            FreeCoinPlusTrust,
        }
    }
}
