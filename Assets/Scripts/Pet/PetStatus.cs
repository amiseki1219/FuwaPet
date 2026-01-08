using System;
using UnityEngine;

namespace Game.Pet
{
    [Serializable]
    public class PetStatus
    {
        public int Trust;          // 合計経験値（これを元にレベルを計算するよ）
        public float Hunger;       // 0-100
        public float Mood;         // 0-100
        public bool IsProtected;

        // 減少スピードの設定
        public float HungerDecayPerSec = 0.01f;
        public float MoodDecayPerSec = 0.006f;

        public PetStatus(int trust = 0, float hunger = 50f, float mood = 50f)
        {
            Trust = trust;
            Hunger = hunger;
            Mood = mood;
            IsProtected = false;
        }

        // ★あみまる専用：特殊なレベル計算ロジック
        public int Level
        {
            get
            {
                // 信頼度100ポイント（Lv.11になる直前）までは10刻み
                if (Trust <= 100)
                {
                    return (Trust / 10) + 1;
                }
                else
                {
                    // 100ポイントを超えたら、そこからは30刻みでレベルアップ！
                    int extraTrust = Trust - 100;
                    return 11 + (extraTrust / 30);
                }
            }
        }

        // 時間経過で減る処理（Trustは勝手に上げないよ！）
        public void Tick(float deltaTime)
        {
            Hunger = Mathf.Clamp(Hunger - HungerDecayPerSec * deltaTime, 0f, 100f);
            Mood = Mathf.Clamp(Mood - MoodDecayPerSec * deltaTime, 0f, 100f);
        }

        // --- 各ステータスを操作する関数（Trustは別々に管理！） ---

        // 信頼度を上げる（ご飯をあげた時などに呼んでね）
        public void AddTrust(int value)
        {
            Trust += value;
        }

        // 満腹度を上げる
        public void AddHunger(float value)
        {
            Hunger = Mathf.Clamp(Hunger + value, 0f, 100f);
        }

        // 機嫌度を上げる
        public void AddMood(float value)
        {
            Mood = Mathf.Clamp(Mood + value, 0f, 100f);
        }
    }
}