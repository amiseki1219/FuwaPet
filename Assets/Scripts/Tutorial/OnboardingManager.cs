using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Core;
using Game.Onboarding;

public class OnboardingManager : MonoBehaviour
{
    [Header("Onboarding Panels")]
    [SerializeField] private GameObject homePanel;             // ★追加：最初のタイトルパネル
    [SerializeField] private GameObject aiConsentPanel;        // AI同意画面
    [SerializeField] private OwnerPanel ownerPanel;             // 飼い主情報
    [SerializeField] private GameObject profileSelectionPanel;  // プロフ画像選択
    [SerializeField] private CharacterPanelLite characterPanel; // キャラ選択
    [SerializeField] private NamePanel namePanel;               // 名前入力
    [SerializeField] private ConfirmPanel confirmPanel;         // 最終確認

    // 最初のステップを Home に設定！
    private OnboardingStep currentStep = OnboardingStep.Home;

    private void Start()
    {
        // ★重要：2回目以降の自動ジャンプ
        // セーブデータを確認して、完了済みなら直接「Home」シーンへ飛ばすお！
        if (SaveManager.Instance != null && SaveManager.Instance.Data.onboardingCompleted)
        {
            Debug.Log("オンボーディング完了済みだっぴ！HomeSceneへ移動するお。");
            SceneManager.LoadScene("Home"); // ←ここがHomeSceneの名前だお
            return;
        }

        // 初回起動なら、最初のHomePanelを表示
        UpdateView();
    }

    public void Next()
    {
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
        // --- ここで各パネルの表示/非表示を切り替えるお ---

        // 0. HomePanel (★追加)
        if (homePanel != null)
            homePanel.SetActive(currentStep == OnboardingStep.Home);

        // 1. AI同意画面
        if (aiConsentPanel != null)
            aiConsentPanel.SetActive(currentStep == OnboardingStep.AIConsent);

        // 2. 飼い主パネル
        if (ownerPanel != null)
            ownerPanel.gameObject.SetActive(currentStep == OnboardingStep.Owner);

        // 3. プロフ画像選択
        if (profileSelectionPanel != null)
            profileSelectionPanel.SetActive(currentStep == OnboardingStep.ProfileImage);

        // 4. キャラ選択パネル
        if (characterPanel != null)
            characterPanel.gameObject.SetActive(currentStep == OnboardingStep.Character);

        // 5. 名前入力パネル
        if (namePanel != null)
            namePanel.gameObject.SetActive(currentStep == OnboardingStep.Name);

        // 6. 最終確認パネル
        if (confirmPanel != null)
        {
            bool isConfirm = (currentStep == OnboardingStep.Confirm);
            confirmPanel.gameObject.SetActive(isConfirm);
            if (isConfirm) confirmPanel.Open(SaveManager.Instance.Data.petName);
        }
    }

    private void CompleteOnboarding()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.onboardingCompleted = true;
            SaveManager.Instance.Save();
        }

        // 全て終わったらHomeへ！
        SceneManager.LoadScene("Home");
    }
}