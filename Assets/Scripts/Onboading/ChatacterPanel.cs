//キャラクター選択
using UnityEngine;
using UnityEngine.UI;
using Game.Core;

public class CharacterPanel : MonoBehaviour
{
    [SerializeField] private Button nextButton;

    private void Start()
    {
        // characterId が 0 なら未選択扱い
        nextButton.interactable = SaveManager.Instance.Data.characterId != 0;
    }

    // キャラボタンのOnClickに、intのIDを渡す
    public void SelectCharacter(int characterId)
    {
        SaveManager.Instance.Data.characterId = characterId;
        nextButton.interactable = true;
    }
}
