using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Game.Core;

public class ProfileEditPanel : MonoBehaviour
{
    [Header("ユーザー情報")]
    [SerializeField] private TMP_InputField userNameInput;
    [SerializeField] private TMP_Dropdown yearDropdown;
    [SerializeField] private TMP_Dropdown monthDropdown;
    [SerializeField] private TMP_Dropdown dayDropdown;

    [Header("UI要素")]
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI nameWarningText;
    [SerializeField] private TextMeshProUGUI birthdayWarningText;

    [Header("フロー")]
    [SerializeField] private OnboardingManager onboardingManager;

    // caption text の文字色（未選択／選択済み）
    private static readonly string ColorUnselected = "#DDD4CB";
    private static readonly string ColorSelected   = "#613F2B";

    private void Start()
    {
        if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);

        BuildDropdownOptions();
        Debug.Log("[Profile] Dropdown生成: year 2026-1940降順+プレースホルダー / month / day");
    }

    // Dropdown の options をコード生成する（先頭にプレースホルダーを入れる）
    private void BuildDropdownOptions()
    {
        // 年：先頭"年" ＋ 2026〜1940 降順
        if (yearDropdown != null)
        {
            var years = new List<string> { "年" };
            for (int y = 2026; y >= 1940; y--) years.Add(y.ToString());
            SetupDropdown(yearDropdown, years);
        }
        else
        {
            Debug.LogWarning("[Profile] yearDropdown が未結線のため生成をスキップ");
        }

        // 月：先頭"月" ＋ 1〜12
        if (monthDropdown != null)
        {
            var months = new List<string> { "月" };
            for (int m = 1; m <= 12; m++) months.Add(m.ToString());
            SetupDropdown(monthDropdown, months);
        }
        else
        {
            Debug.LogWarning("[Profile] monthDropdown が未結線のため生成をスキップ");
        }

        // 日：先頭"日" ＋ 1〜31 固定（存在しない日は決定時バリデーションで弾く）
        if (dayDropdown != null)
        {
            var days = new List<string> { "日" };
            for (int d = 1; d <= 31; d++) days.Add(d.ToString());
            SetupDropdown(dayDropdown, days);
        }
        else
        {
            Debug.LogWarning("[Profile] dayDropdown が未結線のため生成をスキップ");
        }
    }

    // options 適用 → value=0（未選択）に初期化 → 色更新 → onValueChanged 登録
    private void SetupDropdown(TMP_Dropdown dropdown, List<string> options)
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(options);
        dropdown.value = 0;                 // プレースホルダー＝未選択
        dropdown.RefreshShownValue();
        UpdateDropdownColor(dropdown);      // 初期は未選択色
        dropdown.onValueChanged.AddListener(_ => UpdateDropdownColor(dropdown));
    }

    // caption text の色を value に応じて切り替える（value==0 で未選択色）
    private void UpdateDropdownColor(TMP_Dropdown dropdown)
    {
        if (dropdown == null) return;

        var caption = dropdown.captionText;
        if (caption == null)
        {
            Debug.LogWarning("[Profile] captionText が未設定のため色更新をスキップ");
            return;
        }

        string hex = dropdown.value == 0 ? ColorUnselected : ColorSelected;
        if (ColorUtility.TryParseHtmlString(hex, out var c))
        {
            caption.color = c;
        }
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

        // 誕生日チェック（2段階：未選択 → うるう年込み厳密判定）
        int year = 0, month = 0, day = 0;
        bool birthdayOk = true;

        // (1) 未選択チェック（先頭プレースホルダー＝value==0）
        bool anyUnselected =
            yearDropdown == null || monthDropdown == null || dayDropdown == null ||
            yearDropdown.value == 0 || monthDropdown.value == 0 || dayDropdown.value == 0;

        if (anyUnselected)
        {
            if (birthdayWarningText != null)
            {
                birthdayWarningText.text = "※誕生日を選択してね";
                birthdayWarningText.gameObject.SetActive(true);
            }
            isValid = false;
            birthdayOk = false;
        }
        else if (!TryGetSelectedDate(out year, out month, out day))
        {
            // (2) 全て選択済みだが日付が不正（例: 2/30, 4/31）
            if (birthdayWarningText != null)
            {
                birthdayWarningText.text = "※正しい日付を入れてね";
                birthdayWarningText.gameObject.SetActive(true);
            }
            isValid = false;
            birthdayOk = false;
        }

        if (birthdayOk && birthdayWarningText != null)
        {
            birthdayWarningText.gameObject.SetActive(false);
        }

        if (!isValid) return;

        // 保存
        var data = SaveManager.Instance.Data;
        data.userName = userNameInput.text;
        data.ownerBirthday = $"{month}月{day}日";     // 既存形式を維持
        data.ownerBirthYear = year.ToString();         // 新規：西暦4桁
        SaveManager.Instance.Save();

        Debug.Log($"<color=#00E5FF>[決定] ユーザー情報確定: userName={data.userName} birthday={data.ownerBirthday} year={data.ownerBirthYear}</color>");

        // 遷移
        if (onboardingManager != null)
        {
            onboardingManager.Next();
        }
        else
        {
            Debug.LogWarning("[Profile] onboardingManager が未結線のため遷移できません");
        }
    }

    // 各 Dropdown の選択値を int 化し、うるう年込みで妥当な日付かを判定する
    // （未選択チェックは呼び出し側で済ませている前提。ここは値の妥当性のみ）
    private bool TryGetSelectedDate(out int year, out int month, out int day)
    {
        year = month = day = 0;

        try
        {
            year  = int.Parse(yearDropdown.options[yearDropdown.value].text);
            month = int.Parse(monthDropdown.options[monthDropdown.value].text);
            day   = int.Parse(dayDropdown.options[dayDropdown.value].text);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Profile] 誕生日の解析に失敗: {e.Message}");
            return false;
        }

        if (month < 1 || month > 12) return false;
        // うるう年込みの厳密判定（例: 2001/2/29=NG, 2000/2/29=OK, 4/31=NG）
        int maxDay = DateTime.DaysInMonth(year, month);
        return day >= 1 && day <= maxDay;
    }
}
