using UnityEngine;
using TMPro;
using Game.Core;
using System.Collections.Generic;

public class ConfirmPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI userNameText;
    [SerializeField] private TextMeshProUGUI charNameText;
    [SerializeField] private TextMeshProUGUI birthdayText;
    [SerializeField] private OnboardingManager onboardingManager;

    private void OnEnable()
    {
        OnShow();
    }

    public void OnShow()
    {
        var data = SaveManager.Instance.Data;

        // キャラクター名の確認
        var charNameMap = new Dictionary<string, string>
        {
            {"poko", "ぽこ"},
            {"eru", "える"},
            {"koko", "ここ"},
            {"paru", "ぱる"}
        };

        string charId = data.selectedCharacterId;
        string defaultCharName = charNameMap.ContainsKey(charId) ? charNameMap[charId] : charId;
        string nickname = data.petNickname;
        Debug.Log($"<color=cyan>【ConfirmPanel】selectedCharacterId={charId}</color>");
        Debug.Log($"<color=cyan>【ConfirmPanel】petNickname={nickname}</color>");

        string displayName = string.IsNullOrEmpty(nickname)
            ? defaultCharName
            : nickname;
        Debug.Log($"<color=lime>【ConfirmPanel】表示するキャラ名={displayName}</color>");

        // SaveData 全体の確認
        Debug.Log($"<color=yellow>【ConfirmPanel】userName={data.userName}</color>");
        Debug.Log($"<color=yellow>【ConfirmPanel】birthday={data.ownerBirthday}</color>");

        if (userNameText != null)
            userNameText.text = data.userName;

        if (charNameText != null)
            charNameText.text = displayName;

        if (birthdayText != null)
            birthdayText.text = data.ownerBirthday;
    }

    public void OnStartClicked()
    {
        if (onboardingManager != null) { onboardingManager.CompleteOnboarding(); }
        else { Debug.LogWarning("OnboardingManager が未設定です。"); }
    }

    public void OnBackClicked()
    {
        FindAnyObjectByType<ProfileSelectionPanelManager>()?.ShowCharacterInput();
    }
}
