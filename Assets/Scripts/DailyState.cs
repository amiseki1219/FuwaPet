using System;

namespace Game.Core
{
    [Serializable]
    public class DailyState
    {
        public string dateKey;          // 今日の日付 yyyy-MM-dd
        public int careCountToday;      // 今日のお世話回数
        public int bathCount;           // 今日のお風呂回数
        public int careCountYesterday;

        // 日付が変わったら true
        public bool EnsureTodayAndCheckNewDay()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (dateKey == today) return false;

            // 昨日の結果を確定
            careCountYesterday = careCountToday;

            dateKey = today;
            careCountToday = 0;
            bathCount = 0;
            return true;
        }


        public void OnCare()
        {
            EnsureTodayAndCheckNewDay();
            careCountToday++;
        }

        public bool CanBath()
        {
            return bathCount < 1;
        }

        public void MarkBath()
        {
            bathCount++;
        }
    }
}
