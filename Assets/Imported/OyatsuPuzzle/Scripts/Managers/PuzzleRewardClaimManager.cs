using UnityEngine;

namespace OyatsuPuzzle
{
    // Tracks whether each stage's reward has been granted today.
    // Prevents double-granting on ClearPanel re-open or To Puzzle Top navigation.
    public static class PuzzleRewardClaimManager
    {
        private const string KeyPrefix = "OyatsuPuzzle_RewardClaimed_Stage";

        public static bool IsClaimedToday(int stage)
        {
            // ★S-7（2026/8/30）：「今日」の基準を GameDate（JST 3:00）に統一した
            string today   = GameDate.Today();
            string stored  = PlayerPrefs.GetString(Key(stage), "");
            return stored == today;
        }

        // Returns true when the reward was actually granted (first claim).
        // Returns false when it was already claimed today (skips grant).
        public static bool TryClaim(int stage)
        {
            if (IsClaimedToday(stage))
            {
                Debug.Log($"[OyatsuPuzzle] Reward already claimed. stage={stage}");
                return false;
            }
            string today = GameDate.Today();   // ★S-7
            PlayerPrefs.SetString(Key(stage), today);
            PlayerPrefs.Save();
            Debug.Log($"[OyatsuPuzzle] Reward claim recorded. stage={stage}");
            return true;
        }

        public static void ResetAll()
        {
            for (int s = 1; s <= PuzzleStageRegistry.StageCount; s++)
                PlayerPrefs.DeleteKey(Key(s));
            PlayerPrefs.Save();
            Debug.Log("[OyatsuPuzzle] Claimed reward stages cleared.");
        }

        private static string Key(int stage) => $"{KeyPrefix}{stage}";
    }
}
