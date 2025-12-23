using UnityEngine;
using Game.Pet;

namespace Game.Core
{
    public class GameContext : MonoBehaviour
    {
        public static GameContext Instance { get; private set; }

        public PetStatus PetStatus { get; private set; }
        public DailyState Daily { get; private set; } = new DailyState();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            PetStatus = new PetStatus();

            // ✅ 初回起動で「今日」を確定（初回ペナルティ誤爆防止）
            Daily.EnsureTodayAndCheckNewDay();
        }

        private void Update()
        {
            // ✅ 日付が変わった瞬間だけ処理
            if (Daily.EnsureTodayAndCheckNewDay())
            {
                // ✅ 前日のお世話回数が0なら trust -= 10
                if (Daily.careCountYesterday == 0)
                {
                    PetStatus.AddTrust(-10);
                }

                // ✅ trust <= -100 で保護状態
                if (PetStatus.Trust <= -100)
                {
                    PetStatus.IsProtected = true;
                }

                // ここで SaveManager があるなら Save も呼びたいけど、
                // 今は「機能そのまま」優先でOK。あとで統合でやる。
            }
        }
    }
}
