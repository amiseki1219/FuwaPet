using UnityEngine;
using UnityEngine.UI;

namespace OyatsuPuzzle
{
    // PuzzleStartPanel 専用の画像切り替え。
    // CharacterImage … SaveData の選択キャラに応じて切り替え
    // GoalImage      … 現在ステージ(1〜5)に応じて切り替え
    // 参照は全て [SerializeField]（Resources.Load は使わない）。null は Warning 程度に留める。
    public class PuzzleStartPanelImageController : MonoBehaviour
    {
        [Header("Target Images")]
        [SerializeField] private Image characterImage;
        [SerializeField] private Image goalImage;

        [Header("Character Sprites")]
        [SerializeField] private Sprite pokoPlay;
        [SerializeField] private Sprite kokoPlay;
        [SerializeField] private Sprite eruPlay;
        [SerializeField] private Sprite paruPlay;

        [Header("Stage Goal Sprites")]
        [SerializeField] private Sprite stage1Image;
        [SerializeField] private Sprite stage2Image;
        [SerializeField] private Sprite stage3Image;
        [SerializeField] private Sprite stage4Image;
        [SerializeField] private Sprite stage5Image;

        // 現在ステージと選択キャラの両方を反映する。
        public void Apply(int stage)
        {
            ApplyStage(stage);
            ApplyCharacterFromSaveData();
        }

        // 現在ステージに応じて GoalImage を切り替える。
        public void ApplyStage(int stage)
        {
            if (goalImage == null)
            {
                Debug.LogWarning("[PuzzleStartPanelImageController] goalImage が未バインドです。");
                return;
            }

            Sprite sprite = stage switch
            {
                1 => stage1Image,
                2 => stage2Image,
                3 => stage3Image,
                4 => stage4Image,
                5 => stage5Image,
                _ => stage1Image, // 1〜5以外は Stage1 にフォールバック
            };

            if (sprite == null)
            {
                Debug.LogWarning($"[PuzzleStartPanelImageController] Stage{stage} 用の Sprite が未バインドです。");
                return;
            }

            goalImage.sprite = sprite;
        }

        // SaveData の選択キャラIDに応じて CharacterImage を切り替える。
        public void ApplyCharacterFromSaveData()
        {
            if (characterImage == null)
            {
                Debug.LogWarning("[PuzzleStartPanelImageController] characterImage が未バインドです。");
                return;
            }

            string id = null;
            if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
                id = SaveManager.Instance.Data.selectedCharacterId;

            Sprite sprite = id switch
            {
                "poko" => pokoPlay,
                "koko" => kokoPlay,
                "eru"  => eruPlay,
                "paru" => paruPlay,
                _       => pokoPlay, // null / 空 / 未対応IDは PokoPlay にフォールバック
            };

            if (sprite == null)
            {
                Debug.LogWarning($"[PuzzleStartPanelImageController] キャラID '{id}' 用の Sprite が未バインドです。");
                return;
            }

            characterImage.sprite = sprite;
        }
    }
}
