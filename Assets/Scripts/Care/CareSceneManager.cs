using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Game.Core;

public class CareSceneManager : MonoBehaviour
{
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

    // バーと数字は同じ時間で動かす。ここを長くすると、数字がゆっくりカウントアップする
    [Tooltip("ステータスバーと数字が今の値まで動くのにかかる時間")]
    [SerializeField] private float statusAnimateDuration = 1.2f;

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

    [Header("ねんねからの復帰演出")]
    [Tooltip("Sleep から戻ったときに、閉じた幕の穴を広げて朝を見せる。未結線でも動く（演出が出ないだけ）")]
    [SerializeField] private IrisRevealController irisReveal;

    // アイリスが開ききるのを待つと間延びする。開いている途中で出したほうが自然。
    // マイナスにすると「開ききるまで待つ」（IrisRevealController の長さに自動で追従する）。
    [Tooltip("ねんねから戻って、通知・Popup・吹き出しを出すまでの待ち時間。\n" +
             "マイナスにすると、アイリスが開ききるまで待つ")]
    [SerializeField] private float sleepResultDelay = 2f;

    [Tooltip("ねんねから戻ったときの吹き出し。この中からランダムで1つ出る。増やしてよい")]
    [SerializeField] private string[] wakeUpMessages =
    {
        "おはよう！",
        "たくさん寝れたね！",
        "どんな夢を見た？",
        "よく眠れたみたい〜",
        "すっきりした！",
    };

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

    /// <summary>次の SetSpeechBubble() で優先して出す文言。出したら空に戻す。</summary>
    private string _overrideSpeech;

    /// <summary>
    /// true の間は吹き出しを出さない。
    /// ねんね明けは幕が開くまで隠しておき、開いたところで「おはよう」系をタイプライターで出す。
    /// 隠さないと、幕の裏で通常のコメントが出て、開いたあとに差し替わる二段階になってしまう。
    /// </summary>
    private bool _suppressSpeech;
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

    // ねんねから戻ってきたときは、Care が見える前に幕を出しておく。
    // Start でやると、他のコンポーネントの初期化順によっては一瞬中身が見えてしまう。
    // フラグはここでは消さない。下の Start() が通知を出すときに消す。
    private void Awake()
    {
        // 効果の有無に関わらず、ねんねから戻ったら演出は出す。
        // SleepJustCompleted はクールダウン中に立たないので、こちらを見る
        Debug.Log($"[Care][確認用] Awake SleepReturning={SleepSceneManager.SleepReturning} " +
                  $"SleepJustCompleted={SleepSceneManager.SleepJustCompleted} " +
                  $"irisReveal={(irisReveal != null ? irisReveal.name : "★未結線")}");

        if (SleepSceneManager.SleepReturning)
        {
            SleepSceneManager.SleepReturning = false;
            _suppressSpeech = true;   // 幕が開くまで吹き出しを出さない
            if (irisReveal != null) irisReveal.PlayReveal();
            else Debug.LogWarning("[Care][確認用] irisReveal が未結線なのでアイリス演出は出ません");
        }
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
            int cleanAmount = Mathf.RoundToInt(BathWashManager.BathJustCleanAmount);
            ShowNotice($"お風呂完了！清潔 +{cleanAmount}");
            ShowCleanPopup($"+{cleanAmount}");
            PlayBathCompleteEffect();
        }

        // ねんねから戻ってきたとき。お風呂と同じ作り（静的フラグを拾ってすぐ戻す）
        //
        // ただしお風呂と違って、すぐには出さない。
        // 戻った直後は画面が幕で覆われていて、通知も吹き出しも見えないため、
        // アイリスが開ききってから出す。
        Debug.Log($"[Care][確認用] Start SleepJustCompleted={SleepSceneManager.SleepJustCompleted} " +
                  $"energyAmount={SleepSceneManager.SleepJustEnergyAmount}");

