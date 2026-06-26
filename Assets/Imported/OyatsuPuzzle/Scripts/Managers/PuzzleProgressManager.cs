using UnityEngine;

namespace OyatsuPuzzle
{
    // 現在ステージ番号を PlayerPrefs で永続化する
    public static class PuzzleProgressManager
    {
        private const string KeyStage    = "OyatsuPuzzle_Stage";
        private const string KeyAllClear = "OyatsuPuzzle_AllClear";

        public static int CurrentStage
        {
            get => PlayerPrefs.GetInt(KeyStage, 1);
            private set
            {
                PlayerPrefs.SetInt(KeyStage, value);
                PlayerPrefs.Save();
            }
        }

        // 全ステージクリア済みか（永続）。CurrentStage は StageCount で頭打ちのため、
        // 「最終ステージをこれから遊ぶ(=5)」と「最終ステージもクリア済み(=5)」を CurrentStage では
        // 区別できない。そのため最終ステージクリア時にこのフラグを立てて区別する。
        public static bool IsAllCleared
        {
            get => PlayerPrefs.GetInt(KeyAllClear, 0) == 1;
            private set
            {
                PlayerPrefs.SetInt(KeyAllClear, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static void AdvanceStage()
        {
            // 最終ステージをクリアした場合：CurrentStage は据え置き（頭打ち）、全クリアフラグを立てる。
            if (CurrentStage >= PuzzleStageRegistry.StageCount)
            {
                IsAllCleared = true;
                Debug.Log("[OyatsuPuzzle] 全ステージクリア -> AllCleared = true");
                return;
            }

            CurrentStage = CurrentStage + 1;
            Debug.Log($"[OyatsuPuzzle] ステージ進行 -> Stage{CurrentStage}");
        }

        // テスト用リセット
        public static void ResetProgress()
        {
            CurrentStage = 1;
            IsAllCleared = false;
        }

#if UNITY_EDITOR
        public static void DebugResetStage()
        {
            CurrentStage = 1;
            IsAllCleared = false;
            Debug.Log("[OyatsuPuzzle] Current stage reset to 1.");
        }

        public static void DebugSetStage(int stage)
        {
            stage = Mathf.Clamp(stage, 1, PuzzleStageRegistry.StageCount);
            CurrentStage = stage;
            IsAllCleared = false;
            Debug.Log($"[OyatsuPuzzle] Current stage set to {stage}.");
        }
#endif
    }
}
