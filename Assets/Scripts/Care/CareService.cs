using UnityEngine;
using Game.Pet;
using Game.Core;

namespace Game.Care
{
    public class CareService
    {
        private readonly PetStatus pet;
        private readonly DailyLimits daily;

        // しきい値（好みで調整）
        private const int FULL_HUNGER = 100;     // 100以上なら「お腹いっぱい」
        private const int SATISFIED_MOOD = 100;  // 100以上なら「満足してる」

        public CareService(PetStatus petStatus, DailyLimits dailyLimits)
        {
            pet = petStatus;
            daily = dailyLimits;
        }

        public bool TryEat(out int coinReward, out string message)
        {
            coinReward = CareConfig.COIN_REWARD;

            if (pet.Hunger >= FULL_HUNGER)
            {
                message = "今はお腹いっぱい！";
                return false;
            }

            pet.AddHunger(CareConfig.EAT_HUNGER);
            pet.AddMood(CareConfig.EAT_MOOD);

            int trustAdd = CalcTrust("eat", ref daily.ateOnce, first: CareConfig.EAT_TRUST_FIRST, probPlus1: 0.40f);
            pet.AddTrust(trustAdd);

            message = $"ご飯！Coin+{coinReward} / Trust+{trustAdd} / Hunger+{CareConfig.EAT_HUNGER} / Mood+{CareConfig.EAT_MOOD}";
            return true;
        }

        public bool TryPet(out int coinReward, out string message)
        {
            coinReward = CareConfig.COIN_REWARD;

            if (pet.Mood >= SATISFIED_MOOD)
            {
                message = "今は気分じゃないみたい。";
                return false;
            }

            pet.AddMood(CareConfig.PET_MOOD);

            int trustAdd = CalcTrust("pet", ref daily.petOnce, first: CareConfig.PET_TRUST_FIRST, probPlus1: 0.30f);
            pet.AddTrust(trustAdd);

            message = $"なでなで！Coin+{coinReward} / Trust+{trustAdd} / Mood+{CareConfig.PET_MOOD}";
            return true;
        }

        public bool TryPlay(out int coinReward, out string message)
        {
            coinReward = CareConfig.COIN_REWARD;

            if (pet.Mood >= SATISFIED_MOOD)
            {
                message = "今はゆっくりしたい。";
                return false;
            }

            pet.AddHunger(CareConfig.PLAY_HUNGER);
            pet.AddMood(CareConfig.PLAY_MOOD);

            int trustAdd = CalcTrust("play", ref daily.playOnce, first: CareConfig.PLAY_TRUST_FIRST, probPlus1: 0.50f);
            pet.AddTrust(trustAdd);

            message = $"あそんだ！Coin+{coinReward} / Trust+{trustAdd} / Hunger{CareConfig.PLAY_HUNGER} / Mood+{CareConfig.PLAY_MOOD}";
            return true;
        }

        public bool TryBath(out int coinReward, out string message)
        {
            coinReward = CareConfig.BATH_COIN;

            if (daily.bathDone)
            {
                message = "今日はもうお風呂に入れられない！";
                return false;
            }

            daily.bathDone = true;

            pet.AddMood(CareConfig.BATH_MOOD);
            pet.AddTrust(CareConfig.BATH_TRUST_FIRST);

            message = $"お風呂！Coin+{coinReward} / Trust+{CareConfig.BATH_TRUST_FIRST} / Mood+{CareConfig.BATH_MOOD}";
            return true;
        }

        // 初回は固定、2回目以降は「+1 or +0」
        private int CalcTrust(string key, ref bool didOnce, int first, float probPlus1)
        {
            if (!didOnce)
            {
                didOnce = true;
                return first;
            }
            return (Random.value < probPlus1) ? 1 : 0;
        }
    }
}
