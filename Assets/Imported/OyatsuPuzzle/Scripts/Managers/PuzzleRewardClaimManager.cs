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
            string today   = System.DateTime.Today.ToString("yyyy-MM-dd");
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
            string today = System.DateTime.Today.ToString("yyyy-MM-dd");
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
