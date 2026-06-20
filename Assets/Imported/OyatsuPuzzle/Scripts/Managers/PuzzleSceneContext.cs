using UnityEngine;

namespace OyatsuPuzzle
{
    // シーン間でセッション状態を共有するシングルトン（DontDestroyOnLoad）
    public class PuzzleSceneContext : MonoBehaviour
    {
        public static PuzzleSceneContext Instance { get; private set; }

        public PuzzleSession CurrentSession { get; private set; }
        public string LastRewardText { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void StartNewSession(int stageNumber)
        {
            var data = PuzzleStageRegistry.GetStage(stageNumber);
            CurrentSession = new PuzzleSession(data);
            Debug.Log($"[OyatsuPuzzle] セッション開始 Stage{stageNumber} ({data.boardSize}x{data.boardSize}) 手数:{data.maxMoves}");
        }

        public void SetLastReward(string rewardText)
        {
            LastRewardText = rewardText;
        }
    }
}
