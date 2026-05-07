using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Core;
using System.Collections.Generic;

public class CareSceneManager : MonoBehaviour
{
    [Header("キャラクター情報")]
    [SerializeField] private TextMeshProUGUI petNameText;
    [SerializeField] private RawImage userIconImage;
    [SerializeField] private RawImage iconFrameImage;

    [Header("コンディション")]
    [SerializeField] private Image conditionIconImage;
    [SerializeField] private TextMeshProUGUI conditionText;

    [Header("ステータスバー Fill Area")]
    [SerializeField] private RectTransform cleanFillArea;
    [SerializeField] private RectTransform hungerFillArea;
    [SerializeField] private RectTransform energyFillArea;
    [SerializeField] private RectTransform moodFillArea;

    [Header("所持金表示")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI lunaStoneText;

    [Header("おやつパネル所持金表示")]
    [SerializeField] private TextMeshProUGUI oyatuCoinText;
    [SerializeField] private TextMeshProUGUI oyatuLunaStoneText;

    [Header("コンディションアイコン")]
    [SerializeField] private Sprite iconSuperGood;
    [SerializeField] private Sprite iconGood;
    [SerializeField] private Sprite iconNormal;
    [SerializeField] private Sprite iconBad;
    [SerializeField] private Sprite iconSuperBad;

    [Header("おやつパネル")]
    [SerializeField] private GameObject oyatuSelectPanel;

    [Header("通知テキスト（コイン不足など）")]
    [SerializeField] private TextMeshProUGUI noticeText;
    [SerializeField] private float noticeDuration = 2f;



    [Header("信頼度")]
    [SerializeField] private TextMeshProUGUI trustLevelText;

    private PetStatus _status;
    private SaveData _save;
    private Coroutine _noticeCoroutine;
    private Coroutine _popupCoroutine;

    private void Start()
    {
        // GameContextがない場合はダミーで動かす
        if (GameContext.Instance != null)
        {
            _status = GameContext.Instance.PetStatus;
        }
        else
        {
            Debug.LogWarning("GameContextがないのでダミーで動作します");
            _status = new PetStatus();
            _status.AddMood(60f);
            _status.AddClean(70f);
            _status.AddEnergy(80f);
            _status.AddHunger(50f);
        }

        // SaveManagerがない場合はダミーで動かす
        if (SaveManager.Instance != null)
        {
            _save = SaveManager.Instance.Data;
        }
        else
        {
            _save = new SaveData();
            _save.petName = "テスト";
        }

        _status.ApplyTimeDecay();
        LoadCharacterInfo();
        RefreshAll();

        if (noticeText != null) noticeText.gameObject.SetActive(false);
        if (oyatuSelectPanel != null) oyatuSelectPanel.SetActive(false);
    }

    public void RefreshAll()
    {
        SetCondition();
        SetStatusBars();
        SetWallet();
        SetTrustLevel();
    }

    private void LoadCharacterInfo()
    {
        if (petNameText != null)
            petNameText.text = _save.petName;

        if (!string.IsNullOrEmpty(_save.iconId))
        {
            Texture icon = Resources.Load<Texture>("SpecialIcon/" + _save.iconId)
                        ?? Resources.Load<Texture>("Icon/" + _save.iconId);
            if (icon != null && userIconImage != null)
                userIconImage.texture = icon;
        }

        if (!string.IsNullOrEmpty(_save.selectedFrameId))
        {
            Texture frame = Resources.Load<Texture>("Frame/" + _save.selectedFrameId);
            if (frame != null && iconFrameImage != null)
                iconFrameImage.texture = frame;
        }
    }

    private void SetWallet()
    {
        if (coinText != null)
            coinText.text = GameData.Instance.Coin.ToString();
        if (lunaStoneText != null)
            lunaStoneText.text = GameData.Instance.LunaStone.ToString();

        // おやつパネル側も同時更新
        if (oyatuCoinText != null)
            oyatuCoinText.text = GameData.Instance.Coin.ToString();
        if (oyatuLunaStoneText != null)
            oyatuLunaStoneText.text = GameData.Instance.LunaStone.ToString();
    }

    private void SetTrustLevel()
    {
        if (trustLevelText == null) return;
        int level = PetStatus.GetTrustLevel(_save.trust);
        trustLevelText.text = $"Lv {level}";
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

    private void SetStatusBars()
    {
        SetFillArea(cleanFillArea, _status.Clean);
        SetFillArea(hungerFillArea, _status.Hunger);
        SetFillArea(energyFillArea, _status.Energy);
        SetFillArea(moodFillArea, _status.Mood);
    }

    private void SetFillArea(RectTransform fillArea, float value)
    {
        if (fillArea == null) return;
        float ratio = Mathf.Clamp01(value / 100f);
        float fullWidth = fillArea.parent.GetComponent<RectTransform>().rect.width;
        fillArea.sizeDelta = new Vector2(fullWidth * ratio, fillArea.sizeDelta.y);
    }

    private void ShowNotice(string message)
    {
        if (noticeText == null) return;
        if (_noticeCoroutine != null) StopCoroutine(_noticeCoroutine);
        _noticeCoroutine = StartCoroutine(ShowTextCoroutine(noticeText, message, noticeDuration));
    }

    private void ShowPopup(List<string> messages)
    {
        StatusPopup.Instance?.Show(messages);
    }

    private System.Collections.IEnumerator ShowTextCoroutine(
        TextMeshProUGUI tmp, string message, float duration)
    {
        tmp.text = message;
        tmp.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        tmp.gameObject.SetActive(false);
    }

    public void OnBtnBath()
    {
        if (!GameData.Instance.UseCoin(30)) { ShowNotice("コインが足りないよ…！"); return; }
        _status.AddClean(40f);
        _status.AddMood(10f);
        _status.AddEnergy(-5f);
        _status.AddTrust(3);
        _status.OnBath();
        GameContext.Instance?.SavePetStatus();
        ShowPopup(new List<string> { "✨ 清潔 +40", "💕 機嫌 +10" });
        RefreshAll();
    }

    public void OnBtnPet()
    {
        if (!GameData.Instance.UseCoin(10)) { ShowNotice("コインが足りないよ…！"); return; }
        _status.AddMood(25f);
        _status.AddEnergy(-5f);
        _status.AddTrust(2);
        GameContext.Instance?.SavePetStatus();
        ShowPopup(new List<string> { "💕 機嫌 +25" });
        QuestManager.Instance?.NotifyNade();
        RefreshAll();
    }

    public void OnBtnPlay()
    {
        if (!GameData.Instance.UseCoin(20)) { ShowNotice("コインが足りないよ…！"); return; }
        _status.AddEnergy(30f);
        _status.AddMood(10f);
        _status.AddHunger(-10f);
        _status.AddTrust(3);
        _status.OnPlayed();
        ShowPopup(new List<string> { "⚡ 元気 +30", "💕 機嫌 +10" });
        RefreshAll();
    }

    public void OnBtnSleep()
    {
        _status.AddEnergy(40f);
        _status.AddMood(10f);
        _status.AddTrust(1);
        GameContext.Instance?.SavePetStatus();
        ShowPopup(new List<string> { "😴 元気 +40", "💕 機嫌 +10" });
        RefreshAll();
    }

    public void OnBtnFood()
    {
        if (oyatuSelectPanel != null)
            oyatuSelectPanel.SetActive(true);
    }

    public void OnBtnFood_Food()
    {
        if (!GameData.Instance.UseCoin(20)) { ShowNotice("コインが足りないよ！"); return; }
        _status.AddHunger(30f);
        _status.AddEnergy(5f);
        _status.AddMood(5f);
        _status.AddTrust(1);
        _status.OnFed();
        ShowPopup(new List<string> { "🍚 空腹 +30", "⚡ 元気 +5" });
        QuestManager.Instance?.NotifyFeed();
        CloseOyatuPanel();
        RefreshAll();
    }

    public void OnBtnFood_Biscuit()
    {
        if (!GameData.Instance.UseCoin(20)) { ShowNotice("コインが足りないよ！"); return; }
        _status.AddHunger(20f);
        _status.AddEnergy(10f);
        _status.AddMood(15f);
        _status.AddTrust(2);
        _status.OnFed();
        GameContext.Instance?.SavePetStatus();
        ShowPopup(new List<string> { "🍪 空腹 +20", "💕 機嫌 +15" });
        QuestManager.Instance?.NotifyFeed();
        CloseOyatuPanel();
        RefreshAll();
    }

    public void OnBtnFood_Jerky()
    {
        if (!GameData.Instance.UseCoin(20)) { ShowNotice("コインが足りないよ！"); return; }
        _status.AddHunger(25f);
        _status.AddEnergy(15f);
        _status.AddMood(10f);
        _status.AddTrust(2);
        _status.OnFed();
        GameContext.Instance?.SavePetStatus();
        ShowPopup(new List<string> { "🥩 空腹 +25", "⚡ 元気 +15" });
        QuestManager.Instance?.NotifyFeed();
        CloseOyatuPanel();
        RefreshAll();
    }

    public void OnBtnFood_Special()
    {
        if (!GameData.Instance.UseLunaStone(50)) { ShowNotice("ルナストーンが足りないよ！"); return; }
        _status.AddHunger(100f);
        _status.AddEnergy(20f);
        _status.AddMood(20f);
        _status.AddTrust(5);
        _status.OnFed();
        GameContext.Instance?.SavePetStatus();
        ShowPopup(new List<string> { "🍽️ 空腹 全回復！", "💕 機嫌 +20" });
        QuestManager.Instance?.NotifyFeed();
        CloseOyatuPanel();
        RefreshAll();
    }

    public void OnBtnFood_BirthdayCake()
    {
        if (!GameData.Instance.UseLunaStone(100)) { ShowNotice("ルナストーンが足りないよ！"); return; }
        _status.AddHunger(100f);
        _status.AddEnergy(30f);
        _status.AddMood(30f);
        _status.AddTrust(15);
        _status.OnFed();
        GameContext.Instance?.SavePetStatus();
        ShowPopup(new List<string> { "🎂 空腹 全回復！", "💕 機嫌 +30", "⭐ 信頼度 大UP！" });
        QuestManager.Instance?.NotifyFeed();
        CloseOyatuPanel();
        RefreshAll();
    }

    public void CloseOyatuPanel()
    {
        if (oyatuSelectPanel != null)
            oyatuSelectPanel.SetActive(false);
    }
}