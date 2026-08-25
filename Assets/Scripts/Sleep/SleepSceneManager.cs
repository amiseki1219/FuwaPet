using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Game.Core;

/// <summary>
/// ねんね画面（Sleep.unity）の進行役。
///
/// 【体験】
///   リモコンの「消灯」を1回押すだけ。あとは演出を見るだけで操作はない（requirements.md §5）。
///
///   押す → ランプが消える → 眠そうな顔＋眠気の泡 →「おやすみ〜」
///        → ネイビーのもやが外から閉じる → Care へ切り替え
///        → Care 側で同じ幕の穴が広がって朝になる（IrisRevealController）
///
///   真っ黒・真っ白のべた塗りは作らない（もやの穴を閉じきらない）。
///
/// 【効果】
///   元気 +50 / 信頼度 +1pt / 12時間に1回（§5 お世話ボタン効果一覧）
///   ※仕様書では「8時間に1回」だが、2026/8/24 に 12時間へ変更した。
///
/// 【なぜボタンを押した瞬間に効果を確定するのか】
///   「途中で抜けても効果を与える」仕様のため。
///   演出の途中でアプリを落とされても、押した時点で保存済みなので効果が残る。
///   クールダウンの起点も押下時刻にしている。完了まで7秒ほどしかないので実質同じで、
///   保存処理を1箇所にまとめられる。
///
/// 【クールダウン判定をこのクラスに置いている理由】
///   お風呂は MaxBathPerDay が CareSceneManager と BathSceneManager の2箇所にあり、
///   requirements.md にも「一元化する」と課題が残っている。同じことを繰り返さないため、
///   ねんねはここ1箇所に置き、Care 側は SleepSceneManager.GetRemainingCooldown() を呼ぶ。
///
/// 【まだ入っていないもの】
///   ・「カチッ」の効果音   → プロジェクトに音源も再生の仕組みも無い。SE 基盤ができてから
///   ・押したときの振動     → 同上
///   どちらも下の OnLightOffClicked() に TODO コメントを置いてある。1〜2行足すだけで入る。
/// </summary>
public class SleepSceneManager : MonoBehaviour
{
    // ── 仕様の値 ──────────────────────────────────────────────────────────────

    /// <summary>ねんねのクールダウン（時間）。Care 側もこれを参照する。</summary>
    public const int CooldownHours = 12;

    private const float EnergyPerSleep = 50f;
    private const int   TrustPerSleep  = 1;

    // ── Care へ持ち帰る情報 ───────────────────────────────────────────────────
    //   BathWashManager.BathJustCompleted と同じ作り。
    //   Care の Start() が拾ってコメントを出し、すぐ false に戻す。

    public static bool  SleepJustCompleted    = false;
    public static float SleepJustEnergyAmount = 0f;

    /// <summary>
    /// ねんね画面から戻ってきたか。Care のアイリス演出を出すかどうかの判断に使う。
    ///
    /// SleepJustCompleted と分けている理由:
    ///   あちらは「効果を与えたか」なので、クールダウン中は立たない。
    ///   演出は効果の有無に関係なく必要なので、別のフラグにしている。
    /// </summary>
    public static bool SleepReturning = false;

    // ── 結線 ──────────────────────────────────────────────────────────────────

    [Header("結線")]
    [Tooltip("リモコンの「消灯」ボタン")]
    [SerializeField] private Button lightOffButton;

    [Tooltip("押したあと、その場でパッと消すもの（案内の吹き出しなど）。空でもよい")]
    [SerializeField] private GameObject[] hideOnStart;

    [Tooltip("押したあと、画面の下へ滑らせて消すもの（リモコン本体など）。\n" +
             "「消灯」ボタンは結線しなくても自動で対象になる")]
    [SerializeField] private RectTransform[] slideOutTargets;

    [Tooltip("リモコンから出る波紋。未結線でも動く（波紋が出ないだけ）")]
    [SerializeField] private RippleEffect ripple;

    // 押してすぐ消すと、ぷにっと戻るアニメも波紋も見えないままリモコンが消えてしまう。
    // 「押した → 波紋が広がった → 少し間があって暗くなる」の順に見せる。
    [Tooltip("波紋が消えてから、部屋が暗くなり始めるまでの間")]
    [SerializeField] private float delayAfterRipple = 1.0f;

    [Tooltip("リモコンを画面の下へ滑らせて消すのにかける時間。0 にすると即消えになる")]
    [SerializeField] private float remoteSlideOutDuration = 0.6f;

