using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Game.Core;
using System.Text.RegularExpressions;

public class OwnerPanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInput;

    // ★ここを2つに分けたよ！
    [SerializeField] private TMP_InputField monthInput;
    [SerializeField] private TMP_InputField dayInput;

    [SerializeField] private Button nextButton;
    [SerializeField] private OnboardingManager onboarding;
    [SerializeField] private GameObject birthdayWarningText; // ★この1行を書き足すっぴ！

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
        // 1. 半角数字以外の文字が入っていないかチェック
        bool isInvalidFormat = Regex.IsMatch(monthInput.text, @"[^0-9]") || Regex.IsMatch(dayInput.text, @"[^0-9]");

        // 2. ありえない数字（月>12、日>31）のチェック
        bool isInvalidRange = false;

        // 月のチェック
        if (int.TryParse(monthInput.text, out int month))
        {
            if (month < 1 || month > 12) isInvalidRange = true;
        }

        // 日のチェック
        if (int.TryParse(dayInput.text, out int day))
        {
            if (day < 1 || day > 31) isInvalidRange = true;
        }

        // 3. 警告テキストの表示・非表示を切り替え
        // 「数字じゃない時」か「範囲がおかしい時」に警告を出すよ
        if (birthdayWarningText != null)
        {
            birthdayWarningText.SetActive(isInvalidFormat || isInvalidRange);

            // 💡 警告メッセージを状況に合わせて変えたいなら、こんな風にもできるお！
            var warningTxt = birthdayWarningText.GetComponent<TextMeshProUGUI>();
            if (warningTxt != null)
            {
                if (isInvalidFormat) warningTxt.text = "※半角数字で入力してね";
                else if (isInvalidRange) warningTxt.text = "※正しい日付を入力してね";
            }
        }

        // 4. 全項目に入力があり、かつエラーがない時だけボタンを有効にする
        bool isAllFilled = !string.IsNullOrWhiteSpace(nameInput.text) &&
                           !string.IsNullOrWhiteSpace(monthInput.text) &&
                           !string.IsNullOrWhiteSpace(dayInput.text);

        nextButton.interactable = isAllFilled && !isInvalidFormat && !isInvalidRange;
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