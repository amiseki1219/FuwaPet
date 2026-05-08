using UnityEngine;

namespace Game.Core
{
    public class GameContext : MonoBehaviour
    {
        public static GameContext Instance { get; private set; }
        public PetStatus PetStatus { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            PetStatus = new PetStatus();

            // SaveManager.Awake()より後に実行されるよう Script Execution Order で保証すること
            if (SaveManager.Instance?.Data != null)
                PetStatus.LoadFromSave(SaveManager.Instance.Data);
        }

        // お世話後などに呼ぶ保存メソッド
        public void SavePetStatus()
        {
            if (SaveManager.Instance?.Data == null) return;
            PetStatus.SaveToSave(SaveManager.Instance.Data);
            SaveManager.Instance.Save();
        }
    }
}