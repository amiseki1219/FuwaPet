using System;
using UnityEngine;

namespace Game.Core
{
    [Serializable]
    public class PetStatus
    {
        // ─── パラメーター ───────────────────────────
        public float Hunger { get; private set; } = 50f;
        public float Clean { get; private set; } = 50f;
        public float Energy { get; private set; } = 50f;
        public float Mood { get; private set; } = 50f;
        public int Trust { get; private set; } = 0;

        // ─── 最終お世話時刻 ─────────────────────────
        public DateTime LastFedTime { get; private set; } = DateTime.Now;
        public DateTime LastBathTime { get; private set; } = DateTime.Now;
        public DateTime LastPlayTime { get; private set; } = DateTime.Now;

        // ─── SaveDataから初期値を読み込む ────────────
        public void LoadFromSave(SaveData save)
        {
            Hunger = save.hunger;
            Clean = save.clean;
            Energy = save.energy;
            Mood = save.mood;
            Trust = save.trust;
        }

        // ─── SaveDataに書き込む ──────────────────────
        public void SaveToSave(SaveData save)
        {
            save.hunger = Hunger;
            save.clean = Clean;
            save.energy = Energy;
            save.mood = Mood;
            save.trust = Trust;
        }

        // ─── 加算メソッド ────────────────────────────
        public void AddHunger(float v) => Hunger = Mathf.Clamp(Hunger + v, 0f, 100f);
        public void AddClean(float v) => Clean = Mathf.Clamp(Clean + v, 0f, 100f);
        public void AddEnergy(float v) => Energy = Mathf.Clamp(Energy + v, 0f, 100f);
        public void AddMood(float v) => Mood = Mathf.Clamp(Mood + v, 0f, 100f);
        public void AddTrust(int v) => Trust = Mathf.Max(0, Trust + v);

        // ─── お世話時刻の更新 ────────────────────────
        public void OnFed() => LastFedTime = DateTime.Now;
        public void OnBath() => LastBathTime = DateTime.Now;
        public void OnPlayed() => LastPlayTime = DateTime.Now;

        // ─── 時間経過ペナルティ ──────────────────────
        public void ApplyTimeDecay()
        {
            DateTime now = DateTime.Now;

            double hoursSinceFed = (now - LastFedTime).TotalHours;
            double hoursSincePlay = (now - LastPlayTime).TotalHours;
            double hoursSinceBath = (now - LastBathTime).TotalHours;

            AddHunger(-Mathf.Floor((float)hoursSinceFed) * 2f);
            AddEnergy(-Mathf.Floor((float)hoursSincePlay) * 1f);
            AddClean(-Mathf.Floor((float)(hoursSinceBath / 3f)) * 1f);

            float avg = (Hunger + Clean + Energy) / 3f;
            if (avg < 30f) AddMood(-5f);
            else if (avg < 40f) AddMood(-2f);

            Debug.Log($"[PetStatus] TimeDecay適用 Hunger:{Hunger} Clean:{Clean} Energy:{Energy} Mood:{Mood}");
        }

        // ─── 信頼度レベル計算 ────────────────────────
        public static int GetTrustLevel(int trust)
        {
            if (trust < 100) return 1;
            if (trust < 400) return 2;
            if (trust < 1400) return 3;
            return 4 + (trust - 1400) / 2000;
        }

        public static float GetTrustFillAmount(int trust)
        {
            if (trust < 100) return trust / 100f;
            if (trust < 400) return (trust - 100) / 300f;
            if (trust < 1400) return (trust - 400) / 1000f;
            return ((trust - 1400) % 2000) / 2000f;
        }
    }
}