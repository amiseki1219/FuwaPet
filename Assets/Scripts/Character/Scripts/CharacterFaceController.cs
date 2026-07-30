using System;
using System.Collections;
using UnityEngine;
using Game.Core;

/// <summary>
/// 5キャラ共通の表情制御。FaceExpressionDatabase の表情キーで目・口・ほおを切り替える。
/// Poko 既存の FaceController / PokoFaceController とは併存させ、段階移行する。
/// Renderer / Texture が未結線でも例外を出さない（Piyoko は口が無い運用のため）。
/// </summary>
public class CharacterFaceController : MonoBehaviour
{
    // ── 表情キー ────────────────────────────────────────────────
    public const string KeyNormal    = "Normal";
    public const string KeyHappy     = "Happy";
    public const string KeySad       = "Sad";
    public const string KeyAngry     = "Angry";
    public const string KeyShy       = "Shy";
    public const string KeyFun       = "Fun";
    public const string KeySurprised = "Surprised";
    public const string KeyClose     = "Close";
    public const string KeyRelaxed   = "Relaxed";

    // ── レンダラー参照 ──────────────────────────────────────────
    [Header("フェイスレンダラー")]
    [SerializeField] private Renderer eyeLRenderer;
    [SerializeField] private Renderer eyeRRenderer;
    /// <summary>口が無いキャラ（Piyoko）は未結線のままにする。</summary>
    [SerializeField] private Renderer mouthRenderer;

    [Header("ほお（キャラ単位の固定表示）")]
    [SerializeField] private GameObject cheekL;
    [SerializeField] private GameObject cheekR;
    /// <summary>true なら Start で常時表示にし、以後変更しない。Paru / Koko / Piyoko が true。</summary>
    [SerializeField] private bool cheekAlwaysVisible = false;

    [Header("表情データ")]
    [SerializeField] private FaceExpressionDatabase database;
    [SerializeField] private string defaultExpressionKey = KeyNormal;

    [Header("設定")]
    /// <summary>テクスチャを差し替えるシェーダープロパティ名。</summary>
    [SerializeField] private string baseMapPropertyName = "_BaseMap";
    /// <summary>お世話直後の一時表情を維持する秒数。</summary>
    [SerializeField] private float tempDuration = 2.5f;

    // ── 内部状態 ────────────────────────────────────────────────
    private int _baseMapId;

    private string _overrideExpression;
    private Coroutine _tempCoroutine;

    private PetStatus _status;
    private SaveData  _save;

    // まばたき復元用
    private Texture2D _currentEyeL;
    private Texture2D _currentEyeR;

    /// <summary>現在適用中の表情キー（CharacterBlinkController が参照）。</summary>
    public string CurrentExpressionKey { get; private set; } = KeyNormal;

    // ── Unity ライフサイクル ─────────────────────────────────────
    private void Awake()
    {
        string prop = string.IsNullOrEmpty(baseMapPropertyName) ? "_BaseMap" : baseMapPropertyName;
        _baseMapId = Shader.PropertyToID(prop);
    }

    private void Start()
    {
        _status = GameContext.Instance?.PetStatus;
        _save   = SaveManager.Instance?.Data;

        // ほおはキャラ単位の固定。以後 SetExpression では変更しない。
        ApplyCheekFixedVisibility();

        CurrentExpressionKey = string.IsNullOrEmpty(defaultExpressionKey) ? KeyNormal : defaultExpressionKey;
        RefreshExpression();
    }

    // ── 公開 API ────────────────────────────────────────────────

    /// <summary>表情を固定表示する。ResetToAuto() を呼ぶまで維持される。</summary>
    public void SetExpression(string key)
    {
        _overrideExpression = key;
        StopTempRoutine();
        ApplyExpression(key);
    }

    /// <summary>固定表示を解除し、自動判定に戻す。</summary>
    public void ResetToAuto()
    {
        _overrideExpression = null;
        StopTempRoutine();
        RefreshExpression();
    }

    /// <summary>自動判定を再評価して適用する。固定表示中は固定側を維持する。</summary>
    public void RefreshExpression()
    {
        ApplyExpression(_overrideExpression ?? EvaluateExpression());
    }

    /// <summary>
    /// お世話アクション直後に呼ぶ。
    /// actionType: "eat" → Surprised、それ以外 → Happy。tempDuration 秒後に自動へ戻る。
    /// </summary>
    public void TriggerCareAction(string actionType)
    {
        string key = actionType == "eat" ? KeySurprised : KeyHappy;
        StopTempRoutine();
        _tempCoroutine = StartCoroutine(TempExpressionRoutine(key));
    }

    /// <summary>
    /// AI が返す感情ラベルから表情を決める。
    /// requirements.md §5「基本はAIが返す感情ラベル、状態悪い時は状態優先」に従う。
    /// 現時点で呼び出し元は無い（Chat の AI 連携が未実装）。
    /// </summary>
    public void SetEmotionFromAI(string emotion)
    {
        // 状態が悪いときは AI ラベルを捨てて状態判定を優先する
        string key = IsConditionBad() ? EvaluateExpression() : MapAiEmotion(emotion);

        _overrideExpression = key;
        StopTempRoutine();
        ApplyExpression(key);
    }

    /// <summary>まばたき用。目だけを一時的に上書きする。</summary>
    public void SetEyes(Texture2D eyeL, Texture2D eyeR)
    {
        ApplyTexture(eyeLRenderer, eyeL);
        ApplyTexture(eyeRRenderer, eyeR);
    }

