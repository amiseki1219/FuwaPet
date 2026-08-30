using System;
using UnityEngine;

namespace OyatsuPuzzle
{
    // 今日のプレイ残り回数を PlayerPrefs で管理する（本体プロジェクト移植時に差し替え可）
    public static class PuzzlePlayCountManager
    {
        private const int MaxPlaysPerDay = 3;
        private const string KeyDate = "OyatsuPuzzle_Date";
        private const string KeyCount = "OyatsuPuzzle_PlayCount";

        public static int RemainingPlays
        {
            get
            {
                RefreshIfNewDay();
                return MaxPlaysPerDay - PlayerPrefs.GetInt(KeyCount, 0);
            }
        }

        // プレイ回数を1消費。残り0なら false を返す。
        public static bool TryConsume()
        {
            RefreshIfNewDay();
            int used = PlayerPrefs.GetInt(KeyCount, 0);
            if (used >= MaxPlaysPerDay) return false;
            PlayerPrefs.SetInt(KeyCount, used + 1);
            PlayerPrefs.Save();
            Debug.Log($"[OyatsuPuzzle] プレイ回数を消費。残り: {RemainingPlays}");
            return true;
        }

        private static void RefreshIfNewDay()
        {
            // ★S-7（2026/8/30）：「今日」の基準を GameDate（JST 3:00）に統一した
            string today = GameDate.Today();
            string saved = PlayerPrefs.GetString(KeyDate, "");
            if (saved != today)
            {
                PlayerPrefs.SetString(KeyDate, today);
                PlayerPrefs.SetInt(KeyCount, 0);
                PlayerPrefs.Save();
            }
        }
    }
}
