using System;
using UnityEngine;

namespace OyatsuPuzzle
{
    // Checks on startup / StartPanel refresh whether the date has changed.
    // On a new day: resets plays, stage, AllClear flag, reward claims, and session state.
    public static class PuzzleDailyResetManager
    {
        private const string KeyLastDate = "OyatsuPuzzle_LastPuzzlePlayDate";

        public static void CheckDailyReset(PuzzleManager puzzleManager)
        {
            Debug.Log("[OyatsuPuzzle] Daily reset check.");
            string today    = DateTime.Today.ToString("yyyy-MM-dd");
            string lastDate = PlayerPrefs.GetString(KeyLastDate, "");

            if (lastDate == today)
            {
                Debug.Log("[OyatsuPuzzle] Same day. Puzzle state kept.");
                return;
            }

            Debug.Log($"[OyatsuPuzzle] Last date={lastDate} Today={today}");
            Debug.Log("[OyatsuPuzzle] New day detected. Puzzle state reset.");

            PlayerPrefs.SetString(KeyLastDate, today);
            PlayerPrefs.Save();

            // Reset plays via PlayerPrefs keys that PuzzleDailyPlayManager uses
            PlayerPrefs.SetString("OyatsuPuzzle_Date",      today);
            PlayerPrefs.SetInt(   "OyatsuPuzzle_PlayCount", 0);
            PlayerPrefs.Save();

            // Reset stage
            PuzzleProgressManager.ResetProgress();

            // Reset AllClear
            PuzzleAllClearManager.ResetAllClear();

            // Reset reward claims
            PuzzleRewardClaimManager.ResetAll();

            // Reset session state
            PuzzleSessionStateManager.ResetAll();

            // Reset Stage4 random reward
            Stage4RandomRewardManager.ResetToday();

            // Discard any in-memory session
            puzzleManager?.ClearSession();
        }
    }
}