    [Tooltip("夜のもや（ネイビー）。中央が透明・外周が不透明の画像を入れる。Raycast Target は OFF")]
    [SerializeField] private RawImage nightVeil;

    [Tooltip("キャラが出てくる場所。ここの子から FaceController と Animator を探す")]
    [SerializeField] private Transform characterAnchor;

    [Tooltip("ぽこ用の表示ルート。ぽこはここの中にモデルがあるので、こちらも渡しておく")]
    [SerializeField] private Transform legacyPokoRoot;

    // ── 消灯 ──────────────────────────────────────────────────────────────────
    //
    // 部屋のランプは家具ごとに持っていて、もようがえで差し替わる。
    // つまり「このランプ」と名指しで結線できない。
    // なので Scene に置いてあるライトを配列で受け取り、まとめて暗くする方式にした。
    //
    // ※キャラ用ライト（CharacterKeyLight など）をここに入れると、
    //   キャラまで真っ暗になって「おやすみ〜」の顔が見えなくなる。入れるかは見た目次第。
    [Header("消灯")]
    [Tooltip("消灯で暗くするライト。ここに入れたものだけ暗くなる")]
    [SerializeField] private Light[] dimLights;

    [Tooltip("消灯にかける時間")]
    [SerializeField] private float lampFadeDuration = 0.3f;

    [Tooltip("消灯後の明るさの倍率。0 で完全に消える。0.15 くらい残すと部屋の形が見える")]
    [Range(0f, 1f)]
    [SerializeField] private float dimTargetRatio = 0f;

    [Header("眠気の演出")]
    [Tooltip("顔の左右から弧を描いて飛ぶ眠気の泡。未結線でも動く（出ないだけ）")]
    [SerializeField] private SleepyBubbleEmitter sleepyBubbles;

    [Tooltip("「おやすみ〜」の吹き出し。未結線でも動く")]
    [SerializeField] private GameObject speechBubble;

    [Tooltip("吹き出しの中の文字。未結線なら文字は差し替えない")]
    [SerializeField] private TextMeshProUGUI speechText;

    [SerializeField] private string speechMessage = "おやすみなさい...";

    [Tooltip("消灯してから吹き出しを出すまでの間")]
    [SerializeField] private float speechDelay = 0.4f;

    [Tooltip("吹き出しが現れるのにかける時間（フェードイン）")]
    [SerializeField] private float speechFadeInDuration = 0.4f;

    [Tooltip("1文字あたりの間")]
    [SerializeField] private float typeCharDelay = 0.08f;

    // 「…」で余韻を出したいので、特定の文字だけ間を長くする。
    // ここに書いた文字が出たときだけ slowCharDelay を使う。
    [Tooltip("ゆっくり出したい文字。ここに書いた文字だけ間が長くなる")]
    [SerializeField] private string slowCharacters = "...．。…";

    [Tooltip("上の文字を出すときの間")]
    [SerializeField] private float slowCharDelay = 0.35f;

    // ── 演出の長さ ────────────────────────────────────────────────────────────

    [Header("演出の長さ（秒）")]
    [Tooltip("吹き出しを読み終わってから、暗転が始まるまでの間")]
    [SerializeField] private float afterSpeechHold = 1.2f;

    [Tooltip("ネイビーのもやが外から中央へ閉じていく時間")]
    [SerializeField] private float fadeToNightDuration = 1.5f;

    [Tooltip("いちばん暗い状態のまま待つ時間")]
    [SerializeField] private float holdNightDuration = 1.5f;

    // ── 表情・アニメーション ──────────────────────────────────────────────────

    [Header("表情・アニメーション")]
    [Tooltip("眠そうな表情のキー。専用のものができたら差し替える。\n" +
             "いま使えるキー: Normal / Fun / SlightHappy / Happy / Sad / Angry / Surprised / Relaxed")]
    [SerializeField] private string sleepyExpressionKey = "Relaxed";

    [Tooltip("目を擦るアニメーションの Trigger 名。空なら何も鳴らさない")]
    [SerializeField] private string sleepyAnimationTrigger = "";

