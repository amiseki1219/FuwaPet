using UnityEngine;

/// <summary>
/// お風呂の「流す」演出で、画面の左上から流れてくる雲（A3）。
///
/// 【使い方】
///   Canvas/WashPanel の子に Image（雲.png）を置き、この Component を付ける。
///   BathWashManager.OnShowerButton() から PlayEnter() が呼ばれる。
///
/// 【なぜ Animation クリップではなくコードなのか】
///   ・「入ってくる → 少し行き過ぎて戻る → ふわふわ揺れ続ける」を数値で調整したいため
///   ・Animator Controller の編集はあみまるさんの担当作業なので、
///     コードで完結させたほうが往復が少ない
///   ・止まったあとの揺れは終わりのないループなので、クリップより式のほうが素直
///
/// 【座標について】
///   RectTransform の anchoredPosition だけを触る。
///   Canvas は 1080x1920 の CanvasScaler 付きなので、ここで書く数値は
///   「1080x1920 のときのピクセル」と思ってよい。端末差は CanvasScaler が吸収する。
///   ★ワールド座標は一切使わない。OverlayCanvas 直下の ParticleSystem のような
///     座標系の食い違いは起きない。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class BathCloudAnimator : MonoBehaviour
{
    [Header("位置（Canvas 1080x1920 基準のピクセル）")]
    [Tooltip("流れ着く先。ここに雲が止まる。\n" +
             "Scene で雲を置きたい位置へ動かし、その Pos X / Pos Y をここへ写すのが早い")]
    [SerializeField] private Vector2 targetPosition = new Vector2(0f, 700f);

    [Tooltip("登場を始める位置。画面の左上・外側。\n" +
             "X を画面の外（マイナス）にしておくと、外から入ってくるように見える")]
    [SerializeField] private Vector2 startPosition = new Vector2(-900f, 900f);

    [Header("登場アニメーション")]
    [Tooltip("登場にかける秒数")]
    [Range(0.2f, 4f)]
    [SerializeField] private float enterDuration = 1.2f;

    [Tooltip("行き過ぎる量（ピクセル）。目標より少し先まで行ってから戻ることで、\n" +
             "直線的な動きに見えなくなる。0 にすると行き過ぎない")]
    [Range(0f, 200f)]
    [SerializeField] private float overshoot = 60f;

    [Tooltip("登場中に少し下から持ち上げる量（ピクセル）。ふわっと浮かび上がって見える")]
    [Range(0f, 300f)]
    [SerializeField] private float riseAmount = 80f;

    [Header("フェードイン")]
    [Tooltip("透明から出すか。CanvasGroup が無ければ自動で付ける")]
    [SerializeField] private bool fadeIn = true;

    [Tooltip("フェードにかける秒数。登場時間より短くすること")]
    [Range(0.05f, 2f)]
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("到着後のゆれ")]
    [Tooltip("上下にゆれる幅（ピクセル）。0 でゆれなし")]
    [Range(0f, 100f)]
    [SerializeField] private float swayAmplitude = 15f;

    [Tooltip("1往復にかける秒数。大きいほどゆっくり")]
    [Range(0.5f, 8f)]
    [SerializeField] private float swayPeriod = 2.5f;

    [Tooltip("左右にもわずかにゆらす幅（ピクセル）。0 で上下だけ")]
    [Range(0f, 100f)]
    [SerializeField] private float swayAmplitudeX = 8f;

    [Header("退散アニメーション（泡が消え切ったあと）")]
    [Tooltip("流れ去る先。画面の右外にしておくと、右へ抜けていくように見える")]
    [SerializeField] private Vector2 exitPosition = new Vector2(1100f, 820f);

    [Tooltip("退散にかける秒数")]
    [Range(0.2f, 4f)]
    [SerializeField] private float exitDuration = 1f;

    [Tooltip("退散のフェードアウトにかける秒数。退散時間より短くすること")]
    [Range(0.05f, 2f)]
    [SerializeField] private float exitFadeDuration = 0.7f;

    // ── 内部状態 ──────────────────────────────────────────────────────────────

    private RectTransform _rect;
    private CanvasGroup   _group;

    /// <summary>登場アニメーションの経過秒。</summary>
    private float _enterElapsed;

    /// <summary>いま登場アニメーション中か。</summary>
    private bool _entering;

    /// <summary>登場が終わって揺れているか。</summary>
    private bool _swaying;

    /// <summary>揺れの位相。到着した瞬間から数える（いきなり途中から揺れないように）。</summary>
    private float _swayTime;

    /// <summary>定位置に着いたときに1回だけ呼ぶ処理。A4 で「雫を降らせ始める」をつなぐ。</summary>
    private System.Action _onArrived;

    /// <summary>いま退散アニメーション中か。</summary>
    private bool _exiting;

    /// <summary>退散アニメーションの経過秒。</summary>
    private float _exitElapsed;

    /// <summary>退散を始めた位置。揺れの途中から抜けても段差が出ないよう、その場の位置を覚える。</summary>
    private Vector2 _exitFrom;

    /// <summary>退散が終わったときに1回だけ呼ぶ処理。A5 で「完了ボタンを出す」をつなぐ。</summary>
    private System.Action _onExitFinished;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();

        if (fadeIn)
        {
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
        }

        // ★Awake の時点で必ず隠す。
        //   Scene 上で active=1 のまま置いておいても、お風呂を始めた瞬間に雲が出ないようにするため。
        HideImmediate();
    }

    // ── 公開 API ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 雲を隠して、登場前の状態に戻す。
    /// お風呂を始めるとき（BathWashManager.Initialize）に呼ぶ。何度呼んでも安全。
    /// </summary>
    public void HideImmediate()
    {
        _entering       = false;
        _swaying        = false;
        _exiting        = false;
        _enterElapsed   = 0f;
        _exitElapsed    = 0f;
        _swayTime       = 0f;
        _onExitFinished = null;
        _onArrived      = null;

        if (_rect  != null) _rect.anchoredPosition = startPosition;
        if (_group != null) _group.alpha = 0f;

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 左上から流れてくる登場アニメーションを始める。
    /// 「流す」ボタンを押したとき（BathWashManager.OnShowerButton）に呼ぶ。
    ///
    /// ★すでに出ているときに呼び直しても、最初からやり直すだけで壊れない。
    /// </summary>
    public void PlayEnter(System.Action onArrived = null)
    {
        gameObject.SetActive(true);

        _onArrived = onArrived;

        _entering     = true;
        _swaying      = false;
        _exiting      = false;
        _enterElapsed = 0f;
        _exitElapsed  = 0f;
        _swayTime     = 0f;

        if (_rect  != null) _rect.anchoredPosition = startPosition;
        if (_group != null) _group.alpha = 0f;

        Debug.Log($"<color=#00E5FF>[決定]</color> [BathCloud] 雲の登場を開始しました start={startPosition} target={targetPosition} 所要={enterDuration}秒");
    }

    /// <summary>登場アニメーションが終わっているか。A4（雫）を降らせ始める合図に使う。</summary>
    public bool HasArrived => _swaying;

    /// <summary>
    /// 雲を画面外へ流して退散させる。泡が消え切ったとき（A4 の終わり）に呼ぶ。
    ///
    /// ★退散が終わったら onFinished が【1回だけ】呼ばれる。
    ///   A5 では、ここに「完了ボタンを出す」処理をつなぐ。
    ///
    /// ★揺れの途中で呼んでも段差が出ないよう、そのときの位置から流し始める。
    /// ★すでに退散中なら何もしない（二重に呼んでも onFinished が2回走らない）。
    /// </summary>
    public void PlayExit(System.Action onFinished = null)
    {
        if (_exiting) return;

        // 出ていないのに退散を頼まれたら、何もせずその場で終わったことにする。
        // 呼び出し側（A5）の流れを止めないため。
        if (!gameObject.activeSelf)
        {
            Debug.LogWarning("[BathCloud] 雲が出ていない状態で PlayExit が呼ばれました。退散はせず、完了処理だけ進めます");
            onFinished?.Invoke();
            return;
        }

        _entering       = false;
        _swaying        = false;
        _exiting        = true;
        _exitElapsed    = 0f;
        _exitFrom       = _rect != null ? _rect.anchoredPosition : targetPosition;
        _onExitFinished = onFinished;

        Debug.Log($"<color=#00E5FF>[決定]</color> [BathCloud] 雲の退散を開始しました from={_exitFrom} to={exitPosition} 所要={exitDuration}秒");
    }

    // ── 毎フレーム ────────────────────────────────────────────────────────────

    private void Update()
    {
        if      (_entering) UpdateEnter();
        else if (_exiting)  UpdateExit();
        else if (_swaying)  UpdateSway();
    }

    /// <summary>
    /// 登場アニメーション。
    ///
    /// 【動きの作り方】
    ///   X … EaseOutBack（目標を少し行き過ぎてから戻る）
    ///   Y … EaseOutCubic（下から持ち上がって、ふわっと止まる）
    ///   X と Y で違うカーブを使うことで、直線に見えなくなる。
    /// </summary>
    private void UpdateEnter()
    {
        _enterElapsed += Time.deltaTime;

        float duration = Mathf.Max(enterDuration, 0.01f);
        float t = Mathf.Clamp01(_enterElapsed / duration);

        // X：行き過ぎてから戻る。LerpUnclamped を使うのは、行き過ぎ（1を超える値）を通すため
        float tx = EaseOutBack(t, overshoot);
        float x  = Mathf.LerpUnclamped(startPosition.x, targetPosition.x, tx);

        // Y：目標より riseAmount ぶん低い所から、目標へ持ち上げる
        float ty    = EaseOutCubic(t);
        float fromY = Mathf.Min(startPosition.y, targetPosition.y - riseAmount);
        float y     = Mathf.Lerp(fromY, targetPosition.y, ty);

        _rect.anchoredPosition = new Vector2(x, y);

        if (_group != null)
        {
            float fd = Mathf.Max(fadeDuration, 0.01f);
            _group.alpha = fadeIn ? Mathf.Clamp01(_enterElapsed / fd) : 1f;
        }

        if (t >= 1f)
        {
            _entering = false;
            _swaying  = true;
            _swayTime = 0f;
            _rect.anchoredPosition = targetPosition;
            if (_group != null) _group.alpha = 1f;

            Debug.Log("<color=#00E5FF>[決定]</color> [BathCloud] 雲が定位置に着きました（ここから揺れ続けます）");

            // ★先に取り出して null にしてから呼ぶ。中で PlayExit されても二重実行にならない
            var cb = _onArrived;
            _onArrived = null;
            cb?.Invoke();
        }
    }

    /// <summary>
    /// 到着後のゆれ。上下と左右で周期をずらして、機械的な往復に見えないようにする。
    /// </summary>
    private void UpdateSway()
    {
        if (swayAmplitude <= 0f && swayAmplitudeX <= 0f) return;

        _swayTime += Time.deltaTime;

        float period = Mathf.Max(swayPeriod, 0.01f);
        float w = Mathf.PI * 2f / period;

        // ★sin(0) = 0 なので、到着位置から段差なく揺れ始まる
        float dy = Mathf.Sin(_swayTime * w)        * swayAmplitude;
        float dx = Mathf.Sin(_swayTime * w * 0.6f) * swayAmplitudeX;   // 周期をずらす

        _rect.anchoredPosition = targetPosition + new Vector2(dx, dy);
    }

    /// <summary>
    /// 退散アニメーション。だんだん速くなりながら画面外へ抜け、同時に薄くなる。
    /// 登場が EaseOut（減速）なので、こちらは EaseIn（加速）にして対比を付けている。
    /// </summary>
    private void UpdateExit()
    {
        _exitElapsed += Time.deltaTime;

        float duration = Mathf.Max(exitDuration, 0.01f);
        float t = Mathf.Clamp01(_exitElapsed / duration);

        _rect.anchoredPosition = Vector2.Lerp(_exitFrom, exitPosition, EaseInCubic(t));

        if (_group != null)
        {
            float fd = Mathf.Max(exitFadeDuration, 0.01f);
            _group.alpha = Mathf.Clamp01(1f - _exitElapsed / fd);
        }

        if (t >= 1f)
        {
            _exiting = false;

            // ★先にコールバックを取り出して null にしてから呼ぶ。
            //   コールバックの中で HideImmediate や PlayEnter が呼ばれても二重実行にならない。
            var cb = _onExitFinished;
            _onExitFinished = null;

            if (_group != null) _group.alpha = 0f;
            gameObject.SetActive(false);

            Debug.Log("<color=#00E5FF>[決定]</color> [BathCloud] 雲が退散しました");

            cb?.Invoke();
        }
    }

    // ── イージング ────────────────────────────────────────────────────────────

    /// <summary>だんだん速くなる。退散に使う。</summary>
    private static float EaseInCubic(float t) => t * t * t;

    /// <summary>だんだん減速して止まる。</summary>
    private static float EaseOutCubic(float t)
    {
        float u = 1f - t;
        return 1f - u * u * u;
    }

    /// <summary>
    /// 目標を少し行き過ぎてから戻る。
    /// overshootPixels は「どれくらい行き過ぎるか」の目安。0 なら EaseOutCubic と同じ動き。
    /// </summary>
    private static float EaseOutBack(float t, float overshootPixels)
    {
        if (overshootPixels <= 0f) return EaseOutCubic(t);

        // 行き過ぎ量をカーブの強さに変換する。数値が大きいほど大きく跳ねる
        float s = Mathf.Clamp(overshootPixels * 0.012f, 0f, 3f);
        float u = t - 1f;
        return u * u * ((s + 1f) * u + s) + 1f;
    }

    // ★このファイルは iOS ビルドに入るため、UnityEditor / #if UNITY_EDITOR は使わない。
    //   位置合わせは Scene ビューで雲を動かし、その Pos X / Pos Y を
    //   Target Position 欄へ手で写す運用にする。
}
