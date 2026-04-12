using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Game.Core;
using System;
using UnityEngine.SceneManagement;

public class ProfileEditPanel : MonoBehaviour
{
    [Header("ユーザー情報")]
    [SerializeField] private TMP_InputField ownerNameInput;
    [SerializeField] private TMP_InputField monthInput;
    [SerializeField] private TMP_InputField dayInput;

    [Header("ペット情報")]
    [SerializeField] private TMP_InputField petNameInput;
    [SerializeField] private Image characterIconPreview; // 右下のペット

    [Header("プロフィール画像反映")]
    [SerializeField] private RawImage profileRawImage;    // ★中央の大きい丸
    [SerializeField] private GameObject profileSelectionPanel;

    [Header("UI要素")]
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI warningText;

    private void OnEnable() { RefreshDisplay(); }

    private void Start()
    {
        ownerNameInput.onValueChanged.AddListener(_ => Validate());
        monthInput.onValueChanged.AddListener(_ => Validate());
        dayInput.onValueChanged.AddListener(_ => Validate());
        petNameInput.onValueChanged.AddListener(_ => Validate());
        startButton.onClick.AddListener(OnStartGame);
        Validate();
    }

    public void RefreshDisplay()
    {
        var data = SaveManager.Instance.Data;

        // 1. ペットアイコン（PetIconフォルダ）
        if (!string.IsNullOrEmpty(data.selectedCharacterId))
        {
            string petPath = $"PetIcon/PetIcon_{data.selectedCharacterId}";
            Sprite s = Resources.Load<Sprite>(petPath);
            if (characterIconPreview != null && s != null) characterIconPreview.sprite = s;
        }

        // 2. プロフィール画像（Iconsフォルダ）
        UpdateProfilePreview(data.profileImagePath);
    }

    public void OnClickPencil() { if (profileSelectionPanel != null) profileSelectionPanel.SetActive(true); }

    // プロフィール画像（RawImage）を差し替える魔法
    public void UpdateProfilePreview(string iconName)
    {
        if (string.IsNullOrEmpty(iconName)) return;

        // ★ ここを "Icons/" から "Icon/" に変更したお！
        string fullPath = $"Icon/{iconName}";
        Texture2D tex = Resources.Load<Texture2D>(fullPath);

        if (profileRawImage != null && tex != null)
        {
            profileRawImage.texture = tex;
            Debug.Log($"<color=green>【成功】画像を RawImage に反映したお！: {fullPath}</color>");
        }
        else if (tex == null)
        {
            // まだエラーが出るなら、ここをチェックしてだお！
            Debug.LogError($"<color=red>【失敗】画像が見つからないお！ パスを確認してね: Resources/{fullPath}</color>");
        }
    }

    // --- 以下、Validate と OnStartGame はそのまま ---
    void Validate()
    {
        bool isOwnerNameValid = !string.IsNullOrWhiteSpace(ownerNameInput.text) && ownerNameInput.text.Length <= 8;
        bool isPetNameValid = !string.IsNullOrWhiteSpace(petNameInput.text) && petNameInput.text.Length <= 8;
        bool isDateValid = CheckDate(monthInput.text, dayInput.text);

        if (warningText != null)
        {
            warningText.gameObject.SetActive(true);
            if (!isOwnerNameValid || !isPetNameValid) warningText.text = "※名前は8文字以内で入力してね";
            else if (!isDateValid && (!string.IsNullOrEmpty(monthInput.text) || !string.IsNullOrEmpty(dayInput.text))) warningText.text = "※正しい日付を入れてね";
            else warningText.gameObject.SetActive(false);
        }
        startButton.interactable = isOwnerNameValid && isPetNameValid && isDateValid;
    }

    private bool CheckDate(string mStr, string dStr)
    {
        if (!int.TryParse(mStr, out int m) || !int.TryParse(dStr, out int d)) return false;
        return (m >= 1 && m <= 12 && d >= 1 && d <= 31);
    }

    private void OnStartGame()
    {
        var data = SaveManager.Instance.Data;
        data.ownerName = ownerNameInput.text;
        data.ownerBirthday = $"{monthInput.text}月{dayInput.text}日";
        data.petName = petNameInput.text;
        data.onboardingCompleted = true;
        data.startDate = DateTime.Now.ToString("yyyy-MM-dd");
        SaveManager.Instance.Save();
        SceneManager.LoadScene("Care");
    }
}