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
    [SerializeField] private float statusAnimateDuration = 0.6f;

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

    [Tooltip("ぽこ以外のキャラのアクション。未結線でもよい（アニメが出ないだけ）")]
    [SerializeField] private CareCharacterActionController careCharacterAction;

    private const int MaxNadePerDay = 10;
    private const int MaxPlayPerDay = 5;
    // ★S-4（2026/8/29）：お風呂の上限は BathSceneManager.MaxBathPerDay に一元化した。
    //   ここに同じ値を持つと、片方だけ直したときに Care と Bath で判定が食い違う。
    //   ねんねのクールダウンを SleepSceneManager に寄せているのと同じ考え方。

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
        if (SleepSceneManager.SleepReturning)
        {
            SleepSceneManager.SleepReturning = false;
            _suppressSpeech = true;   // 幕が開くまで吹き出しを出さない
            if (irisReveal != null) irisReveal.PlayReveal();
            else Debug.LogWarning("[Care] irisReveal が未結線なのでアイリス演出は出ません");
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
            ShowNotice($"お風呂完了！{ParamNames.Clean} {ParamNames.Pt(cleanAmount)}");
            ShowCleanPopup(ParamNames.Pt(cleanAmount));
            PlayBathCompleteEffect();
        }

        // ねんねから戻ってきたとき。お風呂と同じ作り（静的フラグを拾ってすぐ戻す）
        //
        // ただしお風呂と違って、すぐには出さない。
        // 戻った直後は画面が幕で覆われていて、通知も吹き出しも見えないため、
        // アイリスが開ききってから出す。
        if (SleepSceneManager.SleepJustCompleted)
        {
            SleepSceneManager.SleepJustCompleted = false;
            int energyAmount = Mathf.RoundToInt(SleepSceneManager.SleepJustEnergyAmount);
            StartCoroutine(ShowSleepResultCoroutine(energyAmount));
        }
        else
        {
            Debug.Log("[Care] SleepJustCompleted が false なので、ねんねの結果表示は出しません");
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

        if (wait > 0f) yield return new WaitForSeconds(wait);

        ShowNotice($"ねんね完了！{ParamNames.Energy} {ParamNames.Pt(energyAmount)}");
        energyPopup?.Show(ParamNames.Pt(energyAmount));

        // ここで初めて吹き出しを出す。
        // 機嫌などの通常の判定は SetSpeechBubble() の中で今までどおり動く
        _suppressSpeech = false;
        _overrideSpeech = PickWakeUpMessage();
        SetSpeechBubble();

        Debug.Log($"<color=#00E5FF>[決定]</color> [Care] ねんねの結果を表示しました 元気+{energyAmount} " +
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
        // ぽこ以外は嬉しいアニメ＋しばらく表情を固定する
        careCharacterAction?.PlayHappy();

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

    /// <summary>
    /// 画面に出すキャラ名を返す。
    /// ★2026/8/28：キャラID→日本語名の対応表が4箇所に散っていたため、
    ///   CharacterNames へ集約した。優先順位（ニックネーム → 日本語名 → petName）は変えていない。
    /// </summary>
    private string ResolveCharName() => CharacterNames.ResolveDisplayName(_save);

    /// <summary>
    /// 画面上部の所持コイン・ルナストーンの表示を最新にする。
    ///
    /// ★2026/8/29：0.5秒かけて数字を動かすアニメーションを削除した（あみまるさんの指示）。
    ///   Bath 側の RefreshWallet() も同じ形にそろえてある。
    /// </summary>
    private void SetWallet()
    {
        if (coinText      != null) coinText.text      = GameData.Instance.Coin.ToString();
        if (lunaStoneText != null) lunaStoneText.text = GameData.Instance.LunaStone.ToString();
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

    /// <summary>ステータスバーを一度でも表示したか。★U-1（2026/8/30）で追加。</summary>
    private bool _statusBarsInitialized;

    /// <summary>
    /// ステータスバーを更新する。
    ///
    /// 【動きの決まり】2026/8/30（U-1）
    ///   ・画面に入った1回目 … アニメーションしない。いまの値をそのまま出す
    ///   ・2回目以降         … 【増えたバーだけ】アニメーションで伸ばす。減ったバーは即反映
    ///   ・お風呂／ねんねから戻ったとき … 例外。増えたぶんを巻き戻してから伸ばす
    ///
    /// 【なぜ1回目をアニメーションしないのか】
    ///   Care.unity のスライダーは初期値 100（満タン）で保存されている。
    ///   そこから現在値へ動かすと「開いた瞬間にゲージが減った」ように見えていた。
    ///   実際には減っていない。罪悪感を煽らない方針（requirements.md 付録A.1）に反するので消した。
    ///
    /// 【なぜ減るときはアニメーションしないのか】
    ///   増えたことを見せるのが目的の演出なので、減る動きを見せる理由がない。
    ///   あそぶ（元気+30・おなか-10）なら、元気だけ伸びて、おなかは即座に減る。
    ///
    /// 数字はここでは入れない。バーと同じコルーチンの中でカウントアップさせる。
    /// （即代入するとバーだけ伸びて数字はパッと切り替わり、ちぐはぐに見える）
    /// </summary>
    private void SetStatusBars()
    {
        if (_sliderCoroutine != null) StopCoroutine(_sliderCoroutine);

        if (!_statusBarsInitialized)
        {
            _statusBarsInitialized = true;
            ApplyStatusBarsImmediate();

            // ★お風呂・ねんねから戻ったときだけ、増えたぶんを巻き戻してアニメーションさせる。
            //   Care は LoadScene で作り直されるので、ここも「画面に入った1回目」になる。
            //   そのまま即反映にすると「お風呂完了！キレイ ＋60pt」と出ているのに
            //   ゲージはもう伸び切っている、という食い違いが起きる。
            //   ★この時点では静的フラグはまだ落ちていない（Start の後半で拾って落とす）ので読める。
            //     フラグを【消さないこと】。消すと結果のポップアップが出なくなる。
            if (!TryRewindForCareResult()) return;

            _sliderCoroutine = StartCoroutine(AnimateSlidersCoroutine());
            return;
        }

        // 2回目以降。増えたバーが1本も無いなら、アニメーションせずに即反映して終わり
        if (!AnySliderWillIncrease())
        {
            ApplyStatusBarsImmediate();
            return;
        }

        // 減った・変わらないバーだけ先に確定させる。残り（増えたバー）はコルーチンが伸ばす
        SnapNonIncreasingSliders();
        _sliderCoroutine = StartCoroutine(AnimateSlidersCoroutine());
    }

    /// <summary>バーと数字に現在値をそのまま入れる。アニメーションの最後でも使う。</summary>
    private void ApplyStatusBarsImmediate()
    {
        if (cleanSlider  != null) cleanSlider.value  = _status.Clean;
        if (hungerSlider != null) hungerSlider.value = _status.Hunger;
        if (energySlider != null) energySlider.value = _status.Energy;
        if (moodSlider   != null) moodSlider.value   = _status.Mood;

        SetStatusValueTexts(_status.Mood, _status.Clean, _status.Hunger, _status.Energy);
    }

    /// <summary>そのバーがこれから増えるか。誤差で動かないよう、わずかな差は無視する。</summary>
    private static bool WillIncrease(Slider slider, float target)
        => slider != null && target > slider.value + 0.01f;

    private bool AnySliderWillIncrease()
        => WillIncrease(cleanSlider,  _status.Clean)
        || WillIncrease(hungerSlider, _status.Hunger)
        || WillIncrease(energySlider, _status.Energy)
        || WillIncrease(moodSlider,   _status.Mood);

    /// <summary>増えないバー（減った・変わらない）を、先に現在値へ合わせてしまう。</summary>
    private void SnapNonIncreasingSliders()
    {
        if (cleanSlider  != null && !WillIncrease(cleanSlider,  _status.Clean))  cleanSlider.value  = _status.Clean;
        if (hungerSlider != null && !WillIncrease(hungerSlider, _status.Hunger)) hungerSlider.value = _status.Hunger;
        if (energySlider != null && !WillIncrease(energySlider, _status.Energy)) energySlider.value = _status.Energy;
        if (moodSlider   != null && !WillIncrease(moodSlider,   _status.Mood))   moodSlider.value   = _status.Mood;
    }

    /// <summary>
    /// お風呂・ねんねから戻ってきたときだけ、対象のバーを「お世話する前」の位置まで巻き戻す。
    /// 巻き戻したら true を返す（＝このあとアニメーションさせる）。
    ///
    /// ★気分は 清潔・おなか・元気 の平均（PetStatus.Mood）なので、
    ///   清潔と元気を戻したぶんの 1/3 だけ一緒に戻す。
    /// ★下限は 10（PetStatus のパラメータ下限）。
    /// </summary>
    private bool TryRewindForCareResult()
    {
        float cleanBack  = BathWashManager.BathJustCompleted
            ? BathWashManager.BathJustCleanAmount : 0f;
        float energyBack = SleepSceneManager.SleepJustCompleted
            ? SleepSceneManager.SleepJustEnergyAmount : 0f;

        if (cleanBack <= 0f && energyBack <= 0f) return false;

        if (cleanSlider != null && cleanBack > 0f)
            cleanSlider.value = Mathf.Max(10f, _status.Clean - cleanBack);

        if (energySlider != null && energyBack > 0f)
            energySlider.value = Mathf.Max(10f, _status.Energy - energyBack);

        if (moodSlider != null)
            moodSlider.value = Mathf.Max(10f, _status.Mood - (cleanBack + energyBack) / 3f);

        Debug.Log($"<color=#00E5FF>[決定]</color> [Care] お世話の結果ぶんだけゲージを巻き戻してから伸ばします" +
                  $"（{ParamNames.Clean} -{cleanBack} / 元気 -{energyBack}）");
        return true;
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

        // ★最後は必ず現在値でそろえる。同じ処理を2箇所に書かないよう1メソッドに寄せてある
        ApplyStatusBarsImmediate();
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

        // ★D-2（2026/8/29）：毎回「root=OK text=OK」と出していたログをやめ、
        //   結線が抜けているときだけ知らせる形にした。正常時は何も出さない。
        if (speechBubbleRoot == null || speechBubbleText == null)
        {
            Debug.LogWarning($"[Care] 吹き出しの結線が足りません root={(speechBubbleRoot != null ? "OK" : "★未結線")} " +
                             $"text={(speechBubbleText != null ? "OK" : "★未結線")}");
        }

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
        // ★S-3（2026/8/31）：ここは「読むだけ」。日付を書き換えないので、
        //   Bath 画面へ行って何もせず戻っても「今日入浴した」ことにならない。
        if (DailyCounters.BathToday(_save) >= BathSceneManager.MaxBathPerDay) { ShowNotice($"今日のお風呂は{BathSceneManager.MaxBathPerDay}回までだよ！"); return; }
        GoToScene("Bath");
    }

    public void OnBtnPet()
    {
        if (DailyCounters.NadeToday(_save) >= MaxNadePerDay) { ShowNotice($"今日のなでなでは{MaxNadePerDay}回までだよ！"); return; }
        DailyCounters.ConsumeNade(_save);   // ★S-3：実際になでたときだけ回数と日付が進む
        _status.AddEnergy(3f);
        _status.AddTrust(2);
        GameContext.Instance?.SavePetStatus();
        energyPopup?.Show("+3");
        QuestManager.Instance?.NotifyNade();
        RefreshAll();
    }

    public void OnBtnPlay()
    {
        if (DailyCounters.PlayToday(_save) >= MaxPlayPerDay) { ShowNotice($"今日のあそぶは{MaxPlayPerDay}回までだよ！"); return; }
        // ★2026/8/31：あそぶを無料化した（10🪙 → FREE）。
        //   あそぶ由来のコイン報酬はパズルクリアのみに一本化したため、
        //   ここにあったコイン消費（UseCoin(10)）を削除している。
        DailyCounters.ConsumePlay(_save);   // ★S-3：実際にあそんだときだけ回数と日付が進む
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
}
