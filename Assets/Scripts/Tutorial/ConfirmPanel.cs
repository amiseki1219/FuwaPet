using UnityEngine;
using TMPro;
using Game.Core;
using System.Collections.Generic;

public class ConfirmPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI charNameText;
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
            {"paru", "ぱる"},
            {"piyoko", "ぴよこ"}
        };

        // selectedCharacterId 優先・空なら characterId にフォールバック（Main と揃える）
        string charId = !string.IsNullOrEmpty(data.selectedCharacterId)
            ? data.selectedCharacterId
            : data.characterId;
        string defaultCharName = charNameMap.ContainsKey(charId) ? charNameMap[charId] : charId;
        string nickname = data.petNickname;
        Debug.Log($"<color=cyan>【ConfirmPanel】selectedCharacterId={charId}</color>");
        Debug.Log($"<color=cyan>【ConfirmPanel】petNickname={nickname}</color>");

        string displayName = string.IsNullOrEmpty(nickname)
            ? defaultCharName
            : nickname;
        Debug.Log($"<color=lime>【ConfirmPanel】表示するキャラ名={displayName}</color>");

        // 挨拶文（キャラ名だけの上書きではなく文章全体を組み立てる）
        string greeting = $"今日から{displayName}との毎日がはじまるよ！";
        Debug.Log($"[Confirm] 挨拶文: {greeting}");

        // SaveData 全体の確認
        Debug.Log($"<color=yellow>【ConfirmPanel】userName={data.userName}</color>");
        Debug.Log($"<color=yellow>【ConfirmPanel】birthday={data.ownerBirthday}</color>");

        if (charNameText != null)
            charNameText.text = greeting;
        else
            Debug.LogWarning("[Confirm] charNameText が未結線のため挨拶文の表示をスキップ");
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
