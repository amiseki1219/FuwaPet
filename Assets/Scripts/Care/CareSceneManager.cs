using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Game.Core;

public class CareSceneManager : MonoBehaviour
{
    [Header("キャラクター情報")]
    [SerializeField] private TextMeshProUGUI petNameText;

    [Header("コンディション")]
    [SerializeField] private TextMeshProUGUI conditionText;

    [Header("性格テキスト")]
    [SerializeField] private TextMeshProUGUI personalityLabelText;
    [SerializeField] private TextMeshProUGUI personalityText;

    [Header("ステータスバー Slider")]
    [SerializeField] private Slider cleanSlider;
    [SerializeField] private Slider hungerSlider;
    [SerializeField] private Slider energySlider;
    [SerializeField] private Slider moodSlider;

    [Header("ステータス数値テキスト")]
    [SerializeField] private TextMeshProUGUI moodValueText;
    [SerializeField] private TextMeshProUGUI cleanValueText;
    [SerializeField] private TextMeshProUGUI hungerValueText;
    [SerializeField] private TextMeshProUGUI energyValueText;

    [Header("所持金表示")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI lunaStoneText;

    [Header("通知パネル")]
    [SerializeField] private RectTransform noticePanelRect;
    [SerializeField] private TextMeshProUGUI noticeText;
    [SerializeField] private float noticeDuration = 2f;

    [Header("信頼度")]
    [SerializeField] private Slider trustSlider;
    [SerializeField] private TextMeshProUGUI trustRemainingText;

    [Header("吹き出し")]
    [SerializeField] private TextMeshProUGUI speechBubbleText;
    [SerializeField] private GameObject speechBubbleRoot;

    [Header("ステータスポップアップ")]
    [SerializeField] private StatusPopup cleanPopup;
    [SerializeField] private StatusPopup hungerPopup;
    [SerializeField] private StatusPopup energyPopup;
    [SerializeField] private StatusPopup moodPopup;

    [Header("おやつ")]
    [SerializeField] private OyatuManager oyatuManager;

    [Header("お風呂完了エフェクト")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private GameObject sparkleEffect;

    private const int MaxNadePerDay = 10;
    private const int MaxPlayPerDay = 5;
    private const int MaxBathPerDay = 2;

    private PetStatus _status;
    private SaveData _save;
    private Coroutine _noticeCoroutine;
    private Coroutine _sliderCoroutine;
    private Coroutine _coinCoroutine;
    private Coroutine _lunaStoneCoroutine;
    private Coroutine _typewriterCoroutine;
    private float _originalNoticeX;

    private void OnEnable()
    {
        GameContext.OnAppResumed += OnAppResumed;
    }

    private void OnDisable()
    {
        GameContext.OnAppResumed -= OnAppResumed;
    }

    // 復帰時。減衰と保存は GameContext が済ませているので、Care は表示のやり直しだけ。
    private void OnAppResumed()
    {
        RefreshAll();
    }

    private void Start()
    {
        if (GameContext.Instance != null)
        {
            _status = GameContext.Instance.PetStatus;
        }
        else
        {
            Debug.LogWarning("GameContextがないのでダミーで動作します");
            _status = new PetStatus();
            _status.AddClean(70f);
            _status.AddEnergy(80f);
            _status.AddHunger(50f);
        }

        if (SaveManager.Instance != null)
        {
            _save = SaveManager.Instance.Data;
        }
        else
        {
            _save = new SaveData();
            _save.petName = "テスト";
        }

        // ApplyTimeDecay は起動時は MainUIManager.Start()、復帰時は GameContext が適用するため
        // Care では呼ばない
        LoadCharacterInfo();

        if (noticePanelRect != null)
        {
            _originalNoticeX = noticePanelRect.anchoredPosition.x;
            var pos = noticePanelRect.anchoredPosition;
            pos.x = -Screen.width;
            noticePanelRect.anchoredPosition = pos;
            noticePanelRect.gameObject.SetActive(false);
        }

        RefreshAll();

        if (BathWashManager.BathJustCompleted)
        {
            BathWashManager.BathJustCompleted = false;
            ShowNotice("お風呂完了！清潔 +40 ✨");
            ShowCleanPopup("+40");
            PlayBathCompleteEffect();
        }
    }

    private void PlayBathCompleteEffect()
    {
        if (characterAnimator != null) characterAnimator.SetTrigger("Happy");
        if (sparkleEffect != null)
        {
            sparkleEffect.SetActive(true);
            StartCoroutine(HideSparkleAfterDelay(5f));
        }
    }

    private IEnumerator HideSparkleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (sparkleEffect != null) sparkleEffect.SetActive(false);
    }

    public void RefreshAll()
    {
        SetCondition();
        SetStatusBars();
        SetWallet();
        SetTrustLevel();
        SetPersonality();
        SetSpeechBubble();
    }

    private void LoadCharacterInfo()
    {
        if (petNameText != null)
            petNameText.text = ResolveCharName();
    }

    private string ResolveCharName()
    {
        if (!string.IsNullOrEmpty(_save.petNickname))
            return _save.petNickname;

        string charId = !string.IsNullOrEmpty(_save.selectedCharacterId)
            ? _save.selectedCharacterId
            : _save.characterId;

        return charId switch
        {
            "poko" => "ぽこ",
            "eru"  => "える",
            "koko" => "ここ",
            "paru" => "ぱる",
            "piyoko" => "ぴよこ",
            _      => _save.petName ?? ""
        };
    }

    private void SetWallet()
    {
        if (coinText != null)
        {
            int from = int.TryParse(coinText.text, out int parsed) ? parsed : GameData.Instance.Coin;
            if (_coinCoroutine != null) StopCoroutine(_coinCoroutine);
            _coinCoroutine = StartCoroutine(AnimateCoinText(coinText, from, GameData.Instance.Coin, 0.5f));
        }
        if (lunaStoneText != null)
        {
            int from = int.TryParse(lunaStoneText.text, out int parsed) ? parsed : GameData.Instance.LunaStone;
            if (_lunaStoneCoroutine != null) StopCoroutine(_lunaStoneCoroutine);
            _lunaStoneCoroutine = StartCoroutine(AnimateCoinText(lunaStoneText, from, GameData.Instance.LunaStone, 0.5f));
        }
    }

    private IEnumerator AnimateCoinText(TextMeshProUGUI text, int fromValue, int toValue, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            text.text = Mathf.RoundToInt(Mathf.Lerp(fromValue, toValue, t)).ToString();
            yield return null;
        }
        text.text = toValue.ToString();
    }

