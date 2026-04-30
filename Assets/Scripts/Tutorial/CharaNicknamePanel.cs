using UnityEngine;
using TMPro;
using Game.Core;

public class CharaNicknamePanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField nicknameInput;

    public void OnDecideClicked()
    {
        Debug.Log($"<color=lime>【CharaNicknamePanel】入力値={nicknameInput.text}</color>");
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.petNickname = nicknameInput.text;
            SaveManager.Instance.Save();
            Debug.Log($"<color=lime>【CharaNicknamePanel】保存後petNickname={SaveManager.Instance.Data.petNickname}</color>");
        }

        FindAnyObjectByType<ProfileSelectionPanelManager>()?.ShowConfirmCard();
    }

    public void OnSkipClicked()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.petNickname = "";
            SaveManager.Instance.Save();
        }

        FindAnyObjectByType<ProfileSelectionPanelManager>()?.ShowConfirmCard();
    }
}
