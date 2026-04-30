using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Game.Core;

public class ProfileEditPanel : MonoBehaviour
{
    [Header("ユーザー情報")]
    [SerializeField] private TMP_InputField userNameInput;
    [SerializeField] private TMP_InputField monthInput;
    [SerializeField] private TMP_InputField dayInput;

    [Header("UI要素")]
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI nameWarningText;
    [SerializeField] private TextMeshProUGUI birthdayWarningText;

    private void Start()
    {
        nextButton.onClick.AddListener(OnNextClicked);
    }

    public void OnNextClicked()
    {
        Debug.Log("<color=yellow>【ProfileEditPanel】決定ボタンが押された！</color>");
        bool isValid = true;

        // 名前チェック（スペースのみも空扱い、文字数制限はInspectorで管理）
        string userName = userNameInput != null ? userNameInput.text : "";
        if (string.IsNullOrWhiteSpace(userName))
        {
            if (nameWarningText != null)
            {
                nameWarningText.text = "※名前を入力してね";
                nameWarningText.gameObject.SetActive(true);
            }
            isValid = false;
        }
        else
        {
            if (nameWarningText != null) nameWarningText.gameObject.SetActive(false);
        }

        // 誕生日チェック
        string month = monthInput != null ? monthInput.text : "";
        string day = dayInput != null ? dayInput.text : "";
        if (!CheckDate(month, day))
        {
            if (birthdayWarningText != null)
            {
                birthdayWarningText.text = "※正しい日付を入れてね";
                birthdayWarningText.gameObject.SetActive(true);
            }
            isValid = false;
        }
        else
        {
            if (birthdayWarningText != null) birthdayWarningText.gameObject.SetActive(false);
        }

        if (!isValid) return;

        var data = SaveManager.Instance.Data;
        data.userName = userNameInput.text;
        data.ownerBirthday = $"{monthInput.text}月{dayInput.text}日";
        SaveManager.Instance.Save();

        Debug.Log($"<color=green>【ProfileEditPanel】保存完了！ 名前={data.userName}, 誕生日={data.ownerBirthday}</color>");
        FindAnyObjectByType<StoryPanelManager>()?.NextSub();
    }

    private bool CheckDate(string mStr, string dStr)
    {
        if (!int.TryParse(mStr, out int m) || !int.TryParse(dStr, out int d)) return false;
        return (m >= 1 && m <= 12 && d >= 1 && d <= 31);
    }
}
