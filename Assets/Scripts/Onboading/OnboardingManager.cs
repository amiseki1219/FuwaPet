using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Core;
using Game.Onboarding;

public class OnboardingManager : MonoBehaviour
{
    [SerializeField] private GameObject ownerPanel;
    [SerializeField] private GameObject characterPanel;
    [SerializeField] private GameObject colorPanel;
    [SerializeField] private GameObject namePanel;

    private OnboardingStep currentStep;

    private void Start()
    {
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
            // ✅ オンボーディング完了処理はここ
            SaveManager.Instance.Data.onboardingCompleted = true;
            SaveManager.Instance.Save();
            SceneManager.LoadScene("Home");

        }
    }

    private void UpdateView()
    {
        ownerPanel.SetActive(currentStep == OnboardingStep.Owner);
        characterPanel.SetActive(currentStep == OnboardingStep.Character);
        colorPanel.SetActive(currentStep == OnboardingStep.Color);
        namePanel.SetActive(currentStep == OnboardingStep.Name);
    }
}
