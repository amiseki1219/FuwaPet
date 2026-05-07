using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SettingManager : MonoBehaviour
{
    // CareActions.csから参照される旗だっぴ！
    public static bool shouldOpenProfileOnLoad = false;

    [Header("--- Panels & Navigation ---")]
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private GameObject aiDataPanel;

    [Header("--- Profile Detail Panel ---")]
    [SerializeField] private GameObject profileDetailPanel;
    [SerializeField] private TextMeshProUGUI profileIdText;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TMP_InputField birthdayInputField;
    [SerializeField] private TextMeshProUGUI anniversaryValueText;
    [SerializeField] private GameObject profileSaveButton;
    [SerializeField] private GameObject editButton;
    [SerializeField] private RawImage profileIconImage;
    [SerializeField] private TextMeshProUGUI characterNameValueText;

    [Header("--- Data Transfer Panel ---")]
    [SerializeField] private GameObject dataTransferPanel;

    [Header("--- Purchase Restore Panel ---")]
    [SerializeField] private GameObject purchaseRestorePanel;

    [Header("--- BGM Slider ---")]
    [SerializeField] private Slider bgmSlider;

    [Header("--- Sliding Toggles (SE & Notify) ---")]
    [SerializeField] private ToggleSwitchUI seToggle;
    [SerializeField] private ToggleSwitchUI notifyToggle;

    [Header("--- Info & Version ---")]
    [SerializeField] private TextMeshProUGUI displayPlayerIdText;
    [SerializeField] private TextMeshProUGUI versionText;

    [Header("--- Delete Confirm Panel ---")]
    [SerializeField] private GameObject deleteConfirmPanel;
    [SerializeField] private Image[] radioIndicators;   // 4つ: 使わなくなった/移行/不具合/その他
    [SerializeField] private GameObject otherInputGroup;
    [SerializeField] private TMP_InputField otherInputField;
    [SerializeField] private TextMeshProUGUI errorText;

    private int selectedReasonIndex = -1;
    private bool _isProfileEditing = false;

    private static readonly string[] ReasonLabels = {
        "使わなくなった",
        "別のアカウントに移行する",
        "不具合・エラーが多い",
        "その他"
    };

    private void Start()
    {
        RefreshUI();

        if (versionText != null)
            versionText.text = "Ver. " + Application.version;

        if (displayPlayerIdText != null && SaveManager.Instance != null)
            displayPlayerIdText.text = "ID: " + SaveManager.Instance.Data.playerId;

        if (profileIdText != null && SaveManager.Instance != null)
            profileIdText.text = SaveManager.Instance.Data.playerId;

        if (otherInputField != null)
            otherInputField.onValueChanged.AddListener(OnOtherInputChanged);
    }

    // --- ID コピー ---
    public void CopyPlayerId()
    {
        if (SaveManager.Instance == null) return;
        string id = SaveManager.Instance.Data.playerId;
        GUIUtility.systemCopyBuffer = id;
        Debug.Log("[Setting] IDをコピーしました: " + id);
    }

    public void OnProfileDetailsClicked()
    {
        if (SaveManager.Instance == null) return;

        string id = SaveManager.Instance.Data.playerId;
        GUIUtility.systemCopyBuffer = id;
        Debug.Log("[Setting] IDをコピーしました: " + id);

        shouldOpenProfileOnLoad = true;
        SceneManager.LoadScene("Care");
    }

    // --- ボタン処理 ---
    public void OnContactClicked() => Application.OpenURL("https://forms.gle/cw6MdGnq1Kibqbdr7");
    public void OnTermsOfServiceClicked() => Application.OpenURL("https://jagged-wombat-9c5.notion.site/YURUFU-35184120f12f80cba92bd4f91f2bdeae");
    public void OnPrivacyPolicyClicked() => Application.OpenURL("https://jagged-wombat-9c5.notion.site/YURUFUWorld-35184120f12f80b4b2b7f16a179c5785");
    public void OnAiDataUsageClicked() { if (aiDataPanel != null) aiDataPanel.SetActive(true); }
    public void CloseAiDataPanel() { if (aiDataPanel != null) aiDataPanel.SetActive(false); }

    // --- Profile Detail Panel ---
    public void OpenProfileDetail()
    {
        if (profileDetailPanel != null) profileDetailPanel.SetActive(true);
        LoadProfileUI();
    }

    public void CloseProfileDetail()
    {
        if (profileDetailPanel != null) profileDetailPanel.SetActive(false);
        // 編集中なら入力欄を閉じてリセット
        if (_isProfileEditing) ResetProfileEditState();
    }

    private void LoadProfileUI()
    {
        if (SaveManager.Instance == null) return;
        var data = SaveManager.Instance.Data;

        if (nameInputField != null)
        {
            nameInputField.text = data.userName ?? "";
            nameInputField.interactable = false;
        }
        if (birthdayInputField != null)
        {
            birthdayInputField.text = data.ownerBirthday ?? "";
            birthdayInputField.interactable = false;
        }

        // 初回起動日：未設定なら今日の日付を保存
        if (string.IsNullOrEmpty(data.firstLoginDate))
        {
            data.firstLoginDate = System.DateTime.Now.ToString("yyyy/MM/dd");
            SaveManager.Instance.Save();
        }
        if (anniversaryValueText != null)
            anniversaryValueText.text = data.firstLoginDate;

        if (profileIdText != null)
            profileIdText.text = data.playerId;

        // キャラクター名表示（ニックネームがあればそれを優先）
        if (characterNameValueText != null)
        {
            string charId = !string.IsNullOrEmpty(data.selectedCharacterId) ? data.selectedCharacterId : data.characterId;
            if (!string.IsNullOrEmpty(data.petNickname))
            {
                characterNameValueText.text = data.petNickname;
            }
            else
            {
                characterNameValueText.text = charId switch
                {
                    "poko" => "ぽこ",
                    "eru"  => "える",
                    "koko" => "ここ",
                    "paru" => "ぱる",
                    _ => charId ?? ""
                };
            }
        }

        // アイコン表示（iconId → profileImagePath の順にフォールバック）
        if (profileIconImage != null)
        {
            string iconId = !string.IsNullOrEmpty(data.iconId) ? data.iconId : data.profileImagePath;
            if (!string.IsNullOrEmpty(iconId))
            {
                Texture tex = Resources.Load<Texture>("SpecialIcon/" + iconId);
                if (tex == null) tex = Resources.Load<Texture>("Icon/" + iconId);
                if (tex != null)
                    profileIconImage.texture = tex;
                else
                    Debug.LogWarning("[ProfileDetail] アイコンが見つからないお: " + iconId);
            }
        }

        if (profileSaveButton != null) profileSaveButton.SetActive(false);
        _isProfileEditing = false;
    }

    public void OnEditButtonClicked()
    {
        if (nameInputField != null)
        {
            nameInputField.interactable = true;
            nameInputField.onValueChanged.RemoveListener(OnProfileFieldChanged);
            nameInputField.onValueChanged.AddListener(OnProfileFieldChanged);
        }
        if (birthdayInputField != null)
        {
            birthdayInputField.interactable = true;
            birthdayInputField.onValueChanged.RemoveListener(OnProfileFieldChanged);
            birthdayInputField.onValueChanged.AddListener(OnProfileFieldChanged);
        }
        _isProfileEditing = true;
    }

    private void OnProfileFieldChanged(string value)
    {
        if (profileSaveButton != null) profileSaveButton.SetActive(true);
    }

    private void ResetProfileEditState()
    {
        if (nameInputField != null) nameInputField.interactable = false;
        if (birthdayInputField != null) birthdayInputField.interactable = false;
        if (profileSaveButton != null) profileSaveButton.SetActive(false);
        _isProfileEditing = false;
    }

    public void OnProfileIconClicked()
    {
        Debug.Log("[Setting] プロフィールアイコンをタップ（未実装）");
    }

    public void OnProfileSaveClicked()
    {
        if (SaveManager.Instance == null) return;
        if (nameInputField != null)
            SaveManager.Instance.Data.userName = nameInputField.text;
        if (birthdayInputField != null)
            SaveManager.Instance.Data.ownerBirthday = birthdayInputField.text;
        SaveManager.Instance.Save();
        ResetProfileEditState();
        Debug.Log("[ProfileDetail] 保存しました");
    }

    // --- Data Transfer Panel ---
    public void OpenDataTransfer()
    {
        if (dataTransferPanel != null) dataTransferPanel.SetActive(true);
    }

    public void CloseDataTransfer()
    {
        if (dataTransferPanel != null) dataTransferPanel.SetActive(false);
    }

    public void OnAppleSignInClicked() => Debug.Log("[Setting] Appleでサインイン（Firebase実装待ち）");
    public void OnGoogleSignInClicked() => Debug.Log("[Setting] Googleでサインイン（Firebase実装待ち）");
    public void OnEmailSignUpClicked() => Debug.Log("[Setting] メールアドレスで登録（Firebase実装待ち）");

    // --- Purchase Restore Panel ---
    public void OpenPurchaseRestore()
    {
        if (purchaseRestorePanel != null) purchaseRestorePanel.SetActive(true);
    }

    public void ClosePurchaseRestore()
    {
        if (purchaseRestorePanel != null) purchaseRestorePanel.SetActive(false);
    }

    public void OnPurchaseRestoreConfirmed() => Debug.Log("[Setting] 購入情報を復元中（IAP実装待ち）");

    // --- アカウント削除パネル ---
    public void OnDeleteAccountClicked()
    {
        selectedReasonIndex = -1;
        if (otherInputGroup != null) otherInputGroup.SetActive(false);
        if (otherInputField != null) otherInputField.text = "";
        if (errorText != null) errorText.gameObject.SetActive(false);
        UpdateRadioVisuals();
        if (settingPanel != null) settingPanel.SetActive(false);
        if (deleteConfirmPanel != null) deleteConfirmPanel.SetActive(true);
    }

    public void CloseDeleteConfirmPanel()
    {
        if (deleteConfirmPanel != null) deleteConfirmPanel.SetActive(false);
        if (settingPanel != null) settingPanel.SetActive(true);
    }

    // ラジオボタン選択（Button.onClick から番号別に呼び出し）
    public void SelectReason0() => SelectReason(0);
    public void SelectReason1() => SelectReason(1);
    public void SelectReason2() => SelectReason(2);
    public void SelectReason3() => SelectReason(3);

    private void SelectReason(int index)
    {
        selectedReasonIndex = index;
        if (otherInputGroup != null) otherInputGroup.SetActive(index == 3);
        if (errorText != null) errorText.gameObject.SetActive(false);
        UpdateRadioVisuals();
    }

    private void OnOtherInputChanged(string value)
    {
        if (errorText != null && !string.IsNullOrEmpty(value))
            errorText.gameObject.SetActive(false);
    }

    private void UpdateRadioVisuals()
    {
        if (radioIndicators == null) return;
        var pink = new Color(1f, 0.42f, 0.62f, 1f);
        var grey = new Color(0.78f, 0.78f, 0.78f, 1f);
        for (int i = 0; i < radioIndicators.Length; i++)
        {
            if (radioIndicators[i] != null)
                radioIndicators[i].color = (i == selectedReasonIndex) ? pink : grey;
        }
    }

    public void OnDeleteConfirmed()
    {
        if (selectedReasonIndex < 0)
        {
            if (errorText != null) errorText.gameObject.SetActive(true);
            return;
        }

        if (selectedReasonIndex == 3
            && (otherInputField == null || string.IsNullOrEmpty(otherInputField.text)))
        {
            if (errorText != null) errorText.gameObject.SetActive(true);
            return;
        }

        if (errorText != null) errorText.gameObject.SetActive(false);

        string reason = selectedReasonIndex >= 0 ? ReasonLabels[selectedReasonIndex] : "未選択";
        if (selectedReasonIndex == 3 && otherInputField != null)
            reason = "その他: " + otherInputField.text;
        Debug.Log("[Setting] アカウント削除リクエスト | 理由: " + reason);
        Debug.Log("[Setting] Firebase/バックエンド削除処理 (未実装)");

        if (settingPanel != null) settingPanel.SetActive(false);

        if (SaveManager.Instance != null) SaveManager.Instance.DeleteData();

        SceneLoader.LoadHome();
    }

    // --- BGM / Toggle ---
    public void RefreshUI()
    {
        if (SaveManager.Instance == null) return;
        var data = SaveManager.Instance.Data;
        if (bgmSlider != null) bgmSlider.value = data.bgmVolume;
        notifyToggle?.SetState(data.isNotificationOn, false);
        seToggle?.SetState(data.isSeOn, false);
        AudioListener.volume = data.bgmVolume;
    }

    public void OnBgmSliderChanged()
    {
        if (SaveManager.Instance == null) return;
        float value = bgmSlider.value;
        Debug.Log("[Setting] BGM音量変更: " + value);
        SaveManager.Instance.Data.bgmVolume = value;
        AudioListener.volume = value;
        SaveManager.Instance.Save();
    }

    public void ToggleNotification()
    {
        bool before = SaveManager.Instance != null ? SaveManager.Instance.Data.isNotificationOn : false;
        Debug.Log("[Setting] ToggleNotification呼ばれた / 現在の状態: " +
            (SaveManager.Instance != null ? (before ? "ON" : "OFF") : "SaveManager NULL"));
        if (SaveManager.Instance == null) return;
        SaveManager.Instance.Data.isNotificationOn = !before;
        bool isOn = SaveManager.Instance.Data.isNotificationOn;
        Debug.Log("[Setting] 通知トグル → " + (isOn ? "ON" : "OFF"));
        notifyToggle?.SetState(isOn);
        SaveManager.Instance.Save();
    }

    public void ToggleSe()
    {
        bool before = SaveManager.Instance != null ? SaveManager.Instance.Data.isSeOn : false;
        Debug.Log("[Setting] ToggleSe呼ばれた / 現在の状態: " +
            (SaveManager.Instance != null ? (before ? "ON" : "OFF") : "SaveManager NULL"));
        if (SaveManager.Instance == null) return;
        SaveManager.Instance.Data.isSeOn = !before;
        bool isOn = SaveManager.Instance.Data.isSeOn;
        Debug.Log("[Setting] SEトグル → " + (isOn ? "ON" : "OFF"));
        seToggle?.SetState(isOn);
        SaveManager.Instance.Save();
    }
}
