using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // ★ここを SceneManagement に修正したお！

public class SettingManager : MonoBehaviour
{
    // CareActions.csから参照される旗だっぴ！
    public static bool shouldOpenProfileOnLoad = false;

    [Header("--- Panels & Navigation ---")]
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private GameObject aiDataPanel;
    [SerializeField] private GameObject deleteConfirmPanel;

    [Header("--- BGM Slider ---")]
    [SerializeField] private Slider bgmSlider;

    [Header("--- Sliding Toggles (SE & Notify) ---")]
    [SerializeField] private RectTransform notifyHandle;
    [SerializeField] private Image notifyBackground;
    [SerializeField] private RectTransform seHandle;
    [SerializeField] private Image seBackground;

    [SerializeField] private float posOn = 40f;
    [SerializeField] private float posOff = -40f;
    [SerializeField] private Color colorOn = Color.white;
    [SerializeField] private Color colorOff = Color.gray;

    [Header("--- Info & Version ---")]
    [SerializeField] private TextMeshProUGUI displayPlayerIdText;
    [SerializeField] private TextMeshProUGUI versionText;

    private void Start()
    {
        RefreshUI();

        if (versionText != null)
            versionText.text = "Ver. " + Application.version;

        if (displayPlayerIdText != null && SaveManager.Instance != null)
            displayPlayerIdText.text = "ID: " + SaveManager.Instance.Data.playerId;
    }

    public void OnProfileDetailsClicked()
    {
        if (SaveManager.Instance == null) return;

        // 1. IDをコピー
        string id = SaveManager.Instance.Data.playerId;
        GUIUtility.systemCopyBuffer = id;
        Debug.Log("<color=cyan>IDをコピーしたお！</color>");

        // 2. 旗を立てる
        shouldOpenProfileOnLoad = true;

        // 3. シーン移動（SceneLoaderと名前を合わせたお！）
        SceneManager.LoadScene("Care");
    }

    // --- 以下、ボタン処理など ---
    public void OnContactClicked() => Application.OpenURL("https://example.com/contact");
    public void OnTermsOfServiceClicked() => Application.OpenURL("https://example.com/terms");
    public void OnAiDataUsageClicked() { if (aiDataPanel != null) aiDataPanel.SetActive(true); }
    public void OnDeleteAccountClicked() { if (deleteConfirmPanel != null) deleteConfirmPanel.SetActive(true); }
    public void OnRestorePurchaseClicked() => Debug.Log("購入情報を復元中...");

    public void RefreshUI()
    {
        if (SaveManager.Instance == null) return;
        var data = SaveManager.Instance.Data;
        if (bgmSlider != null) bgmSlider.value = data.bgmVolume;
        UpdateSwitchVisuals(notifyHandle, notifyBackground, data.isNotificationOn);
        UpdateSwitchVisuals(seHandle, seBackground, data.isSeOn);
        AudioListener.volume = data.bgmVolume;
    }

    public void OnBgmSliderChanged()
    {
        SaveManager.Instance.Data.bgmVolume = bgmSlider.value;
        AudioListener.volume = bgmSlider.value;
        SaveManager.Instance.Save();
    }

    public void ToggleNotification()
    {
        SaveManager.Instance.Data.isNotificationOn = !SaveManager.Instance.Data.isNotificationOn;
        UpdateSwitchVisuals(notifyHandle, notifyBackground, SaveManager.Instance.Data.isNotificationOn);
        SaveManager.Instance.Save();
    }

    public void ToggleSe()
    {
        SaveManager.Instance.Data.isSeOn = !SaveManager.Instance.Data.isSeOn;
        UpdateSwitchVisuals(seHandle, seBackground, SaveManager.Instance.Data.isSeOn);
        SaveManager.Instance.Save();
    }

    private void UpdateSwitchVisuals(RectTransform handle, Image bg, bool isOn)
    {
        if (handle == null || bg == null) return;
        handle.anchoredPosition = new Vector2(isOn ? posOn : posOff, 0);
        bg.color = isOn ? colorOn : colorOff;
    }
}