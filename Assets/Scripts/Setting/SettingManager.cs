using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Game.Core;

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
    [SerializeField] private TMP_InputField charNameInputField;
    [SerializeField] private TMP_InputField birthdayInputField;
    [SerializeField] private TextMeshProUGUI anniversaryValueText;
    [SerializeField] private GameObject profileSaveButton;
    [SerializeField] private GameObject editButton;
    [SerializeField] private RawImage profileIconImage;
    [SerializeField] private TextMeshProUGUI characterNameValueText;
    [SerializeField] private TextMeshProUGUI profileAlertText;
    [SerializeField] private TextMeshProUGUI profileErrorText;
    [SerializeField] private Button cancelButton;

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
    private bool _isProfileEditing  = false;
    private bool _awaitingConfirm   = false;
    private string _origName        = "";
    private string _origCharName    = "";
    private string _origBirthday    = "";

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
    public void OnContactClicked() => Application.OpenURL("https://yurufuworld.com/contact.html");
    public void OnTermsOfServiceClicked() => Application.OpenURL("https://yurufuworld.com/terms.html");
    public void OnPrivacyPolicyClicked() => Application.OpenURL("https://yurufuworld.com/privacy.html");
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
        if (cancelButton != null)      cancelButton.gameObject.SetActive(false);
        if (profileAlertText != null)  profileAlertText.gameObject.SetActive(false);
        if (charNameInputField != null) charNameInputField.gameObject.SetActive(false);
        _isProfileEditing  = false;
        _awaitingConfirm   = false;

        CheckLockStateOnOpen(data);
    }

    private void CheckLockStateOnOpen(SaveData data)
    {
        bool nameLocked     = IsLocked(data.lastNameChangeDate);
        bool charNameLocked = IsLocked(data.lastCharNameChangeDate);
        bool birthdayLocked = IsLocked(data.lastBirthdayChangeDate);

        int lockedCount = (nameLocked ? 1 : 0) + (charNameLocked ? 1 : 0) + (birthdayLocked ? 1 : 0);

        string message = "";

        if (lockedCount == 1)
        {
            // 単体：「名前はXXXXまで変更できません」
            if (nameLocked)     message = "名前は" + GetUnlockDate(data.lastNameChangeDate) + "まで変更できません";
            if (charNameLocked) message = "キャラクター名は" + GetUnlockDate(data.lastCharNameChangeDate) + "まで変更できません";
            if (birthdayLocked) message = "誕生日は" + GetUnlockDate(data.lastBirthdayChangeDate) + "まで変更できません";
        }
        else if (lockedCount == 2)
        {
            // 複数：「名前・キャラクター名はXXXXまで変更できません」（最遅の解除日）
            var names = new System.Collections.Generic.List<string>();
            var dates = new System.Collections.Generic.List<string>();
            if (nameLocked)     { names.Add("名前");         dates.Add(data.lastNameChangeDate); }
            if (charNameLocked) { names.Add("キャラクター名"); dates.Add(data.lastCharNameChangeDate); }
            if (birthdayLocked) { names.Add("誕生日");        dates.Add(data.lastBirthdayChangeDate); }

            string latestDate = dates[0];
            foreach (var d in dates)
            {
                if (System.DateTime.TryParse(d, out System.DateTime dt) &&
                    System.DateTime.TryParse(latestDate, out System.DateTime current) &&
                    dt > current)
                    latestDate = d;
            }
            message = string.Join("・", names) + "は" + GetUnlockDate(latestDate) + "まで変更できません";
        }
        else if (lockedCount == 3)
        {
            // 全項目：「XXXXまで変更できません」（最遅の解除日のみ）
            string latestDate = data.lastNameChangeDate;
            foreach (var d in new[] { data.lastCharNameChangeDate, data.lastBirthdayChangeDate })
            {
                if (System.DateTime.TryParse(d, out System.DateTime dt) &&
                    System.DateTime.TryParse(latestDate, out System.DateTime current) &&
                    dt > current)
                    latestDate = d;
            }
            message = GetUnlockDate(latestDate) + "まで変更できません";
        }

        if (profileErrorText != null)
        {
            if (lockedCount > 0)
            {
                profileErrorText.text = message;
                profileErrorText.gameObject.SetActive(true);
            }
            else
            {
                profileErrorText.gameObject.SetActive(false);
            }
        }
    }

    public void OnEditButtonClicked()
    {
        var data = SaveManager.Instance?.Data;
        if (data == null) return;

        bool nameLocked     = IsLocked(data.lastNameChangeDate);
        bool charNameLocked = IsLocked(data.lastCharNameChangeDate);
        bool birthdayLocked = IsLocked(data.lastBirthdayChangeDate);

        // 全項目ロック中は編集不可（エラーメッセージは LoadProfileUI で表示済み）
        if (nameLocked && charNameLocked && birthdayLocked) return;

        // ロックエラーテキストを非表示
        if (profileErrorText != null) profileErrorText.gameObject.SetActive(false);

        // ロックされていないフィールドを編集可能に
        if (!nameLocked && nameInputField != null)
        {
            nameInputField.interactable = true;
            nameInputField.onValueChanged.RemoveListener(OnProfileFieldChanged);
            nameInputField.onValueChanged.AddListener(OnProfileFieldChanged);
        }
        if (!charNameLocked && charNameInputField != null)
        {
            if (characterNameValueText != null) characterNameValueText.gameObject.SetActive(false);
            charNameInputField.text = data.petNickname ?? "";
            charNameInputField.gameObject.SetActive(true);
            charNameInputField.interactable = true;
            charNameInputField.onValueChanged.RemoveListener(OnProfileFieldChanged);
            charNameInputField.onValueChanged.AddListener(OnProfileFieldChanged);
        }
        if (!birthdayLocked && birthdayInputField != null)
        {
            birthdayInputField.interactable = true;
            birthdayInputField.onValueChanged.RemoveListener(OnProfileFieldChanged);
            birthdayInputField.onValueChanged.AddListener(OnProfileFieldChanged);
        }

        // 元の値を保存（変更検出用）
        _origName     = data.userName ?? "";
        _origCharName = data.petNickname ?? "";
        _origBirthday = data.ownerBirthday ?? "";

        if (profileSaveButton != null) profileSaveButton.SetActive(true);
        if (cancelButton != null)      cancelButton.gameObject.SetActive(true);

        _isProfileEditing = true;
        _awaitingConfirm  = false;
    }

    public void OnCancelClicked()
    {
        // 入力値を編集前に戻す
        if (nameInputField != null)     nameInputField.text     = _origName;
        if (charNameInputField != null) charNameInputField.text = _origCharName;
        if (birthdayInputField != null) birthdayInputField.text = _origBirthday;
        ResetProfileEditState();
    }

    private void OnProfileFieldChanged(string value)
    {
        if (profileSaveButton != null) profileSaveButton.SetActive(true);
    }

    private void ResetProfileEditState()
    {
        if (nameInputField != null) nameInputField.interactable = false;
        if (charNameInputField != null)
        {
            charNameInputField.interactable = false;
            charNameInputField.gameObject.SetActive(false);
        }
        if (characterNameValueText != null) characterNameValueText.gameObject.SetActive(true);
        if (birthdayInputField != null) birthdayInputField.interactable = false;
        if (profileSaveButton != null) profileSaveButton.SetActive(false);
        if (cancelButton != null)      cancelButton.gameObject.SetActive(false);
        if (profileAlertText != null)  profileAlertText.gameObject.SetActive(false);
        _isProfileEditing = false;
        _awaitingConfirm  = false;
    }

    // ─── 2週間ロックヘルパー ──────────────────────────────────

    private bool IsLocked(string dateStr)
    {
        if (string.IsNullOrEmpty(dateStr)) return false;
        if (System.DateTime.TryParse(dateStr, out System.DateTime d))
            return (System.DateTime.Now - d).TotalDays < 14.0;
        return false;
    }

    private string GetUnlockDate(string dateStr)
    {
        if (System.DateTime.TryParse(dateStr, out System.DateTime d))
            return d.AddDays(14).ToString("yyyy/MM/dd");
        return "";
    }

    public void OnProfileIconClicked()
    {
        Debug.Log("[Setting] プロフィールアイコンをタップ（未実装）");
    }

    public void OnProfileSaveClicked()
    {
        if (SaveManager.Instance == null) return;
        var data = SaveManager.Instance.Data;

        if (!_awaitingConfirm)
        {
            // 誕生日フォーマット検証（変更がある場合のみ）
            if (birthdayInputField != null &&
                birthdayInputField.interactable &&
                birthdayInputField.text != _origBirthday &&
                !string.IsNullOrEmpty(birthdayInputField.text))
            {
                string birthdayText = birthdayInputField.text;
                bool formatOk = System.Text.RegularExpressions.Regex.IsMatch(birthdayText, @"^\d{1,2}/\d{1,2}$");
                string birthdayError = "";
                if (!formatOk)
                {
                    birthdayError = "誕生日は「月/日」の形式で入力してください（例：3/15）";
                }
                else
                {
                    var parts = birthdayText.Split('/');
                    int month = int.Parse(parts[0]);
                    int day   = int.Parse(parts[1]);
                    if (month < 1 || month > 12)
                        birthdayError = "月は1〜12の範囲で入力してください";
                    else if (day < 1 || day > 31)
                        birthdayError = "日は1〜31の範囲で入力してください";
                }
                if (!string.IsNullOrEmpty(birthdayError))
                {
                    if (profileAlertText != null)
                    {
                        profileAlertText.text = birthdayError;
                        profileAlertText.gameObject.SetActive(true);
                    }
                    return;
                }
            }

            // 1回目：変更項目を検出してAlertTextを表示
            var changed = new System.Collections.Generic.List<string>();
            if (nameInputField != null && nameInputField.text != _origName)                       changed.Add("名前");
            if (charNameInputField != null && charNameInputField.gameObject.activeSelf
                && charNameInputField.text != _origCharName)                                      changed.Add("キャラクター名");
            if (birthdayInputField != null && birthdayInputField.text != _origBirthday)           changed.Add("誕生日");

            if (changed.Count == 0)
            {
                ResetProfileEditState();
                return;
            }

            string fields = string.Join("・", changed);
            if (profileAlertText != null)
            {
                profileAlertText.text = "2週間" + fields + "を変更できませんが変更しますか？";
                profileAlertText.gameObject.SetActive(true);
            }
            _awaitingConfirm = true;
        }
        else
        {
            // 2回目：保存して変更日を記録
            string today = System.DateTime.Now.ToString("yyyy-MM-dd");

            if (nameInputField != null && nameInputField.text != _origName)
            {
                data.userName            = nameInputField.text;
                data.lastNameChangeDate  = today;
            }
            if (charNameInputField != null && charNameInputField.gameObject.activeSelf
                && charNameInputField.text != _origCharName)
            {
                data.petNickname             = charNameInputField.text;
                data.lastCharNameChangeDate  = today;
                if (characterNameValueText != null) characterNameValueText.text = charNameInputField.text;
            }
            if (birthdayInputField != null && birthdayInputField.text != _origBirthday)
            {
                data.ownerBirthday           = birthdayInputField.text;
                data.lastBirthdayChangeDate  = today;
            }

            SaveManager.Instance.Save();
            Debug.Log("[ProfileDetail] 保存しました");
            ResetProfileEditState();
            CheckLockStateOnOpen(data);
        }
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

        // DeleteData() が Data を新品にした後に読み直す必要があるため、順序を入れ替えないこと。
        // GameContext は DontDestroyOnLoad で生き残るので、作り直さないと
        // 次の SavePetStatus() で古い値が新しい SaveData に書き戻される。
        if (GameContext.Instance != null) GameContext.Instance.ReloadFromSave();

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
