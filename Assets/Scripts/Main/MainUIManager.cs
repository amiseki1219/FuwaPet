using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Core;

public class MainUIManager : MonoBehaviour
{
    [Header("ユーザー情報")]
    [SerializeField] private RawImage userIconImage;
    [SerializeField] private TextMeshProUGUI userNameText;

    [Header("キャラクター情報")]
    [SerializeField] private TextMeshProUGUI petNameText;
    [SerializeField] private TextMeshProUGUI conditionText;
    [SerializeField] private Image conditionIconImage;
    [SerializeField] private TextMeshProUGUI daysTogetherText;

    [Header("信頼度")]
    [SerializeField] private Image trustCircleImage;
    [SerializeField] private TextMeshProUGUI trustLevelText;

    [Header("機嫌ゲージ Fill Area")]
    [SerializeField] private RectTransform moodFillArea;

    [Header("所持金")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI lunaStoneText;

    [Header("コンディションアイコン")]
    [SerializeField] private Sprite iconSuperGood;
    [SerializeField] private Sprite iconGood;
    [SerializeField] private Sprite iconNormal;
    [SerializeField] private Sprite iconBad;
    [SerializeField] private Sprite iconSuperBad;

    [Header("ぽよんメニュー")]
    [SerializeField] private MenuPoyonController careMenu;
    [SerializeField] private MenuPoyonController shopMenu;
    [SerializeField] private MenuPoyonController settingMenu;

    [Header("やることパネル")]
    [SerializeField] private GameObject questPanel;

    private int _currentOpenIndex = -1;
    private PetStatus _status;
    private SaveData _save;

    private void Start()
    {
        if (GameContext.Instance != null)
        {
            _status = GameContext.Instance.PetStatus;
        }
        else
        {
            _status = new PetStatus();
        }

        if (SaveManager.Instance != null)
        {
            _save = SaveManager.Instance.Data;
        }
        else
        {
            _save = new SaveData();
        }

        _status.ApplyTimeDecay();
        RefreshAll();
    }

    public void RefreshAll()
    {
        SetUserInfo();
        SetPetInfo();
        SetWallet();
        SetMoodBar();
        SetTrustCircle();
    }

    // ─── ユーザー情報 ────────────────────────────

    private void SetUserInfo()
    {
        if (userNameText != null)
            userNameText.text = _save.userName;

        if (!string.IsNullOrEmpty(_save.iconId))
        {
            Texture icon = Resources.Load<Texture>("SpecialIcon/" + _save.iconId)
                        ?? Resources.Load<Texture>("Icon/" + _save.iconId);
            if (icon != null && userIconImage != null)
                userIconImage.texture = icon;
        }
    }

    // ─── キャラクター情報 ────────────────────────

    private void SetPetInfo()
    {
        if (petNameText != null)
            petNameText.text = _save.petName;

        // 出会って〇〇日
        if (daysTogetherText != null)
        {
            if (!string.IsNullOrEmpty(_save.startDate) &&
                System.DateTime.TryParse(_save.startDate, out System.DateTime start))
            {
                int days = (System.DateTime.Now - start).Days + 1;
                daysTogetherText.text = $"{days}日";
            }
            else
            {
                daysTogetherText.text = "1日";
            }
        }

        SetCondition();
    }

    private void SetCondition()
    {
        float mood = _status.Mood;
        string text;
        Sprite icon;

        if (mood >= 80f) { text = "絶好調"; icon = iconSuperGood; }
        else if (mood >= 60f) { text = "好調"; icon = iconGood; }
        else if (mood >= 40f) { text = "普通"; icon = iconNormal; }
        else if (mood >= 20f) { text = "不調"; icon = iconBad; }
        else { text = "絶不調"; icon = iconSuperBad; }

        if (conditionText != null) conditionText.text = text;
        if (conditionIconImage != null && icon != null)
            conditionIconImage.sprite = icon;
    }

    // ─── 信頼度円形ゲージ ────────────────────────

    private void SetTrustCircle()
    {
        int trust = _save.trust;
        int level = PetStatus.GetTrustLevel(trust);
        float fill = PetStatus.GetTrustFillAmount(trust);

        if (trustCircleImage != null)
            trustCircleImage.fillAmount = fill;

        if (trustLevelText != null)
            trustLevelText.text = $"{level}";
    }

    // ─── 機嫌ゲージ ──────────────────────────────

    private void SetMoodBar()
    {
        if (moodFillArea == null) return;
        float ratio = Mathf.Clamp01(_status.Mood / 50f);
        float fullWidth = moodFillArea.parent.GetComponent<RectTransform>().rect.width;
        moodFillArea.sizeDelta = new Vector2(fullWidth * ratio, moodFillArea.sizeDelta.y);
    }

    // ─── 所持金 ──────────────────────────────────

    private void SetWallet()
    {
        if (coinText != null)
            coinText.text = GameData.Instance.Coin.ToString();
        if (lunaStoneText != null)
            lunaStoneText.text = GameData.Instance.LunaStone.ToString();
    }

    // ─── ぽよんメニュー ──────────────────────────

    public void OnMainButtonClicked(int index)
    {
        if (index == 1) return; // Chatはシーン遷移

        if (_currentOpenIndex == index)
        {
            CloseAllSubMenus();
            _currentOpenIndex = -1;
            return;
        }

        CloseAllSubMenus();

        switch (index)
        {
            case 0: if (careMenu != null) careMenu.OpenMenu(); break;
            case 2: if (shopMenu != null) shopMenu.OpenMenu(); break;
            case 3: if (settingMenu != null) settingMenu.OpenMenu(); break;
        }

        _currentOpenIndex = index;
    }

    private void CloseAllSubMenus()
    {
        if (careMenu != null) careMenu.CloseMenu();
        if (shopMenu != null) shopMenu.CloseMenu();
        if (settingMenu != null) settingMenu.CloseMenu();
    }

    // ─── やることパネル ──────────────────────────

    public void OnBtnQuest()
    {
        if (questPanel == null) return;
        questPanel.SetActive(true);
    }

    public void OnCloseQuestPanel()
    {
        if (questPanel == null) return;
        questPanel.SetActive(false);
    }


}