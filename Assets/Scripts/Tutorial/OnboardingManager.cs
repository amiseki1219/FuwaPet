using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Core;
using Game.Onboarding;

public class OnboardingManager : MonoBehaviour
{
    [Header("Onboarding Panels (New Flow)")]
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject aiConsentPanel;
    // ★ 型を GameObject に変えたお！これで「青い立方体」を紐付けられるっぴ
    [SerializeField] private GameObject characterPanel;
    [SerializeField] private GameObject profileEditPanel;

    private OnboardingStep currentStep = OnboardingStep.Home;

    private void Start()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.Data.onboardingCompleted)
        {
            Debug.Log("オンボーディング完了済みだっぴ！CareSceneへ移動するお。");
            SceneManager.LoadScene("Care");
            return;
        }

        UpdateView();
    }

    public void Next()
    {
        if (currentStep < OnboardingStep.ProfileEdit)
        {
            currentStep++;
            Debug.Log($"<color=cyan>次へ移動！ 現在のステップ: {currentStep}</color>");
            UpdateView();
        }
        else
        {
            CompleteOnboarding();
        }
    }

    private void UpdateView()
    {
        // 全パネルを今のステップに合わせてオンオフするお！
        if (homePanel != null)
            homePanel.SetActive(currentStep == OnboardingStep.Home);

        if (aiConsentPanel != null)
            aiConsentPanel.SetActive(currentStep == OnboardingStep.AIConsent);

        if (characterPanel != null)
            characterPanel.SetActive(currentStep == OnboardingStep.Character);

        if (profileEditPanel != null)
            profileEditPanel.SetActive(currentStep == OnboardingStep.ProfileEdit);
    }

    public void CompleteOnboarding()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.onboardingCompleted = true;
            SaveManager.Instance.Save();
        }
        SceneManager.LoadScene("Care");
    }
}