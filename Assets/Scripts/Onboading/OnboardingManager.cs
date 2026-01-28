using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Core;

public class OnboardingManager : MonoBehaviour
{
    // ★ここが超重要！使う名前（ProfileImage, Confirm）をすべてここに登録するよ
    public enum OnboardingStep
    {
        Owner,
        ProfileImage,
        Character,
        Name,
        Confirm
    }

    [SerializeField] private OwnerPanel ownerPanel;
    // ★ここを「GameObject」にすると、SetActiveのエラーが消えるよ！
    [SerializeField] private GameObject profileSelectionPanel;
    [SerializeField] private CharacterPanelLite characterPanel;
    [SerializeField] private NamePanel namePanel;
    [SerializeField] private ConfirmPanel confirmPanel;

    private OnboardingStep currentStep = OnboardingStep.Owner;

    private void Start()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.Data.onboardingCompleted)
        {
            SceneManager.LoadScene("Home");
            return;
        }
        UpdateView();
    }

    public void Next()
    {
        // ★Confirm（最後）まで進めるように設定
        if (currentStep < OnboardingStep.Confirm)
        {
            currentStep++;
            UpdateView();
        }
        else
        {
            CompleteOnboarding();
        }
    }

    private void UpdateView()
    {
        // 今のステップに合わせて、表示するパネルを切り替えるよ
        if (ownerPanel != null) ownerPanel.gameObject.SetActive(currentStep == OnboardingStep.Owner);
        if (profileSelectionPanel != null) profileSelectionPanel.SetActive(currentStep == OnboardingStep.ProfileImage);
        if (characterPanel != null) characterPanel.gameObject.SetActive(currentStep == OnboardingStep.Character);
        if (namePanel != null) namePanel.gameObject.SetActive(currentStep == OnboardingStep.Name);

        if (confirmPanel != null)
        {
            if (currentStep == OnboardingStep.Confirm)
            {
                confirmPanel.gameObject.SetActive(true);
                // 保存されている名前を使って確認画面を開くよ
                confirmPanel.Open(SaveManager.Instance.Data.petName);
            }
            else
            {
                confirmPanel.gameObject.SetActive(false);
            }
        }
    }

    private void CompleteOnboarding()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.onboardingCompleted = true;
            SaveManager.Instance.Save();
        }
        SceneManager.LoadScene("Care");
    }
}