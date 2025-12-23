using System;

namespace Game.Core
{
    [Serializable]
    public class DailyLimits
    {
        public string dateKey; // yyyy-MM-dd

        public bool ateOnce;
        public bool petOnce;
        public bool playOnce;
        public bool bathDone; // 1日1回

        public void RefreshIfNewDay()
        {
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            if (dateKey == today) return;

            dateKey = today;
            ateOnce = false;
            petOnce = false;
            playOnce = false;
            bathDone = false;
        }
    }
}
