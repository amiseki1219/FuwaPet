namespace Game.Care
{
    public static class CareConfig
    {
        public const int COIN_REWARD = 5;

        // ご飯
        public const int EAT_HUNGER = +25;
        public const int EAT_MOOD = +2;
        public const int EAT_TRUST_FIRST = +1;

        // 撫でる
        public const int PET_MOOD = +6;
        public const int PET_TRUST_FIRST = +3;

        // 遊ぶ
        public const int PLAY_HUNGER = -5;
        public const int PLAY_MOOD = +12;
        public const int PLAY_TRUST_FIRST = +2;

        // お風呂（1日1回）
        public const int BATH_MOOD = +15;
        public const int BATH_TRUST_FIRST = +2;
        public const int BATH_COIN = 15; // 表の +15 に合わせるならこっち
    }
}
