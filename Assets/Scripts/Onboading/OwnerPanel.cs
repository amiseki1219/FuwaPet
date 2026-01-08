using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Game.Core;

public class OwnerPanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInput;

    // ★ここを2つに分けたよ！
    [SerializeField] private TMP_InputField monthInput;
    [SerializeField] private TMP_InputField dayInput;

    [SerializeField] private Button nextButton;
    [SerializeField] private OnboardingManager onboarding;

    private void Awake()
    {
        // 1. セット確認（今のままでOK！）
        if (nameInput == null || monthInput == null || dayInput == null)
            Debug.LogError("入力欄がどこかセットされてないよ！");

        if (SaveManager.Instance == null) Debug.LogError("SaveManagerがまだ準備できてないよ！");

        // 2. データの読み込み
        var data = SaveManager.Instance.Data;

        nameInput.text = data.ownerName ?? "";

        // ★ここを追加！ 保存されている「〇月〇日」から数字を抜き出して箱に戻す
        if (!string.IsNullOrEmpty(data.ownerBirthday))
        {
            // 「月」と「日」という文字で区切って数字だけ取り出すよ
            string[] parts = data.ownerBirthday.Split(new char[] { '月', '日' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                monthInput.text = parts[0]; // 前の数字（月）を入れる
                dayInput.text = parts[1];   // 後の数字（日）を入れる
            }
        }

        nextButton.interactable = false;

        // 3. リスナー登録（今のままでOK！）
        nameInput.onValueChanged.AddListener(_ => Validate());
        monthInput.onValueChanged.AddListener(_ => Validate());
        dayInput.onValueChanged.AddListener(_ => Validate());

        Validate();
    }

    void Validate()
    {
        // ★3つとも文字が入っている時だけボタンが押せる！
        bool isValid =
            !string.IsNullOrWhiteSpace(nameInput.text) &&
            !string.IsNullOrWhiteSpace(monthInput.text) &&
            !string.IsNullOrWhiteSpace(dayInput.text);

        nextButton.interactable = isValid;
    }

    public void SaveOwnerInfo()
    {
        var data = SaveManager.Instance.Data;

        data.ownerName = nameInput.text;

        // ★ここで「〇月〇日」という一つの文章にして保存するよ！
        data.ownerBirthday = $"{monthInput.text}月{dayInput.text}日";
    }

    public void OnClickNext()
    {
        SaveOwnerInfo();
        onboarding.Next();
    }
}