        if (SleepSceneManager.SleepJustCompleted)
        {
            SleepSceneManager.SleepJustCompleted = false;
            int energyAmount = Mathf.RoundToInt(SleepSceneManager.SleepJustEnergyAmount);
            StartCoroutine(ShowSleepResultCoroutine(energyAmount));
        }
        else
        {
            Debug.LogWarning("[Care][確認用] SleepJustCompleted が false なので、ねんねの結果表示は出しません");
        }
    }

    /// <summary>
    /// ねんねの結果を、アイリスが開ききってから出す。
    /// 幕がまだ閉じている間に出しても見えないので、そのぶん待つ。
    /// </summary>
    private IEnumerator ShowSleepResultCoroutine(int energyAmount)
    {
        float wait = sleepResultDelay >= 0f
            ? sleepResultDelay
            : (irisReveal != null ? irisReveal.TotalDuration : 0f);

        Debug.Log($"[Care][確認用] ねんねの結果表示を {wait} 秒後に出します");
        if (wait > 0f) yield return new WaitForSeconds(wait);

        ShowNotice($"ねんね完了！元気 +{energyAmount}");
        energyPopup?.Show($"+{energyAmount}");

        // ここで初めて吹き出しを出す。
        // 機嫌などの通常の判定は SetSpeechBubble() の中で今までどおり動く
        _suppressSpeech = false;
        _overrideSpeech = PickWakeUpMessage();
        SetSpeechBubble();

        Debug.Log($"<color=#00E5FF>[決定]</color> [Care][確認用] ねんねの結果を表示しました 元気+{energyAmount} " +
                  $"notice={(noticePanelRect != null ? "OK" : "★未結線")} " +
                  $"energyPopup={(energyPopup != null ? "OK" : "★未結線")} " +
                  $"吹き出し={(speechBubbleText != null ? "OK" : "★未結線")}");
    }

    /// <summary>ねんね明けの言葉を1つ選ぶ。未設定なら既定の1文。</summary>
    private string PickWakeUpMessage()
    {
        if (wakeUpMessages == null || wakeUpMessages.Length == 0) return "おはよう！";
        return wakeUpMessages[Random.Range(0, wakeUpMessages.Length)];
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
        // 数字はここでは入れない。バーと同じコルーチンの中でカウントアップさせる。
        // （即代入するとバーだけ伸びて数字はパッと切り替わり、ちぐはぐに見える）
        if (_sliderCoroutine != null) StopCoroutine(_sliderCoroutine);
        _sliderCoroutine = StartCoroutine(AnimateSlidersCoroutine());
    }

    /// <summary>
    /// バーと数字を、今の値まで一緒に動かす。
    ///
    /// 数字をバーと同じ時間で動かしているので、
    /// statusAnimateDuration を長くすると「カウントアップしている」感じが強くなる。
    /// バーの開始位置は今表示されている値。値が減るときも同じように動く。
    /// </summary>
    private IEnumerator AnimateSlidersCoroutine()
    {
        float startClean  = cleanSlider  != null ? cleanSlider.value  : _status.Clean;
        float startHunger = hungerSlider != null ? hungerSlider.value : _status.Hunger;
        float startEnergy = energySlider != null ? energySlider.value : _status.Energy;
        float startMood   = moodSlider   != null ? moodSlider.value   : _status.Mood;

        float duration = Mathf.Max(0f, statusAnimateDuration);

        if (duration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float clean  = Mathf.Lerp(startClean,  _status.Clean,  t);
                float hunger = Mathf.Lerp(startHunger, _status.Hunger, t);
                float energy = Mathf.Lerp(startEnergy, _status.Energy, t);
                float mood   = Mathf.Lerp(startMood,   _status.Mood,   t);

                if (cleanSlider  != null) cleanSlider.value  = clean;
                if (hungerSlider != null) hungerSlider.value = hunger;
                if (energySlider != null) energySlider.value = energy;
                if (moodSlider   != null) moodSlider.value   = mood;

                SetStatusValueTexts(mood, clean, hunger, energy);

                yield return null;
            }
        }

        if (cleanSlider  != null) cleanSlider.value  = _status.Clean;
        if (hungerSlider != null) hungerSlider.value = _status.Hunger;
        if (energySlider != null) energySlider.value = _status.Energy;
        if (moodSlider   != null) moodSlider.value   = _status.Mood;

        SetStatusValueTexts(_status.Mood, _status.Clean, _status.Hunger, _status.Energy);
    }

    /// <summary>数字の表示だけを書き換える。切り上げではなく切り捨てで、バーの見た目と揃える。</summary>
    private void SetStatusValueTexts(float mood, float clean, float hunger, float energy)
    {
        if (moodValueText != null)   moodValueText.text   = $"{(int)mood}/100";
        if (cleanValueText != null)  cleanValueText.text  = $"{(int)clean}/100";
        if (hungerValueText != null) hungerValueText.text = $"{(int)hunger}/100";
        if (energyValueText != null) energyValueText.text = $"{(int)energy}/100";
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
        // ねんね明けは幕が開くまで黙っている
        if (_suppressSpeech)
        {
            if (speechBubbleRoot != null) speechBubbleRoot.SetActive(false);
            return;
        }

        string speech;
        // ねんね明けなど、状態に関係なく出したい言葉があるときはそちらを優先する
        if (!string.IsNullOrEmpty(_overrideSpeech)) { speech = _overrideSpeech; _overrideSpeech = null; }
        else if (_status.Hunger < 40f) speech = "おなかすいたよ…";
        else if (_status.Clean < 40f)  speech = "お風呂入りたいな…";
        else if (_status.Energy < 40f) speech = "ちょっとつかれたかも…";
        else if (_status.Mood < 40f)   speech = "なんかしょんぼりしてる…";
        else if (_status.Hunger >= 70f && _status.Clean >= 70f &&
                 _status.Energy >= 70f && _status.Mood >= 70f)
                                       speech = "今日も元気だよ！";
        else                           speech = "一緒にいられて嬉しいな";

        Debug.Log($"[Care][確認用] 吹き出し「{speech}」 root={(speechBubbleRoot != null ? "OK" : "★未結線")} text={(speechBubbleText != null ? "OK" : "★未結線")}");

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
        // クールダウンの判定は SleepSceneManager に一元化している（定数を2箇所に持たないため）
        var remain = SleepSceneManager.GetRemainingCooldown(_save);
        if (remain > System.TimeSpan.Zero)
        {
            ShowNotice($"ねんねはあと{SleepSceneManager.FormatRemain(remain)}後だよ！");
            return;
        }
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
