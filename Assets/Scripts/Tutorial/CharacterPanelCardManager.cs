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
            SaveManager.Instance.Data.selectedCharacterId = characterIds[currentIndex];
            SaveManager.Instance.Data.iconId = characterIds[currentIndex];
            SaveManager.Instance.Save();
        }

        if (onboardingManager != null)
            onboardingManager.Next();
    }
}
