using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Core;

public class OnboardingManager : MonoBehaviour
{
    public enum OnboardingStep { Owner, Character, Name }

    [SerializeField] private OwnerPanel ownerPanel;
    [SerializeField] private CharacterPanelLite characterPanel;
    [SerializeField] private NamePanel namePanel;

    private OnboardingStep currentStep;

    private void Start()
    {
        if (ownerPanel == null || characterPanel == null || namePanel == null) return;
        currentStep = OnboardingStep.Owner;
        UpdateView();
    }

    public void Next()
    {
        if (currentStep < OnboardingStep.Name)
        {
            currentStep++;
            UpdateView();
        }
        else
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Data.onboardingCompleted = true;
                SaveManager.Instance.Save();
            }
            SceneManager.LoadScene("Home");
        }
    }

    private void UpdateView()
    {
        if (ownerPanel != null) ownerPanel.gameObject.SetActive(currentStep == OnboardingStep.Owner);
        if (characterPanel != null) characterPanel.gameObject.SetActive(currentStep == OnboardingStep.Character);
        if (namePanel != null) namePanel.gameObject.SetActive(currentStep == OnboardingStep.Name);
    }
}