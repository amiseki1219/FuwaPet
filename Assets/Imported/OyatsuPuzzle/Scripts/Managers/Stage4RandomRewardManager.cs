using UnityEngine;
using Random = UnityEngine.Random;

namespace OyatsuPuzzle
{
    // Rolls and persists Stage4's random reward once per day.
    // ClearPanel re-opens or To Puzzle Top never re-roll.
    public static class Stage4RandomRewardManager
    {
        private const string KeyDecidedDate = "OyatsuPuzzle_Stage4RewardDate";
        private const string KeyPiece       = "OyatsuPuzzle_Stage4RewardPiece";

        public static bool IsDecidedToday
        {
            get
            {
                string today = System.DateTime.Today.ToString("yyyy-MM-dd");
                return PlayerPrefs.GetString(KeyDecidedDate, "") == today;
            }
        }

        // Returns the decided piece name (roll if not yet decided today).
        public static string GetOrRollRewardText()
        {
            if (IsDecidedToday)
            {
                string saved = PlayerPrefs.GetString(KeyPiece, "Niboshi x1");
                Debug.Log($"[OyatsuPuzzle] Stage4 random reward already decided: {saved}");
                return $"Random Reward: {saved}";
            }
            return RollAndSave();
        }

        private static string RollAndSave()
        {
            float roll = Random.value;
            PieceType piece;
            if (roll < 0.5f)
                piece = PieceType.Niboshi;
            else if (roll < 0.7f)
                piece = PieceType.Biscuit;
            else if (roll < 0.9f)
                piece = PieceType.CarrotStick;
            else
                piece = Random.value < 0.5f ? PieceType.StrawberryCake : PieceType.Pudding;

            string text  = $"{piece.ToEnglishName()} x1";
            string today = System.DateTime.Today.ToString("yyyy-MM-dd");
            PlayerPrefs.SetString(KeyDecidedDate, today);
            PlayerPrefs.SetString(KeyPiece,       text);
            PlayerPrefs.Save();

            Debug.Log($"[OyatsuPuzzle] Stage4 random reward rolled: {text}");
            return $"Random Reward: {text}";
        }

        public static void ResetToday()
        {
            PlayerPrefs.DeleteKey(KeyDecidedDate);
            PlayerPrefs.DeleteKey(KeyPiece);
            PlayerPrefs.Save();
            Debug.Log("[OyatsuPuzzle] Stage4 random reward cleared by daily reset.");
        }
    }
}