    private void SetTrustLevel()
    {
        int trust = _save.trust;

        bool isMax = TrustFormula.IsMaxLevel(trust);
        int remaining = TrustFormula.GetPtsToNextLevel(trust);

        if (isMax || remaining == 0)
        {
            if (trustSlider != null)
                trustSlider.value = 1f;

            if (trustRemainingText != null)
                trustRemainingText.text = "Lv.100 カンスト達成！";
        }
        else
        {
            if (trustSlider != null)
                trustSlider.value = TrustFormula.GetFillAmount(trust);

            if (trustRemainingText != null)
                trustRemainingText.text = $"あと{remaining}ptで信頼度アップ！";
        }
    }

    private void SetCondition()
    {
        float avg = (_status.Hunger + _status.Clean + _status.Energy) / 3f;
        string text;

        if (avg >= 80f)      text = "絶好調！";
        else if (avg >= 60f) text = "元気いっぱい！";
        else if (avg >= 40f) text = "ふつう";
        else if (avg >= 20f) text = "しょんぼり";
        else                 text = "元気ない...";

        if (conditionText != null)
            conditionText.text = $"{ResolveCharName()}は{text}";
    }

    private void SetStatusBars()
    {
        if (moodValueText != null)   moodValueText.text   = $"{(int)_status.Mood}/100";
        if (cleanValueText != null)  cleanValueText.text  = $"{(int)_status.Clean}/100";
        if (hungerValueText != null) hungerValueText.text = $"{(int)_status.Hunger}/100";
        if (energyValueText != null) energyValueText.text = $"{(int)_status.Energy}/100";

        if (_sliderCoroutine != null) StopCoroutine(_sliderCoroutine);
        _sliderCoroutine = StartCoroutine(AnimateSlidersCoroutine());
    }

    private IEnumerator AnimateSlidersCoroutine()
    {
        float startClean  = cleanSlider  != null ? cleanSlider.value  : 0f;
        float startHunger = hungerSlider != null ? hungerSlider.value : 0f;
        float startEnergy = energySlider != null ? energySlider.value : 0f;
        float startMood   = moodSlider   != null ? moodSlider.value   : 0f;

        float elapsed = 0f;
        const float duration = 0.5f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (cleanSlider  != null) cleanSlider.value  = Mathf.Lerp(startClean,  _status.Clean,  t);
            if (hungerSlider != null) hungerSlider.value = Mathf.Lerp(startHunger, _status.Hunger, t);
            if (energySlider != null) energySlider.value = Mathf.Lerp(startEnergy, _status.Energy, t);
            if (moodSlider   != null) moodSlider.value   = Mathf.Lerp(startMood,   _status.Mood,   t);
            yield return null;
        }

