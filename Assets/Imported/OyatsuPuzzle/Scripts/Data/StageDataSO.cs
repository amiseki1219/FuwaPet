using System.Collections.Generic;
using UnityEngine;

namespace OyatsuPuzzle
{
    [CreateAssetMenu(fileName = "StageData", menuName = "OyatsuPuzzle/StageData")]
    public class StageDataSO : ScriptableObject
    {
        public int stageNumber;
        public int boardSize;       // 5 or 6
        public int maxMoves;
        public List<PieceGoalEntry> goals = new();
        public RewardData reward;

        [System.Serializable]
        public class PieceGoalEntry
        {
            public PieceType pieceType;
            public int requiredCount;
        }

        public List<PieceGoal> CreateGoals()
        {
            var list = new List<PieceGoal>();
            foreach (var g in goals)
                list.Add(new PieceGoal(g.pieceType, g.requiredCount));
            return list;
        }
    }
}
