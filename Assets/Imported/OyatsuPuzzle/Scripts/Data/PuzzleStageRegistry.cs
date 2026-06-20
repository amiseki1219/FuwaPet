using System.Collections.Generic;
using UnityEngine;

namespace OyatsuPuzzle
{
    // ランタイムでステージ定義を取得するレジストリ。
    // ScriptableObject が未設定の場合はコードのデフォルト定義にフォールバックする。
    public static class PuzzleStageRegistry
    {
        private static List<StageDataSO> _overrides;

        public static void SetOverrides(List<StageDataSO> stages) => _overrides = stages;

        public static StageDataSO GetStage(int stageNumber)
        {
            if (_overrides != null)
            {
                var so = _overrides.Find(s => s.stageNumber == stageNumber);
                if (so != null) return so;
            }
            return CreateDefault(stageNumber);
        }

        public static int StageCount => 5;

        private static StageDataSO CreateDefault(int n)
        {
            var so = ScriptableObject.CreateInstance<StageDataSO>();
            so.stageNumber = n;

            switch (n)
            {
                case 1:
                    so.boardSize = 5; so.maxMoves = 15;
                    so.goals.Add(new StageDataSO.PieceGoalEntry { pieceType = PieceType.Niboshi, requiredCount = 5 });
                    so.reward = new RewardData { rewardType = RewardData.RewardType.FreeCoin, amount = 50 };
                    break;
                case 2:
                    so.boardSize = 5; so.maxMoves = 14;
                    so.goals.Add(new StageDataSO.PieceGoalEntry { pieceType = PieceType.Niboshi, requiredCount = 5 });
                    so.goals.Add(new StageDataSO.PieceGoalEntry { pieceType = PieceType.Biscuit, requiredCount = 3 });
                    so.reward = new RewardData { rewardType = RewardData.RewardType.Piece, pieceReward = PieceType.Niboshi, amount = 1 };
                    break;
                case 3:
                    so.boardSize = 5; so.maxMoves = 13;
                    so.goals.Add(new StageDataSO.PieceGoalEntry { pieceType = PieceType.Coin, requiredCount = 8 });
                    so.reward = new RewardData { rewardType = RewardData.RewardType.FreeCoin, amount = 50 };
                    break;
                case 4:
                    so.boardSize = 6; so.maxMoves = 12;
                    so.goals.Add(new StageDataSO.PieceGoalEntry { pieceType = PieceType.Niboshi, requiredCount = 6 });
                    so.goals.Add(new StageDataSO.PieceGoalEntry { pieceType = PieceType.Biscuit, requiredCount = 4 });
                    so.goals.Add(new StageDataSO.PieceGoalEntry { pieceType = PieceType.CarrotStick, requiredCount = 4 });
                    so.reward = new RewardData { rewardType = RewardData.RewardType.RandomPiece };
                    break;
                case 5:
                    so.boardSize = 6; so.maxMoves = 10;
                    so.goals.Add(new StageDataSO.PieceGoalEntry { pieceType = PieceType.Pudding, requiredCount = 5 });
                    so.goals.Add(new StageDataSO.PieceGoalEntry { pieceType = PieceType.HeartMacaron, requiredCount = 5 });
                    so.goals.Add(new StageDataSO.PieceGoalEntry { pieceType = PieceType.StrawberryCake, requiredCount = 5 });
                    so.reward = new RewardData { rewardType = RewardData.RewardType.FreeCoinPlusTrust, amount = 150, trustPoints = 50 };
                    break;
                default:
                    so.boardSize = 5; so.maxMoves = 15;
                    break;
            }
            return so;
        }
    }
}
