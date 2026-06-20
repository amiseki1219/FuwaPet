using UnityEngine;
using Random = UnityEngine.Random;

namespace OyatsuPuzzle
{
    public static class RewardManager
    {
        // Call this at stage clear. stage number is used for double-grant prevention.
        public static string GiveReward(RewardData reward, int stage)
        {
            bool firstClaim = PuzzleRewardClaimManager.TryClaim(stage);

            string rewardText = RewardText(reward, stage);

            if (firstClaim)
                Debug.Log($"[OyatsuPuzzle] Reward granted: Stage {stage} {rewardText}");

            return rewardText;
        }

        // Always returns the display text without side effects (safe for ClearPanel re-display).
        public static string GetRewardDisplayText(RewardData reward, int stage)
            => RewardText(reward, stage);

        private static string RewardText(RewardData reward, int stage)
        {
            switch (reward.rewardType)
            {
                case RewardData.RewardType.FreeCoin:
                    return $"Free Coin +{reward.amount}";

                case RewardData.RewardType.Piece:
                    return $"{reward.pieceReward.ToEnglishName()} x{reward.amount}";

                case RewardData.RewardType.RandomPiece:
                    return Stage4RandomRewardManager.GetOrRollRewardText();

                case RewardData.RewardType.FreeCoinPlusTrust:
                    return $"Free Coin +{reward.amount}\nTrust +{reward.trustPoints}pt";

                default:
                    return "No Reward";
            }
        }
    }

    public static class PieceTypeExtensions
    {
        public static string ToEnglishName(this PieceType type) => type switch
        {
            PieceType.Niboshi        => "Niboshi",
            PieceType.Biscuit        => "Biscuit",
            PieceType.CarrotStick    => "Carrot Stick",
            PieceType.StrawberryCake => "Strawberry Cake",
            PieceType.Pudding        => "Pudding",
            PieceType.Coin           => "Coin",
            PieceType.StarCookie     => "Star Cookie",
            PieceType.HeartMacaron   => "Heart Macaron",
            _                        => "Unknown",
        };
    }
}
