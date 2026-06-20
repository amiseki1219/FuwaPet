using UnityEngine;

namespace OyatsuPuzzle
{
    // Tracks whether a puzzle session was in progress when the app quit/suspended.
    // Used to detect mid-session exits on restart and return safely to StartPanel.
    public static class PuzzleSessionStateManager
    {
        private const string KeyInSession   = "OyatsuPuzzle_InSession";
        private const string KeyActiveStage = "OyatsuPuzzle_ActiveSessionStage";

        public static bool IsInSession
            => PlayerPrefs.GetInt(KeyInSession, 0) == 1;

        public static int ActiveSessionStage
            => PlayerPrefs.GetInt(KeyActiveStage, 1);

        public static void MarkSessionStarted(int stage)
        {
            PlayerPrefs.SetInt(KeyInSession,   1);
            PlayerPrefs.SetInt(KeyActiveStage, stage);
            PlayerPrefs.Save();
            Debug.Log($"[OyatsuPuzzle] Puzzle session started. stage={stage}");
        }

        public static void MarkSessionCompleted()
        {
            PlayerPrefs.SetInt(KeyInSession, 0);
            PlayerPrefs.Save();
            Debug.Log("[OyatsuPuzzle] Puzzle session completed.");
        }

        // Call on StartPanel show. Returns true if an interrupted session is detected.
        public static bool CheckAndClearInterrupted()
        {
            if (!IsInSession) return false;
            Debug.Log("[OyatsuPuzzle] Interrupted session detected. Returning to StartPanel.");
            Debug.Log("[OyatsuPuzzle] Play count is not restored.");
            PlayerPrefs.SetInt(KeyInSession, 0);
            PlayerPrefs.Save();
            return true;
        }

        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(KeyInSession);
            PlayerPrefs.DeleteKey(KeyActiveStage);
            PlayerPrefs.Save();
        }
    }
}
