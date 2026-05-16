using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class HomeManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dayText;

    [Header("背景表示用")]
    [SerializeField] private Image backgroundDisplay;

    void Start()
    {
        // 起動振り分け: 初回またはアカウント削除後はTutorialへ（ローディングなし）
        if (SaveManager.Instance == null || !SaveManager.Instance.Data.onboardingCompleted)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Tutorial");
            return;
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (SaveManager.Instance == null) return;

        string today = DateTime.Today.ToString("M月d日");
        string birthday = SaveManager.Instance.Data.ownerBirthday;
        string user = SaveManager.Instance.Data.userName;
        string charID = SaveManager.Instance.Data.characterId;
        // ★ これを足して、UnityのConsoleウィンドウを見てみて！
        Debug.Log($"今日の判定: {today} / 保存されている誕生日: {birthday}");

        // もし ID が空なら予備を使う
        if (string.IsNullOrEmpty(charID))
            charID = SaveManager.Instance.Data.selectedCharacterId;

        if (today == birthday)
        {
            // 🎂 お誕生日モード
            dayText.text = $"\\ {user} /\nお誕生日おめでとう";
            LoadBackground("BackgroundUI/Birthday_" + charID);
        }
        else
        {
            // 🏠 通常モード
            string pet = SaveManager.Instance.Data.petName;
            if (!DateTime.TryParse(SaveManager.Instance.Data.startDate, out DateTime startDate))
                startDate = DateTime.Today;
            int days = (DateTime.Today - startDate).Days + 1;

            dayText.text = $"{user} と {pet} が\n出会って {days} 日";
            LoadBackground("BackgroundUI/Background_" + charID);
        }
    }

    // 背景読み込みを共通化してスッキリ！
    private void LoadBackground(string path)
    {
        Sprite bgSprite = Resources.Load<Sprite>(path);
        if (bgSprite != null && backgroundDisplay != null)
        {
            backgroundDisplay.sprite = bgSprite;
        }
        else
        {
            Debug.LogError("画像がないよ！パスを確認してね: " + path);
        }
    }
}