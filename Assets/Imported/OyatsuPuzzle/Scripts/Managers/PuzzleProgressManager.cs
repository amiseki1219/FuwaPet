using UnityEngine;

namespace OyatsuPuzzle
{
    // 現在ステージ番号を PlayerPrefs で永続化する
    public static class PuzzleProgressManager
    {
        private const string KeyStage = "OyatsuPuzzle_Stage";

        public static int CurrentStage
        {
            get => PlayerPrefs.GetInt(KeyStage, 1);
            private set
            {
                PlayerPrefs.SetInt(KeyStage, value);
                PlayerPrefs.Save();
            }
        }

        public static void AdvanceStage()
        {
            int next = Mathf.Min(CurrentStage + 1, PuzzleStageRegistry.StageCount);
            CurrentStage = next;
            Debug.Log($"[OyatsuPuzzle] ステージ進行 -> Stage{next}");
        }

        // テスト用リセット
        public static void ResetProgress()
        {
            CurrentStage = 1;
        }

#if UNITY_EDITOR
        public static void DebugResetStage()
        {
            CurrentStage = 1;
            Debug.Log("[OyatsuPuzzle] Current stage reset to 1.");
        }

        public static void DebugSetStage(int stage)
        {
            stage = Mathf.Clamp(stage, 1, PuzzleStageRegistry.StageCount);
            CurrentStage = stage;
            Debug.Log($"[OyatsuPuzzle] Current stage set to {stage}.");
        }
#endif
    }
}
