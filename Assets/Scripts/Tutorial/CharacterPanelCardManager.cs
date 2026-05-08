using UnityEngine;
using Game.Core;

public class CharacterPanelCardManager : MonoBehaviour
{
    [SerializeField] private GameObject[] characterPanels;
    [SerializeField] private string[] characterIds;
    [SerializeField] private OnboardingManager onboardingManager;

    private int currentIndex = 0;

    private void OnEnable()
    {
        currentIndex = 0;
        ShowCurrentPanel();
    }

    private void ShowCurrentPanel()
    {
        for (int i = 0; i < characterPanels.Length; i++)
        {
            if (characterPanels[i] != null)
                characterPanels[i].SetActive(i == currentIndex);
        }
    }

    public void OnNextClicked()
    {
        currentIndex = (currentIndex + 1) % characterPanels.Length;
        ShowCurrentPanel();
    }

    public void OnPrevClicked()
    {
        currentIndex = (currentIndex - 1 + characterPanels.Length) % characterPanels.Length;
        ShowCurrentPanel();
    }

    public void OnDecideClicked()
    {
        if (SaveManager.Instance != null && characterIds.Length > currentIndex)
        {
            var data = SaveManager.Instance.Data;
            data.selectedCharacterId = characterIds[currentIndex];
            data.iconId = characterIds[currentIndex];
            SetInitialPersonality(data, characterIds[currentIndex]);
            SaveManager.Instance.Save();
        }

        if (onboardingManager != null)
            onboardingManager.Next();
    }

    private void SetInitialPersonality(SaveData data, string characterId)
    {
        switch (characterId)
        {
            case "poko":
                data.personalityActivity    =  60;
                data.personalityDependency  =  70;
                data.personalityDiligence   =   0;
                data.personalityHonesty     =  80;
                data.personalitySensitivity =  40;
                break;
            case "eru":
                data.personalityActivity    = -40;
                data.personalityDependency  = -50;
                data.personalityDiligence   =  20;
                data.personalityHonesty     = -30;
                data.personalitySensitivity = -60;
                break;
            case "koko":
                data.personalityActivity    = -20;
                data.personalityDependency  =  40;
                data.personalityDiligence   = -20;
                data.personalityHonesty     =  60;
                data.personalitySensitivity =  70;
                break;
            case "paru":
                data.personalityActivity    =  30;
                data.personalityDependency  = -60;
                data.personalityDiligence   =  10;
                data.personalityHonesty     = -80;
                data.personalitySensitivity =  20;
                break;
            default:
                Debug.LogWarning($"[CharacterPanelCardManager] 未定義のキャラID: {characterId}");
                break;
        }
    }
}
