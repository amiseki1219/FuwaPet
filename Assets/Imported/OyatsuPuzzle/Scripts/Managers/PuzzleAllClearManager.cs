using UnityEngine;

namespace OyatsuPuzzle
{
    public static class PuzzleAllClearManager
    {
        private const string KeyAllClearDate = "OyatsuPuzzle_AllClearDate";

        public static bool IsAllClearedToday
        {
            get
            {
                string today = System.DateTime.Today.ToString("yyyy-MM-dd");
                return PlayerPrefs.GetString(KeyAllClearDate, "") == today;
            }
        }

        public static void MarkAllClearedToday()
        {
            string today = System.DateTime.Today.ToString("yyyy-MM-dd");
            PlayerPrefs.SetString(KeyAllClearDate, today);
            PlayerPrefs.Save();
            Debug.Log("[OyatsuPuzzle] All stages cleared today.");
            Debug.Log("[OyatsuPuzzle] AllClearToday saved: true");
        }

        public static void ResetAllClear()
        {
            PlayerPrefs.DeleteKey(KeyAllClearDate);
            PlayerPrefs.Save();
            Debug.Log("[OyatsuPuzzle] AllClearToday reset to false.");
        }
    }
}