    // ── もやの見え方 ──────────────────────────────────────────────────────────
    //
    // RawImage の UV Rect を使って「丸く閉じる／広がる」を作っている。
    // シェーダーもマテリアルも要らないのがこの方式の利点。
    //
    // 【UV Rect の値の意味】
    //   小さい(0.1) … テクスチャの中央だけを画面全体に拡大 → 中央の穴（透明部分）が画面より大きい
    //   大きい(3.0) … テクスチャを縮小して画面に敷く → 穴が小さく絞られ、外側は端の色で埋まる
    //
    //   ※テクスチャの Wrap Mode を Clamp にしておくこと。
    //     Repeat だと外側に模様が繰り返して破綻する。
    //
    // 【真っ黒・真っ白を作らないための調整箱】
    //   nightUvEnd を上げすぎると穴が閉じきって真っ暗になる。3.0〜3.5 くらいが目安。
    //   morningUvEnd を下げすぎると真っ白になる。0.8 前後が目安。
    [Header("もやの見え方")]
    [Tooltip("夜のもやの色。深いネイビー")]
    [SerializeField] private Color nightColor = new Color(0.106f, 0.129f, 0.251f, 1f); // #1B2140

    [Tooltip("夜のもや・開始時の UV スケール。小さいほど透明")]
    [SerializeField] private float nightUvStart = 0.10f;

    [Tooltip("夜のもや・終了時の UV スケール。穴の直径 ≒ 画面幅 × 0.32 ÷ この値。\n" +
             "Care の IrisRevealController の Uv Closed と同じ値にすること。\n" +
             "ここがズレると、画面が切り替わった瞬間に穴の大きさが飛ぶ")]
    [SerializeField] private float nightUvEnd = 40f;

    [Tooltip("もやを揺らす幅。0 で揺れなし")]
    [SerializeField] private float driftAmplitude = 0.014f;

    [Tooltip("もやの揺れる速さ")]
    [SerializeField] private float driftSpeed = 0.8f;

    [Header("その他")]
    [SerializeField] private string returnSceneName = "Care";

    private bool _started;

    /// <summary>吹き出しをフェードで消すために使う。無ければ実行時に足す。</summary>
    private CanvasGroup _speechGroup;

    // ── クールダウン判定（Care からも呼ばれる） ───────────────────────────────

    /// <summary>
    /// ねんねができるようになるまでの残り時間。0 なら今すぐできる。
    /// lastSleepTicks が壊れている場合（別端末の時計など）は 0 を返して先へ進ませる。
    /// </summary>
    public static TimeSpan GetRemainingCooldown(SaveData save)
    {
        if (save == null || save.lastSleepTicks <= 0) return TimeSpan.Zero;

        DateTime last;
        try
        {
            last = new DateTime(save.lastSleepTicks);
        }
        catch (ArgumentOutOfRangeException)
        {
            Debug.LogWarning($"[Sleep] lastSleepTicks が不正です（{save.lastSleepTicks}）。クールダウンなしとして扱います");
            return TimeSpan.Zero;
        }

        TimeSpan remain = last.AddHours(CooldownHours) - DateTime.Now;
        return remain > TimeSpan.Zero ? remain : TimeSpan.Zero;
    }

    /// <summary>いまねんねができるか。</summary>
    public static bool IsSleepReady(SaveData save) => GetRemainingCooldown(save) <= TimeSpan.Zero;

    // ── 進行 ──────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (nightVeil != null)
        {
            nightVeil.color = nightColor;
            nightVeil.raycastTarget = false;
            IrisRevealController.MakeSquare(nightVeil);
            ApplyVeil(nightVeil, nightUvStart, 0f);
            nightVeil.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[Sleep] nightVeil が未結線です。暗転しません");
        }

        // 吹き出しは押すまで出さない（泡は Play() を呼ぶまで出ないので、ここでは触らない）
        if (speechBubble != null) speechBubble.SetActive(false);

