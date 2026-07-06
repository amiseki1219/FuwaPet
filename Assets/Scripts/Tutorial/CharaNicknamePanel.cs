using UnityEngine;
using TMPro;
using Game.Core;

public class CharaNicknamePanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private OnboardingManager onboardingManager;

    public void OnDecideClicked()
    {
        Debug.Log($"<color=lime>【CharaNicknamePanel】入力値={nicknameInput.text}</color>");
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.petNickname = nicknameInput.text;
            SaveManager.Instance.Save();
            Debug.Log($"<color=lime>【CharaNicknamePanel】保存後petNickname={SaveManager.Instance.Data.petNickname}</color>");
        }

        if (onboardingManager != null) { onboardingManager.Next(); }
        else { Debug.LogWarning("OnboardingManager が未設定です。"); }
    }

    public void OnSkipClicked()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.petNickname = "";
            SaveManager.Instance.Save();
        }

        if (onboardingManager != null) { onboardingManager.Next(); }
        else { Debug.LogWarning("OnboardingManager が未設定です。"); }
    }
}
