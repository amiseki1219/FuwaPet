using System;
using System.Collections;
using UnityEngine;
using Game.Core;

/// <summary>
/// ぽこちゃんの表情制御。Main / Care / Bath シーンで共通利用できる。
/// Inspector で各表情の Renderer と Texture2D を設定すること。
/// </summary>
public class FaceController : MonoBehaviour
{
    // ── 表情データ定義 ──────────────────────────────────────────
    [Serializable]
    private class ExpressionData
    {
        public Texture2D eyeL;
        public Texture2D eyeR;
        public Texture2D mouth;
        public bool cheekOn;
    }

    // ── レンダラー参照 ──────────────────────────────────────────
    [Header("フェイスレンダラー")]
    [SerializeField] private Renderer eyeLRenderer;
    [SerializeField] private Renderer eyeRRenderer;
    [SerializeField] private Renderer mouthRenderer;
    /// <summary>ほっぺオブジェクト。ON/OFF で SetActive する。</summary>
    [SerializeField] private GameObject cheekObject;

    // ── 表情テクスチャセット ────────────────────────────────────
    [Header("表情テクスチャ（Inspector でテクスチャを設定）")]
    [SerializeField] private ExpressionData normal;
    [SerializeField] private ExpressionData fun;
    [SerializeField] private ExpressionData slightHappy;
    [SerializeField] private ExpressionData happy;
    [SerializeField] private ExpressionData sad;
    [SerializeField] private ExpressionData angry;
    [SerializeField] private ExpressionData surprised;
    [SerializeField] private ExpressionData relaxed;

    [Header("設定")]
    /// <summary>お世話直後の一時表情を維持する秒数（2〜3秒）。</summary>
    [SerializeField] private float tempDuration = 2.5f;

    // ── 内部状態 ────────────────────────────────────────────────
    private string _overrideExpression;
    private Coroutine _tempCoroutine;

    private PetStatus _status;
    private SaveData  _save;

    // まばたき復元用（PokoBlinkController から参照される）
    private Texture2D _currentEyeL;
    private Texture2D _currentEyeR;

    /// <summary>現在適用中の表情キー（PokoBlinkController が参照）。</summary>
    public string CurrentExpressionKey { get; private set; } = "Normal";

    // ── Unity ライフサイクル ─────────────────────────────────────
    private void Start()
    {
        _status = GameContext.Instance?.PetStatus;
        _save   = SaveManager.Instance?.Data;
        RefreshExpression();
    }

    // ── 公開 API ────────────────────────────────────────────────

    /// <summary>
    /// お世話アクション直後に呼ぶ。
    /// actionType: "eat" → Surprised、それ以外 → Happy
    /// tempDuration 秒後に自動判定に戻る。
    /// </summary>
    public void TriggerCareAction(string actionType)
    {
        string key = actionType == "eat" ? "Surprised" : "Happy";
        if (_tempCoroutine != null) StopCoroutine(_tempCoroutine);
        _tempCoroutine = StartCoroutine(TempExpressionRoutine(key));
    }

    /// <summary>
    /// 外部から表情を直接指定する（Bath シーンなどが使う）。
    /// ResetToAuto() を呼ぶまで固定される。
    /// </summary>
    public void SetExpression(string expressionKey)
    {
        _overrideExpression = expressionKey;
        if (_tempCoroutine != null)
        {
            StopCoroutine(_tempCoroutine);
            _tempCoroutine = null;
        }
        ApplyExpression(expressionKey);
    }

    /// <summary>SetExpression() による固定を解除し、自動判定に戻す。</summary>
    public void ResetToAuto()
    {
        _overrideExpression = null;
        if (_tempCoroutine != null)
        {
            StopCoroutine(_tempCoroutine);
            _tempCoroutine = null;
        }
        RefreshExpression();
    }

    // ── 内部ロジック ────────────────────────────────────────────

    private IEnumerator TempExpressionRoutine(string key)
    {
        ApplyExpression(key);
        yield return new WaitForSeconds(tempDuration);
        _tempCoroutine = null;
        if (_overrideExpression == null) RefreshExpression();
    }

