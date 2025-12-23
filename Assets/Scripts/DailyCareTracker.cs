using System;
using UnityEngine;
using Game.Pet;

namespace Game.Core
{
    [Serializable]
    public class DailyCareTracker
    {
        // 今日のキー
        public string todayKey; // yyyy-MM-dd

        // 昨日分のケア回数（今日に切り替わる直前の値）
        public int careCountToday;

        // ペナルティ設定
        public int penaltyIfZeroCare = 10;

        string GetTodayKey() => DateTime.Now.ToString("yyyy-MM-dd");

        public void InitIfEmpty()
        {
            if (!string.IsNullOrEmpty(todayKey)) return;
            todayKey = GetTodayKey();
            careCountToday = 0;
        }

        /// <summary>
        /// 日付が変わってたら、前日ケア0回なら trust を下げる → 今日用にリセット
        /// </summary>
        public void CheckDayRolloverAndApplyPenalty(PetStatus pet)
        {
            InitIfEmpty();

            var current = GetTodayKey();
            if (todayKey == current) return;

            // 日付が変わった瞬間：前日（= careCountToday）を評価
            if (careCountToday == 0)
            {
                pet.AddTrust(-penaltyIfZeroCare);
            }

            // 今日に更新してリセット
            todayKey = current;
            careCountToday = 0;
        }

        public void OnCareSuccess()
        {
            InitIfEmpty();
            careCountToday++;
        }
    }
}