        if (lightOffButton != null)
        {
            lightOffButton.onClick.AddListener(OnLightOffClicked);
        }
        else
        {
            Debug.LogWarning("[Sleep] lightOffButton が未結線です");
        }
    }

    public void OnLightOffClicked()
    {
        if (_started) return;   // 連打よけ
        _started = true;

        // TODO: SE 基盤ができたら「カチッ」をここで鳴らす（SaveData.isSeOn を見る）
        // TODO: 振動もここで入れる

        // ここでは消さない。押せなくするだけ。
        // 消すのは PlaySleepSequence() の中で、波紋が消えたあと
        if (lightOffButton != null) lightOffButton.interactable = false;

        // 波紋は押した瞬間から。ぷにっとと重なって出るのが自然
        ripple?.Play();

        // クールダウン中でも演出は流す。
        // 通常は Care 側で止まるが、Sleep.unity から直接 Play したときはここに来る。
        // このシーンには戻る手段がないので、押しても何も起きない作りにすると詰んでしまう。
        var save = SaveManager.Instance?.Data;
        TimeSpan remain = GetRemainingCooldown(save);

        if (remain > TimeSpan.Zero)
        {
            Debug.LogWarning($"[Sleep] クールダウン中のため効果を与えません（残り {FormatRemain(remain)}）");
        }
        else
        {
            ApplyEffect(save);
        }

        StartCoroutine(PlaySleepSequence());
    }

    /// <summary>
    /// リモコンと案内を画面の下へ滑らせてから消す。
    ///
    /// 落ちる距離は Canvas の高さぶん。解像度が変わっても必ず画面外まで出る。
    /// 動きは「だんだん速く」にしている。すっと引っ込むより、重みがあって手に馴染む。
    /// </summary>
    private IEnumerator SlideOutRemoteCoroutine()
    {
        // 案内などはその場で消す。落とすのはリモコンだけにしたいため
        HideInstantly();

        var targets = new List<RectTransform>();

        if (lightOffButton != null && lightOffButton.transform is RectTransform btnRt)
            targets.Add(btnRt);

        if (slideOutTargets != null)
        {
            foreach (var rt in slideOutTargets)
            {
                if (rt != null && !targets.Contains(rt)) targets.Add(rt);
            }
        }

        if (targets.Count == 0 || remoteSlideOutDuration <= 0f)
        {
            HideRemote();
            yield break;
        }

        // 落とす距離は Canvas の高さ。少し余裕を持たせて確実に画面外へ出す
        float drop = 1500f;
        var canvas = targets[0].GetComponentInParent<Canvas>();
        if (canvas != null && canvas.transform is RectTransform canvasRt)
            drop = canvasRt.rect.height * 1.1f;

        var from = new Vector2[targets.Count];
        for (int i = 0; i < targets.Count; i++) from[i] = targets[i].anchoredPosition;

        float elapsed = 0f;
        while (elapsed < remoteSlideOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / remoteSlideOutDuration);
            float eased = t * t;   // だんだん速く

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] == null) continue;
                targets[i].anchoredPosition = from[i] + Vector2.down * (drop * eased);
            }
            yield return null;
        }

        foreach (var rt in targets)
        {
            if (rt != null) rt.gameObject.SetActive(false);
        }

        // 位置を戻しておく。Scene を抜けるので実害はないが、
        // 途中で作り直したときに座標がずれたままになるのを防ぐ
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] != null) targets[i].anchoredPosition = from[i];
        }
    }

    /// <summary>hideOnStart のものをその場で消す。</summary>
    private void HideInstantly()
    {
        if (hideOnStart == null) return;
        foreach (var go in hideOnStart)
        {
            if (go != null) go.SetActive(false);
        }
    }

    /// <summary>リモコンと案内をまとめて片付ける（滑らせない場合の経路）。</summary>
    private void HideRemote()
    {
        HideInstantly();

        if (lightOffButton != null) lightOffButton.gameObject.SetActive(false);

        if (slideOutTargets == null) return;
        foreach (var rt in slideOutTargets)
        {
            if (rt != null) rt.gameObject.SetActive(false);
        }
    }

    /// <summary>元気・信頼度を足して、クールダウンの起点を記録し、保存する。</summary>
    private void ApplyEffect(SaveData save)
    {
        if (save == null)
        {
            Debug.LogWarning("[Sleep] SaveData が取得できませんでした。効果を反映できません");
            return;
        }

        // 元気は PetStatus（GameContext が DontDestroyOnLoad で保持）を正とする。
        // SaveData を直接書き換えるとメモリ上の値とずれ、後続の SavePetStatus() で上書きされて消える。
        var ctx = GameContext.Instance;

        if (ctx != null)
        {
            ctx.PetStatus.AddEnergy(EnergyPerSleep);
            ctx.PetStatus.AddTrust(TrustPerSleep);
        }
        else
        {
            // Sleep.unity 単独再生時のみここに来る想定
            Debug.LogWarning("[Sleep] GameContext が無いため SaveData へ直接書き込みました。エディタ単独再生時のみ発生する想定です");
            save.energy = Mathf.Clamp(save.energy + EnergyPerSleep, 10f, 100f);
            save.trust += TrustPerSleep;
        }

        save.lastSleepTicks = DateTime.Now.Ticks;

        float energyForLog = ctx != null ? ctx.PetStatus.Energy : save.energy;
        Debug.Log($"<color=#00E5FF>[決定]</color> [Sleep] ねんね完了 元気={energyForLog} (+{EnergyPerSleep}) 信頼度+{TrustPerSleep} 次は{CooldownHours}時間後");

        // 保存は1回だけ。SavePetStatus() が SaveToSave() → SaveManager.Save() まで行う。
        if (ctx != null) ctx.SavePetStatus();
        else             SaveManager.Instance?.Save();

        SleepJustCompleted    = true;
        SleepJustEnergyAmount = EnergyPerSleep;
    }

    private IEnumerator PlaySleepSequence()
    {
        // ① 波紋が消えるまで待つ（ぷにっと戻るのもこの間に終わる）
        float rippleWait = ripple != null ? ripple.TotalDuration : 0f;
        if (rippleWait > 0f) yield return new WaitForSeconds(rippleWait);

        // ② 少し間を置く。ここが「押したあとの余韻」になる
        if (delayAfterRipple > 0f) yield return new WaitForSeconds(delayAfterRipple);

        // ③ リモコンと案内を画面下へ滑らせて片付ける
        yield return SlideOutRemoteCoroutine();

        // ④ 部屋のライトを落とす
        yield return DimLightsCoroutine();

        // ⑤ 眠そうな顔＋眠気の泡
        ApplySleepyLook();
        sleepyBubbles?.Play();

        // ⑥ 「おやすみなさい...」。フェードインしてから1文字ずつ出す
        if (speechDelay > 0f) yield return new WaitForSeconds(speechDelay);
        yield return ShowSpeechCoroutine();

        if (afterSpeechHold > 0f) yield return new WaitForSeconds(afterSpeechHold);

        // ⑦ ネイビーのもやが外から中央へ閉じる。
        //    吹き出しは消さない。もやがそのまま覆いかぶさる
        sleepyBubbles?.StopEmitting();   // 飛んでいる途中の泡はそのまま消えるまで飛ぶ
        yield return VeilCoroutine(nightVeil, nightUvStart, nightUvEnd, fadeToNightDuration);

        // ⑧ 完全に閉じて、そのまま待つ。
        //    UV を上げるだけでは中央に小さな穴が残るので、最後は角を映して塗りつぶす
        IrisRevealController.CloseFully(nightVeil);
        yield return new WaitForSeconds(holdNightDuration);

        // ⑨ 閉じたまま Care へ。続きは Care の IrisRevealController が引き取り、
        //    同じ幕の穴を広げて朝を見せる（2026/8/25 に方式変更）
        GoBack();
    }

    /// <summary>
    /// dimLights に入っているライトを、まとめて lampFadeDuration かけて暗くする。
    ///
    /// 明るさの基準は「押した瞬間の値」を使う。
    /// WindowViewController が Start() で夜の明るさを入れるので、こちらの Start() で
    /// 控えておくと、適用前の値を掴んでしまうことがあるため。
    ///
    /// gameObject.SetActive(false) はしない。WindowViewController が同じライトを
    /// 管理しているので、勝手に消すとあちらの想定と食い違う。intensity だけ触る。
    /// </summary>
    private IEnumerator DimLightsCoroutine()
    {
        if (dimLights == null || dimLights.Length == 0)
        {
            Debug.LogWarning("[Sleep] dimLights が空です。消灯しません");
            yield break;
        }

        int n = dimLights.Length;
        var from = new float[n];
        for (int i = 0; i < n; i++)
            from[i] = dimLights[i] != null ? dimLights[i].intensity : 0f;

        if (lampFadeDuration <= 0f)
        {
            for (int i = 0; i < n; i++)
                if (dimLights[i] != null) dimLights[i].intensity = from[i] * dimTargetRatio;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < lampFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lampFadeDuration);
            for (int i = 0; i < n; i++)
                if (dimLights[i] != null)
                    dimLights[i].intensity = Mathf.Lerp(from[i], from[i] * dimTargetRatio, t);
            yield return null;
        }

        for (int i = 0; i < n; i++)
            if (dimLights[i] != null) dimLights[i].intensity = from[i] * dimTargetRatio;
    }

    /// <summary>
    /// 吹き出しをふわっと出してから、1文字ずつ表示する。
    /// 出したあとは消さない。暗転がそのまま覆いかぶさる。
    /// </summary>
    private IEnumerator ShowSpeechCoroutine()
    {
        if (speechBubble == null) yield break;

        // フェードさせたいので CanvasGroup を用意する。Scene に付いていなければ足す
        _speechGroup = speechBubble.GetComponent<CanvasGroup>();
        if (_speechGroup == null) _speechGroup = speechBubble.AddComponent<CanvasGroup>();

        _speechGroup.alpha = 0f;
        if (speechText != null) speechText.text = "";
        speechBubble.SetActive(true);

        // ① 枠だけ先にふわっと出す
        if (speechFadeInDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < speechFadeInDuration)
            {
                elapsed += Time.deltaTime;
                _speechGroup.alpha = Mathf.Clamp01(elapsed / speechFadeInDuration);
                yield return null;
            }
        }
        _speechGroup.alpha = 1f;

        // ② 1文字ずつ。slowCharacters の文字だけ間を長くして余韻を作る
        if (speechText == null || string.IsNullOrEmpty(speechMessage)) yield break;

        foreach (char c in speechMessage)
        {
            speechText.text += c;

            bool slow = !string.IsNullOrEmpty(slowCharacters) && slowCharacters.IndexOf(c) >= 0;
            float wait = slow ? slowCharDelay : typeCharDelay;
            if (wait > 0f) yield return new WaitForSeconds(wait);
        }
    }

    /// <summary>
    /// 眠そうな表情にして、目を擦るアニメを鳴らす。
    ///
    /// キャラの置き場所は2通りある。
    ///   ぽこ以外 … characterAnchor の下に Prefab が生成される
    ///   ぽこ     … legacyPokoRoot の中に最初から置いてある（CharacterStaticDisplayController 参照）
    /// どちらに居ても見つけられるよう、両方を順に探す。
    /// </summary>
    private void ApplySleepyLook()
    {
        FaceController face = FindIn<FaceController>(characterAnchor) ?? FindIn<FaceController>(legacyPokoRoot);
        if (!string.IsNullOrEmpty(sleepyExpressionKey))
        {
            if (face != null) face.SetExpression(sleepyExpressionKey);
            else Debug.LogWarning("[Sleep] FaceController が見つかりません。表情を変えられません");
        }

        if (!string.IsNullOrEmpty(sleepyAnimationTrigger))
        {
            Animator animator = FindIn<Animator>(characterAnchor) ?? FindIn<Animator>(legacyPokoRoot);
            if (animator != null) animator.SetTrigger(sleepyAnimationTrigger);
            else Debug.LogWarning("[Sleep] Animator が見つかりません");
        }
    }

    private static T FindIn<T>(Transform root) where T : Component
        => root == null ? null : root.GetComponentInChildren<T>(true);

    // ── もやの制御 ────────────────────────────────────────────────────────────

    /// <summary>もやの UV スケールを from → to へ動かす。SmoothStep で入りと終わりを柔らかくする。</summary>
    private IEnumerator VeilCoroutine(RawImage img, float from, float to, float duration)
    {
        if (img == null) yield break;

        if (duration <= 0f)
        {
            ApplyVeil(img, to, Time.time);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            ApplyVeil(img, IrisRevealController.LerpUv(from, to, t), Time.time);
            yield return null;
        }
        ApplyVeil(img, to, Time.time);
    }

    /// <summary>
    /// UV Rect を中心そろえで設定する。
    /// x, y をずらすことで、もやがゆっくり漂って見える（同じ形のまま止まらない）。
    /// </summary>
    private void ApplyVeil(RawImage img, float scale, float time)
        => IrisRevealController.ApplyVeil(img, scale, time, driftAmplitude, driftSpeed);

    /// <summary>
    /// Care へ戻る。
    ///
    /// LoadingManager を通さないのは、Loading 画面が0.85秒ほど割り込んで
    /// アイリス演出が途切れるため。Sleep も Care も軽いので直接切り替える。
    /// </summary>
    private void GoBack()
    {
        SleepReturning = true;   // Care 側がこれを見てアイリスを開く
        SceneManager.LoadScene(returnSceneName);
    }

    /// <summary>残り時間を「3時間20分」の形にする。1時間未満なら「20分」。</summary>
    public static string FormatRemain(TimeSpan remain)
    {
        int hours = (int)remain.TotalHours;
        int minutes = remain.Minutes;
        if (hours > 0) return $"{hours}時間{minutes}分";
        return $"{Mathf.Max(1, minutes)}分";
    }
}