        if (cleanSlider  != null) cleanSlider.value  = _status.Clean;
        if (hungerSlider != null) hungerSlider.value = _status.Hunger;
        if (energySlider != null) energySlider.value = _status.Energy;
        if (moodSlider   != null) moodSlider.value   = _status.Mood;
    }

    private void SetPersonality()
    {
        if (personalityLabelText != null)
            personalityLabelText.text = $"{ResolveCharName()}の性格";
        if (personalityText != null)
            personalityText.text = "ふつうの子";
    }

    private void SetSpeechBubble()
    {
        string speech;
        if (_status.Hunger < 40f)      speech = "おなかすいたよ…";
        else if (_status.Clean < 40f)  speech = "お風呂入りたいな…";
        else if (_status.Energy < 40f) speech = "ちょっとつかれたかも…";
        else if (_status.Mood < 40f)   speech = "なんかしょんぼりしてる…";
        else if (_status.Hunger >= 70f && _status.Clean >= 70f &&
                 _status.Energy >= 70f && _status.Mood >= 70f)
                                       speech = "今日も元気だよ！";
        else                           speech = "一緒にいられて嬉しいな";

        if (speechBubbleRoot != null) speechBubbleRoot.SetActive(true);
        if (speechBubbleText != null)
        {
            if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = StartCoroutine(TypewriterCoroutine(speech));
        }
    }

    private IEnumerator TypewriterCoroutine(string message)
    {
        speechBubbleText.text = "";
        foreach (char c in message)
        {
            speechBubbleText.text += c;
            yield return new WaitForSeconds(0.05f);
        }
    }

    public void ShowNotice(string message)
    {
        if (noticePanelRect == null) return;
        if (_noticeCoroutine != null) StopCoroutine(_noticeCoroutine);
        if (noticeText != null) noticeText.text = message;
        _noticeCoroutine = StartCoroutine(SlideNoticeCoroutine());
    }

    private IEnumerator SlideNoticeCoroutine()
    {
        float offScreenX = -Screen.width;
        const float slideTime = 0.3f;

        noticePanelRect.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < slideTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideTime);
            float eased = 1f - (1f - t) * (1f - t);
            noticePanelRect.anchoredPosition = new Vector2(Mathf.Lerp(offScreenX, _originalNoticeX, eased), noticePanelRect.anchoredPosition.y);
            yield return null;
        }
        noticePanelRect.anchoredPosition = new Vector2(_originalNoticeX, noticePanelRect.anchoredPosition.y);

        yield return new WaitForSeconds(noticeDuration);

        elapsed = 0f;
        while (elapsed < slideTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideTime);
            float eased = t * t;
            noticePanelRect.anchoredPosition = new Vector2(Mathf.Lerp(_originalNoticeX, offScreenX, eased), noticePanelRect.anchoredPosition.y);
            yield return null;
        }
        noticePanelRect.anchoredPosition = new Vector2(offScreenX, noticePanelRect.anchoredPosition.y);
        noticePanelRect.gameObject.SetActive(false);
    }

    public void OnBtnBath()
    {
        ResetDailyCountIfNeeded();
        if (_save.bathCountToday >= MaxBathPerDay) { ShowNotice($"今日のお風呂は{MaxBathPerDay}回までだよ！"); return; }
        GoToScene("Bath");
    }

    public void OnBtnPet()
    {
        ResetDailyCountIfNeeded();
        if (_save.nadeCountToday >= MaxNadePerDay) { ShowNotice($"今日のなでなでは{MaxNadePerDay}回までだよ！"); return; }
        _save.nadeCountToday++;
        _status.AddEnergy(3f);
        _status.AddTrust(2);
        GameContext.Instance?.SavePetStatus();
        energyPopup?.Show("+3");
        QuestManager.Instance?.NotifyNade();
        RefreshAll();
    }

    public void OnBtnPlay()
    {
        ResetDailyCountIfNeeded();
        if (_save.playCountToday >= MaxPlayPerDay) { ShowNotice($"今日のあそぶは{MaxPlayPerDay}回までだよ！"); return; }
        if (!GameData.Instance.UseCoin(10)) { ShowNotice("コインが足りないよ…！"); return; }
        _save.playCountToday++;
        _status.AddEnergy(30f);
        _status.AddHunger(-10f);
        _status.OnPlayed();
        _status.AddTrust(3);
        GameContext.Instance?.SavePetStatus();
        energyPopup?.Show("+30");
        RefreshAll();
    }

    public void OnBtnSleep()
    {
        GoToScene("Sleep");
    }

    private void GoToScene(string sceneName)
    {
        if (LoadingManager.Instance != null)
            LoadingManager.Instance.LoadSceneWithLoading(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    public void OnBtnFood()
    {
        oyatuManager?.ShowPanel();
    }

    public void ShowCleanPopup(string text)  => cleanPopup?.Show(text);
    public void ShowHungerPopup(string text) => hungerPopup?.Show(text);
    public void ShowEnergyPopup(string text) => energyPopup?.Show(text);

    private void ResetDailyCountIfNeeded()
    {
        string today = System.DateTime.Now.ToString("yyyy-MM-dd");
        if (_save.lastBathDate != today) { _save.bathCountToday = 0; _save.lastBathDate = today; }
        if (_save.lastNadeDate != today) { _save.nadeCountToday = 0; _save.lastNadeDate = today; }
        if (_save.lastPlayDate != today) { _save.playCountToday = 0; _save.lastPlayDate = today; }
    }
}
