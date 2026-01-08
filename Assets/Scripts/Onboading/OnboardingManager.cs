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
        // SaveManagerから「オンボーディングが終わってるか」を確認
        if (SaveManager.Instance != null && SaveManager.Instance.Data.onboardingCompleted)
        {
            // 2回目以降の人はHOME画面へ！
            UnityEngine.SceneManagement.SceneManager.LoadScene("Home");
            return;
        }

        // 初回の人はそのままチュートリアルの最初のパネル（OwnerPanel）を表示
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