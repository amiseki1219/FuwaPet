using UnityEngine;
using UnityEngine.UI;
using Game.Core;

[System.Serializable]
public class CharacterImageSetting
{
    public string characterId;
    public Texture2D image;
}

public class ProfileSelectionPanelManager : MonoBehaviour
{
    [SerializeField] private GameObject characterInputPanel;
    [SerializeField] private GameObject confirmCard;
    [SerializeField] private RawImage selectedCharacterImage;
    [SerializeField] private CharacterImageSetting[] characterImages;

    private void OnEnable()
    {
        UpdateCharacterImage();
        ShowCharacterInput();
    }

    private void UpdateCharacterImage()
    {
        if (selectedCharacterImage == null) return;
        var data = SaveManager.Instance.Data;
        foreach (var setting in characterImages)
        {
            if (setting.characterId == data.selectedCharacterId)
            {
                selectedCharacterImage.texture = setting.image;
                return;
            }
        }
    }

    public void ShowCharacterInput()
    {
        if (characterInputPanel != null) characterInputPanel.SetActive(true);
        if (confirmCard != null) confirmCard.SetActive(false);
    }

    public void ShowConfirmCard()
    {
        if (characterInputPanel != null) characterInputPanel.SetActive(false);
        if (confirmCard != null) confirmCard.SetActive(true);
    }
}
