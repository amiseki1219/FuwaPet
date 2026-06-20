using System;
using UnityEngine;

namespace OyatsuPuzzle
{
    // 今日のプレイ回数を管理するMonoBehaviour（移植時にSaveManagerへ接続）
    public class PuzzleDailyPlayManager : MonoBehaviour
    {
        [SerializeField] private int maxPlaysPerDay = 5;

        private const string KeyDate  = "OyatsuPuzzle_Date";
        private const string KeyCount = "OyatsuPuzzle_PlayCount";

        public int MaxPlays => maxPlaysPerDay;

        public int RemainingPlays
        {
            get
            {
                RefreshIfNewDay();
                return Mathf.Max(0, maxPlaysPerDay - PlayerPrefs.GetInt(KeyCount, 0));
            }
        }

        public bool CanPlay() => RemainingPlays > 0;

        public bool ConsumePlay()
        {
            RefreshIfNewDay();
            int used = PlayerPrefs.GetInt(KeyCount, 0);
            if (used >= maxPlaysPerDay) return false;
            PlayerPrefs.SetInt(KeyCount, used + 1);
            PlayerPrefs.Save();
            Debug.Log($"[OyatsuPuzzle] プレイ回数消費。残り: {RemainingPlays}");
            return true;
        }

        private void RefreshIfNewDay()
        {
            string today = DateTime.Today.ToString("yyyy-MM-dd");
            if (PlayerPrefs.GetString(KeyDate, "") != today)
            {
                PlayerPrefs.SetString(KeyDate, today);
                PlayerPrefs.SetInt(KeyCount, 0);
                PlayerPrefs.Save();
            }
        }

        public void DebugResetPlays()
        {
            string today = DateTime.Today.ToString("yyyy-MM-dd");
            PlayerPrefs.SetString(KeyDate, today);
            PlayerPrefs.SetInt(KeyCount, 0);
            PlayerPrefs.Save();
            Debug.Log($"[OyatsuPuzzle] Daily plays reset to {maxPlaysPerDay} / {maxPlaysPerDay}.");
        }
    }
}