    /// <summary>まばたき後に現在表情の目へ復元する。</summary>
    public void RestoreCurrentExpressionEyes()
    {
        ApplyTexture(eyeLRenderer, _currentEyeL);
        ApplyTexture(eyeRRenderer, _currentEyeR);
    }

    // ── 内部ロジック ────────────────────────────────────────────

    private void StopTempRoutine()
    {
        if (_tempCoroutine == null) return;
        StopCoroutine(_tempCoroutine);
        _tempCoroutine = null;
    }

    private IEnumerator TempExpressionRoutine(string key)
    {
        ApplyExpression(key);
        yield return new WaitForSeconds(tempDuration);
        _tempCoroutine = null;
        if (_overrideExpression == null) RefreshExpression();
    }

    /// <summary>ほおはキャラ単位の固定。FaceExpressionData.showCheek は参照しない。</summary>
    private void ApplyCheekFixedVisibility()
    {
        if (cheekL != null) cheekL.SetActive(cheekAlwaysVisible);
        if (cheekR != null) cheekR.SetActive(cheekAlwaysVisible);
    }

    /// <summary>ステータスに基づいて表情キーを決定する（優先度順）。</summary>
    private string EvaluateExpression()
    {
        int days = CalcDaysSinceLastInteraction();

        // 優先度 1: 4日以上放置 → Angry
        if (days >= 4) return KeyAngry;
        // 優先度 2: ちょうど3日放置 → Sad
        if (days == 3) return KeySad;

        if (_status == null) return KeyNormal;

        float avg = (_status.Hunger + _status.Clean + _status.Energy + _status.Mood) / 4f;

        // 優先度 4: 4パラ平均 70以上 → Happy
        if (avg >= 70f) return KeyHappy;

        // 優先度 5: 元気・空腹 30以上 かつ 気分 60以上 → Fun
        if (_status.Energy >= 30f && _status.Hunger >= 30f && _status.Mood >= 60f) return KeyFun;

        // 優先度 6: 空腹 or 元気 30未満、または 4パラ平均 30未満 → Sad
        if (_status.Hunger < 30f || _status.Energy < 30f || avg < 30f) return KeySad;

        // 優先度 7: デフォルト → Normal
        return KeyNormal;
    }

    /// <summary>
    /// 「状態が悪い」判定。放置3日以上 または 空腹 30未満 または 元気 30未満 または 4パラ平均 30未満。
    /// </summary>
    private bool IsConditionBad()
    {
        if (CalcDaysSinceLastInteraction() >= 3) return true;
        if (_status == null) return false;

        float avg = (_status.Hunger + _status.Clean + _status.Energy + _status.Mood) / 4f;
        return _status.Hunger < 30f || _status.Energy < 30f || avg < 30f;
    }

    /// <summary>
    /// 今日から見た「最後のログイン/お世話」の経過日数。
    /// SaveData.lastDate を基準に、PetStatus の各ケア時刻の最新で補正する。
    /// </summary>
    private int CalcDaysSinceLastInteraction()
    {
        if (_save == null) return 0;
        // GameContext が未初期化のシーンでは評価しない
        if (_status == null) return 0;

        DateTime last = DateTime.Today;

        if (!string.IsNullOrEmpty(_save.lastDate) &&
            DateTime.TryParse(_save.lastDate, out DateTime parsed))
        {
            last = parsed.Date;
        }

        DateTime latestCare = _status.LastFedTime.Date;
        if (_status.LastBathTime.Date > latestCare) latestCare = _status.LastBathTime.Date;
        if (_status.LastPlayTime.Date > latestCare) latestCare = _status.LastPlayTime.Date;

        if (latestCare > last) last = latestCare;

        return Math.Max(0, (DateTime.Today - last).Days);
    }

    /// <summary>AI の感情ラベル → 表情キー。大文字小文字は無視。未知は Normal。</summary>
    private string MapAiEmotion(string emotion)
    {
        if (string.IsNullOrEmpty(emotion)) return KeyNormal;

        switch (emotion.Trim().ToLowerInvariant())
        {
            case "happy":  return KeyHappy;
            case "sad":    return KeySad;
            case "shy":    return KeyShy;
            case "angry":  return KeyAngry;
            case "normal": return KeyNormal;
            default:       return KeyNormal;
        }
    }

    private void ApplyExpression(string key)
    {
        string resolvedKey = string.IsNullOrEmpty(key) ? KeyNormal : key;

        FaceExpressionData data = database?.GetExpression(resolvedKey);

        // DB に無いキーは Normal にフォールバック
        if (data == null && resolvedKey != KeyNormal)
        {
            resolvedKey = KeyNormal;
            data = database?.GetExpression(resolvedKey);
        }
        if (data == null) return;

        CurrentExpressionKey = resolvedKey;
        _currentEyeL = data.leftEyeTexture;
        _currentEyeR = data.rightEyeTexture;

        ApplyTexture(eyeLRenderer,  data.leftEyeTexture);
        ApplyTexture(eyeRRenderer,  data.rightEyeTexture);
        ApplyTexture(mouthRenderer, data.mouthTexture);

        // ほおは表情では変更しない（キャラ単位の固定）

        Debug.Log($"[Character] 表情確定: {resolvedKey} ({name})");
    }

    /// <summary>
    /// MaterialPropertyBlock でテクスチャを差し替える。
    /// r.material を直接触ると Material が複製されるため使わない。
    /// </summary>
    private void ApplyTexture(Renderer r, Texture2D tex)
    {
        if (r == null || tex == null) return;

        var mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        mpb.SetTexture(_baseMapId, tex);
        r.SetPropertyBlock(mpb);
    }
}