    private void RefreshExpression()
    {
        ApplyExpression(EvaluateExpression());
    }

    /// <summary>ステータスに基づいて表情キーを決定する（優先度順）。</summary>
    private string EvaluateExpression()
    {
        int days = CalcDaysSinceLastInteraction();

        // 優先度 1: 4日以上放置（「3日以上」= >3日 の意味として区別）→ Angry
        if (days >= 4) return "Angry";
        // 優先度 2: ちょうど3日放置 → Sad
        if (days == 3) return "Sad";

        if (_status == null) return "Normal";

        float avg = (_status.Hunger + _status.Clean + _status.Energy + _status.Mood) / 4f;

        // 優先度 5: 全パラ平均 70以上 → Happy
        if (avg >= 70f) return "Happy";

        // 優先度 6: 元気・空腹 30以上 かつ 気分 60以上 → Fun
        if (_status.Energy >= 30f && _status.Hunger >= 30f && _status.Mood >= 60f) return "Fun";

        // 優先度 7/8: 空腹 or 元気 30未満、全パラ平均 30未満 → Sad
        if (_status.Hunger < 30f || _status.Energy < 30f || avg < 30f) return "Sad";

        // 優先度 9: デフォルト → Normal
        return "Normal";
    }

    /// <summary>
    /// 今日から見た「最後のログイン/お世話」の経過日数。
    /// SaveData.lastDate を基準に、PetStatus の各ケア時刻の最新で補正する。
    /// </summary>
    private int CalcDaysSinceLastInteraction()
    {
        if (_save == null) return 0;
        // GameContextが未初期化（MainシーンでGameContextが存在しない場合など）は評価しない
        if (_status == null) return 0;

        DateTime last = DateTime.Today;

        if (!string.IsNullOrEmpty(_save.lastDate) &&
            DateTime.TryParse(_save.lastDate, out DateTime parsed))
        {
            last = parsed.Date;
        }

        if (_status != null)
        {
            DateTime fed  = _status.LastFedTime.Date;
            DateTime bath = _status.LastBathTime.Date;
            DateTime play = _status.LastPlayTime.Date;

            DateTime latestCare = fed;
            if (bath > latestCare) latestCare = bath;
            if (play > latestCare) latestCare = play;

            if (latestCare > last) last = latestCare;
        }

        return Math.Max(0, (DateTime.Today - last).Days);
    }

    /// <summary>PokoBlinkController から呼ばれる。目だけを一時的に上書きする。</summary>
    public void SetEyes(Texture2D eyeL, Texture2D eyeR)
    {
        SetTex(eyeLRenderer, eyeL);
        SetTex(eyeRRenderer, eyeR);
    }

    /// <summary>PokoBlinkController から呼ばれる。まばたき後に現在表情の目を復元する。</summary>
    public void RestoreCurrentExpressionEyes()
    {
        SetTex(eyeLRenderer, _currentEyeL);
        SetTex(eyeRRenderer, _currentEyeR);
    }

    private void ApplyExpression(string key)
    {
        ExpressionData data = GetData(key) ?? GetData("Normal");
        if (data == null) return;

        CurrentExpressionKey = key;
        _currentEyeL = data.eyeL;
        _currentEyeR = data.eyeR;

        SetTex(eyeLRenderer,  data.eyeL);
        SetTex(eyeRRenderer,  data.eyeR);
        SetTex(mouthRenderer, data.mouth);

        if (cheekObject != null) cheekObject.SetActive(data.cheekOn);
    }

    private void SetTex(Renderer r, Texture2D tex)
    {
        if (r == null || tex == null) return;
        r.material.mainTexture = tex;
    }

    private ExpressionData GetData(string key) => key switch
    {
        "Normal"      => normal,
        "Fun"         => fun,
        "SlightHappy" => slightHappy,
        "Happy"       => happy,
        "Sad"         => sad,
        "Angry"       => angry,
        "Surprised"   => surprised,
        "Relaxed"     => relaxed,
        _             => normal,
    };
}
