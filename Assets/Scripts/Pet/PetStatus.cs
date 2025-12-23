using System;
using UnityEngine;

namespace Game.Pet
{
    [Serializable]
    public class PetStatus
    {
        public int Trust;          // 無限（上限なし）
        public float Hunger;       // 0-100
        public float Mood;         // 0-100
        public bool IsProtected;    // ゲームオーバー状態

        // 1秒あたり減る量（好きに調整してOK）
        public float HungerDecayPerSec = 0.01f; // 100→0 が約2.7時間
        public float MoodDecayPerSec = 0.006f;

        public PetStatus(int trust = 0, float hunger = 50f, float mood = 50f)
        {
            Trust = trust;
            Hunger = hunger;
            Mood = mood;
            IsProtected = false;
        }

        public void Tick(float deltaTime)
        {
            Hunger = Mathf.Clamp(Hunger - HungerDecayPerSec * deltaTime, 0f, 100f);
            Mood = Mathf.Clamp(Mood - MoodDecayPerSec * deltaTime, 0f, 100f);
        }

        public void AddTrust(int value)
        {
            Trust += value; // clampしない
        }


        public void AddHunger(float value)
        {
            Hunger = Mathf.Clamp(Hunger + value, 0f, 100f);
        }

        public void AddMood(float value)
        {
            Mood = Mathf.Clamp(Mood + value, 0f, 100f);
        }
    }
}
