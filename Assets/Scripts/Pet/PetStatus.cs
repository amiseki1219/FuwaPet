using System;
using UnityEngine;

namespace Game.Core
{
    [Serializable]
    public class PetStatus
    {
        // ─── パラメーター ───────────────────────────
        public float Hunger { get; private set; } = 50f;
        public float Clean  { get; private set; } = 50f;
        public float Energy { get; private set; } = 50f;
        public float Mood   => Mathf.Clamp((Hunger + Clean + Energy) / 3f, 10f, 100f);
        public int   Trust  { get; private set; } = 0;

        // ─── 最終お世話時刻 ─────────────────────────
        public DateTime LastFedTime   { get; private set; } = DateTime.Now;
        public DateTime LastBathTime  { get; private set; } = DateTime.Now;
        public DateTime LastPlayTime  { get; private set; } = DateTime.Now;

        // ─── SaveDataから初期値を読み込む ────────────
        public void LoadFromSave(SaveData save)
        {
            Hunger = save.hunger;
            Clean  = save.clean;
            Energy = save.energy;
            Trust  = save.trust;
        }

        // ─── SaveDataに書き込む ──────────────────────
        public void SaveToSave(SaveData save)
        {
            save.hunger = Hunger;
            save.clean  = Clean;
            save.energy = Energy;
            save.trust  = Trust;
        }

        // ─── 加算メソッド ────────────────────────────
        public void AddHunger(float v) => Hunger = Mathf.Clamp(Hunger + v, 10f, 100f);
        public void AddClean(float v)  => Clean  = Mathf.Clamp(Clean  + v, 10f, 100f);
        public void AddEnergy(float v) => Energy = Mathf.Clamp(Energy + v, 10f, 100f);
        public void AddTrust(int v)    => Trust  = Mathf.Max(0, Trust + v);

        // ─── お世話時刻の更新 ────────────────────────
        public void OnFed()    => LastFedTime  = DateTime.Now;
        public void OnBath()   => LastBathTime = DateTime.Now;
        public void OnPlayed() => LastPlayTime = DateTime.Now;

        // ─── 時間経過ペナルティ ──────────────────────
        public void ApplyTimeDecay()
        {
            DateTime now = DateTime.Now;

            float hoursSinceFed  = (float)(now - LastFedTime).TotalHours;
            float hoursSinceBath = (float)(now - LastBathTime).TotalHours;
            float hoursSincePlay = (float)(now - LastPlayTime).TotalHours;

            AddHunger(-5f * hoursSinceFed);
            AddClean( -4f * hoursSinceBath);
            AddEnergy(-4f * hoursSincePlay);

            Debug.Log($"[PetStatus] TimeDecay適用 Hunger:{Hunger} Clean:{Clean} Energy:{Energy} Mood:{Mood}");
        }
    }
}
