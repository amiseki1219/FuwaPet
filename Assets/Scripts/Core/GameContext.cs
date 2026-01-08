using UnityEngine;
using Game.Pet;

namespace Game.Core
{
    public class GameContext : MonoBehaviour
    {
        public static GameContext Instance { get; private set; }

        // 最初から中身を作っておくことで、他が読み取るときに「空っぽ」になるのを防ぐよ
        public PetStatus PetStatus { get; set; } = new PetStatus();
        public DailyCareTracker DailyTracker { get; private set; } = new DailyCareTracker();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            PetStatus = new PetStatus();
            DontDestroyOnLoad(gameObject);

            // 起動時に初期化を済ませる
            DailyTracker.InitIfEmpty();
        }

        private void Update()
        {
            if (PetStatus == null) return;
            PetStatus.Tick(Time.deltaTime);
            DailyTracker.CheckDayRolloverAndApplyPenalty(PetStatus);
        }
    }
}