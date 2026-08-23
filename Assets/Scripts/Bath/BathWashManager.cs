using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class BathWashManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public static bool BathJustCompleted = false;
    /// <summary>直前のお風呂で回復した清潔値。Care 画面の完了表示が参照する。</summary>
    public static float BathJustCleanAmount = 0f;
    private class ShampooData
    {
        public string id;
        public string displayName;
        public string imageName;
        public string description;
    }

    private static readonly List<ShampooData> AllShampoo = new List<ShampooData>
    {
        new ShampooData { id = "normal",    displayName = "せっけん",          imageName = "NomalImage",   description = "さっぱりやさしい泡立ち。\n毎日使えるシンプルなせっけん"          },
        new ShampooData { id = "ichigo",    displayName = "いちごシャンプー",   imageName = "IchigoImage",  description = "ふんわり甘くてかわいい香り。\n使うたびに甘えん坊になっちゃう？"   },
        new ShampooData { id = "hoshizora", displayName = "ほしぞらシャンプー", imageName = "HoshiImage",   description = "星空みたいな神秘的な香り。\nコツコツがんばる気持ちが芽生えるかも" },
        new ShampooData { id = "rainbow",   displayName = "レインボーせっけん", imageName = "RainbowImage", description = "7色の泡があふれだす！\nどんな変化が起きるかはおたのしみ♪"         },
    };

    // お風呂1回あたりの信頼度加点。requirements.md §5「お世話ボタン効果一覧」で +3pt と確定している。
    private const int TrustPerBath = 3;

    [Header("こすり設定")]
    [SerializeField] private float requiredDistancePerScrub = 80f;
    [SerializeField] private int maxScrubCount = 24;
    [SerializeField] private Vector2 particleOffset = Vector2.zero;
    [SerializeField] private RectTransform scrubArea;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI percentText;
    [SerializeField] private TextMeshProUGUI rubCountText;
    [SerializeField] private Slider gaugeSlider;
    [SerializeField] private RawImage shampooIcon;
    [SerializeField] private TextMeshProUGUI shampooNameText;
    [SerializeField] private TextMeshProUGUI shampooDescriptionText;
    [SerializeField] private GameObject hintText;
    [SerializeField] private GameObject completeButton;

    [Header("タッチエフェクト")]
    [SerializeField] private BathTouchEffect touchEffect;

    [Header("手のカーソル")]
    [SerializeField] private RectTransform handCursor;

    [Header("泡 (BubbleGroupの子を順番に登録)")]
    [SerializeField] private BubbleController[] bubbles;

    private int _scrubCount;
    private bool _isComplete;
    private bool _inputBlocked;
    private bool _isDragging;
    private Vector2 _lastTouchPos;
    private float _accumulatedDistance;
    private string _shampooId;
    private System.Collections.IEnumerator _sliderCoroutine;

    // Screen Space Camera 対応：scrubArea 判定に使うカメラ
    private Camera _canvasCamera;

    // ── ライフサイクル ────────────────────────────────────────────────────────

    private void Awake()
    {
        _canvasCamera = ResolveCanvasCamera();

        var canvas = GetComponentInParent<Canvas>();
        Debug.Log($"[BathWash] Awake: canvas={canvas?.name} renderMode={canvas?.renderMode} 使用カメラ={_canvasCamera?.name ?? "null"}");
    }

    /// <summary>
    /// scrubArea の座標変換に使うカメラを決める。判断をここ1箇所に集約している。
    ///
    /// なぜ Render Mode を見るのか:
    ///   Unity は Canvas の Render Mode を Screen Space - Overlay に変えても、
    ///   Render Camera の参照（worldCamera）を消さない。Inspector 上は欄が隠れるだけで、
    ///   内部には Screen Space - Camera 時代のカメラが残り続ける。
    ///   その状態で worldCamera をそのまま渡すと「カメラ空間の Canvas」として変換され、
    ///   ScreenPointToLocalPointInRectangle の結果が大きくズレる。
    ///   → 画面のどこを触っても scrubArea の範囲外と判定され、一切こすれなくなる。
    ///
    ///   2026/8/23: お風呂画面を Orthographic → Perspective に作り替え、Canvas を
    ///   Screen Space - Camera → Overlay へ変更した際に、この不具合として表面化した。
    ///
    /// Overlay のときは必ず null を渡すのが正解。
    /// </summary>
    private Camera ResolveCanvasCamera()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return null;

        // Overlay ではカメラを使わない（残っている参照を無視する）
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;

        return canvas.worldCamera;
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    // ── 初期化 ────────────────────────────────────────────────────────────────

    public void Initialize(string shampooId)
    {
        _shampooId           = shampooId;
        _scrubCount          = 0;
        _isComplete          = false;
        _inputBlocked        = true;
        _isDragging          = false;
        _accumulatedDistance = 0f;

        // canvas camera を再取得（Awake後に別 Canvas に移動した場合のため）
        _canvasCamera = ResolveCanvasCamera();

        StartCoroutine(UnblockInputNextFrame());

        if (completeButton != null) completeButton.SetActive(false);
        if (hintText != null) hintText.SetActive(true);
        if (handCursor != null) handCursor.gameObject.SetActive(false);

        Debug.Log($"[BathWash] Initialize: shampooId={shampooId} canvasCamera={_canvasCamera?.name ?? "null"} scrubArea={scrubArea?.name ?? "null"}");

        UpdateUI();
        ResetBubbles();
        UpdateShampooInfo(shampooId);

        // シャンプー別に泡の色を切り替える（requirements.md §5）
        touchEffect?.SetShampoo(shampooId);
    }

    private System.Collections.IEnumerator UnblockInputNextFrame()
    {
        yield return null;
        _inputBlocked = false;
        Debug.Log("[BathWash] Input unblocked");
    }

    // ── ドラッグ開始・終了 ────────────────────────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"[BathWash] OnPointerDown pos={eventData.position} isComplete={_isComplete} scrubCount={_scrubCount} blocked={_inputBlocked}");

        if (_isComplete || _scrubCount >= maxScrubCount || _inputBlocked) return;

        _isDragging          = true;
        _lastTouchPos        = eventData.position;
        _accumulatedDistance = 0f;

        bool inArea = IsInScrubArea(eventData.position);
        Debug.Log($"[BathWash] OnPointerDown: inArea={inArea} cam={_canvasCamera?.name ?? "null"}");

        UpdateFollowEffects(eventData.position, show: inArea);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log($"[BathWash] OnPointerUp pos={eventData.position} scrubCount={_scrubCount}");
        _isDragging = false;
        UpdateFollowEffects(eventData.position, show: false);
    }

    // ── こすり判定（毎フレーム） ───────────────────────────────────────────────

    private void Update()
    {
        if (!_isDragging || _isComplete || _inputBlocked) return;

        Vector2 currentPos = Vector2.zero;
        bool hasInput = false;

        if (Touch.activeTouches.Count > 0)
        {
            currentPos = Touch.activeTouches[0].screenPosition;
            hasInput = true;
        }
        else if (Mouse.current?.leftButton.isPressed == true)
        {
            currentPos = Mouse.current.position.ReadValue();
            hasInput = true;
        }

        if (!hasInput)
        {
            _isDragging = false;
            UpdateFollowEffects(Vector2.zero, show: false);
            return;
        }

        bool inArea = IsInScrubArea(currentPos);

        Debug.Log($"[BathWash] Update: pos={currentPos} inArea={inArea} scrub={_scrubCount}");

        // ① エフェクトは scrubArea 内のときだけ表示する
        UpdateFollowEffects(currentPos, show: inArea);

        float dist = Vector2.Distance(currentPos, _lastTouchPos);
        _lastTouchPos = currentPos;

        if (!inArea) return;

        // ② こすり距離が閾値を超えたらカウント
        _accumulatedDistance += dist;
        if (_accumulatedDistance >= requiredDistancePerScrub)
        {
            _accumulatedDistance -= requiredDistancePerScrub;
            _scrubCount++;
            Debug.Log($"[BathWash] ★ scrubCount++ = {_scrubCount}");
            UpdateUI();
            UpdateBubbles(_scrubCount);

            if (_scrubCount >= maxScrubCount)
                OnWashComplete();
        }
    }

    // ── エフェクト追従（毎フレーム共通処理） ──────────────────────────────────

    // show=true : 表示して位置更新 / show=false : 非表示にする
    private void UpdateFollowEffects(Vector2 screenPos, bool show)
    {
        // HandCursor は OverlayCanvas（ScreenSpaceOverlay）にあるため camera=null で変換
        if (handCursor != null)
        {
            handCursor.gameObject.SetActive(show);
            if (show)
            {
                var parentRect = handCursor.parent as RectTransform;
                if (parentRect != null)
                {
                    Vector2 local;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        parentRect, screenPos, null, out local);
                    handCursor.anchoredPosition = local;
                }
            }
        }

        // Particle：show=true で連続放出、false で停止（emission.enabled のみ制御）
        if (show)
            touchEffect?.StartContinuous(screenPos);
        else
            touchEffect?.StopContinuous();
    }

    // ── scrubArea 判定（Screen Space Camera 対応） ─────────────────────────────

    private bool IsInScrubArea(Vector2 screenPos)
    {
        if (scrubArea == null)
        {
            Debug.LogWarning("[BathWash] IsInScrubArea: scrubArea is NULL → returning true");
            return true;
        }
        // RectangleContainsScreenPoint は orthographic camera + CanvasScaler 環境で誤判定することがある
        // ScreenPointToLocalPointInRectangle でローカル座標に変換してから rect 判定する
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            scrubArea, screenPos, _canvasCamera, out localPos);
        bool result = scrubArea.rect.Contains(localPos);
        Debug.Log($"[BathWash] IsInScrubArea: screen={screenPos} cam={_canvasCamera?.name ?? "null"} local={localPos} rect={scrubArea.rect} result={result}");
        return result;
    }

    // ── UI更新 ────────────────────────────────────────────────────────────────

    private void UpdateUI()
    {
        float pct = (float)_scrubCount / maxScrubCount * 100f;
        Debug.Log($"[BathWash] UpdateUI: scrubCount={_scrubCount} pct={pct:F1}%");
        if (percentText  != null) percentText.text  = $"{Mathf.RoundToInt(pct)}%";
        if (rubCountText != null) rubCountText.text = $"あと {maxScrubCount - _scrubCount} 回";  // _scrubCount は 0→max へ増えるので、残り回数は引き算で出す
        if (gaugeSlider  != null)
        {
            float target = (float)_scrubCount / maxScrubCount;
            if (_sliderCoroutine != null) StopCoroutine(_sliderCoroutine);
            _sliderCoroutine = AnimateSliderCoroutine(gaugeSlider, gaugeSlider.value, target, 0.3f);
            StartCoroutine(_sliderCoroutine);
        }
    }

    private System.Collections.IEnumerator AnimateSliderCoroutine(Slider slider, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            slider.value = Mathf.Lerp(from, to, t);
            yield return null;
        }
        slider.value = to;
        _sliderCoroutine = null;
    }

    // ── 泡ステージ管理 ────────────────────────────────────────────────────────
    // bubbles配列の順番:
    // [0]Head_01 [1]Head_02 [2]Head_03 [3]Ear_L [4]Ear_R
    // [5]Body_01 [6]Body_02 [7]Body_03 [8]Body_04 [9]Tail

    private void UpdateBubbles(int scrubCount)
    {
        int stage = scrubCount / 6;
        switch (stage)
        {
            case 0:
                for (int i = 0; i < bubbles.Length; i++) SafeHide(i);
                break;
            case 1:
                for (int i = 0; i < 5  && i < bubbles.Length; i++) SafeShow(i, 0.15f);
                for (int i = 5; i < bubbles.Length; i++) SafeHide(i);
                break;
            case 2:
                for (int i = 0; i < bubbles.Length; i++) SafeShow(i, 0.15f);
                break;
            case 3:
                for (int i = 0; i < 5  && i < bubbles.Length; i++) SafeShow(i, 0.15f);
                for (int i = 5; i < 9  && i < bubbles.Length; i++) SafeHide(i);
                if (bubbles.Length > 9) SafeShow(9, 0.15f);
                break;
            default: // stage 4 (24回)
                for (int i = 0; i < bubbles.Length; i++) SafePop(i);
                break;
        }
    }

    private void SafeShow(int i, float size) { if (bubbles[i] != null) bubbles[i].Show(size); }
    private void SafeHide(int i)             { if (bubbles[i] != null) bubbles[i].Hide(); }
    private void SafePop(int i)              { if (bubbles[i] != null) bubbles[i].PopEffect(); }

    private void ResetBubbles()
    {
        foreach (var b in bubbles)
        {
            if (b == null) continue;
            b.transform.localScale = Vector3.zero;
        }
    }

    private void UpdateShampooInfo(string shampooId)
    {
        var data = AllShampoo.Find(s => s.id == shampooId);
        if (data == null) return;

        if (shampooNameText        != null) shampooNameText.text        = data.displayName;
        if (shampooDescriptionText != null) shampooDescriptionText.text = data.description;
        if (shampooIcon != null)
        {
            var tex = Resources.Load<Texture2D>($"BathItemUI/{data.imageName.Normalize(System.Text.NormalizationForm.FormC)}");
            if (tex == null) tex = Resources.Load<Texture2D>($"BathItemUI/{data.imageName.Normalize(System.Text.NormalizationForm.FormD)}");
            if (tex == null) tex = Resources.Load<Texture2D>($"BathItemUI/{data.imageName}");
            shampooIcon.texture = tex;
        }
    }

    // ── ボタンハンドラ ────────────────────────────────────────────────────────

    public void OnSkip()
    {
        _scrubCount = maxScrubCount;
        UpdateUI();
        OnWashComplete();
    }

    private void OnWashComplete()
    {
        _isComplete = true;
        _isDragging = false;
        touchEffect?.StopContinuous();
        if (hintText != null) hintText.SetActive(false);
        if (completeButton != null) completeButton.SetActive(true);
        Debug.Log("[BathWash] WashComplete!");
    }

    public void OnComplete()
    {
        var save = SaveManager.Instance?.Data;
        if (save == null) return;

        // 清潔値は PetStatus（GameContext が DontDestroyOnLoad で保持）を正とする。
        // SaveData を直接書き換えるとメモリ上の値とずれ、後続の SavePetStatus() で上書きされて消える。
        var ctx = Game.Core.GameContext.Instance;

        // 2つの経路で同じ値を使うため、回復量は1回だけ求めて使い回す。
        float cleanAmount = GetCleanAmount();

        if (ctx != null)
        {
            ctx.PetStatus.AddClean(cleanAmount);
            ctx.PetStatus.AddTrust(TrustPerBath);   // 信頼度 +3pt（§5）。保存は下の SavePetStatus() がまとめて行う
            ctx.PetStatus.OnBath();   // 最終入浴時刻（表情の放置日数判定が参照）
        }
        else
        {
            // Bath.unity には GameContext が無いため、単独再生時のみここに来る。
            Debug.LogWarning("[OnComplete] GameContext が無いため清潔値を SaveData へ直接書き込んだ。エディタ単独再生時のみ発生する想定。");
            save.clean = Mathf.Clamp(save.clean + cleanAmount, 0f, 100f);
            save.trust += TrustPerBath;   // 単独再生時は PetStatus を経由できないので SaveData へ直接
        }

        ResetBathCountIfNewDay(save);
        save.bathCountToday++;
        save.lastBathDate = System.DateTime.Now.ToString("yyyy-MM-dd");

        ApplyPersonality(save);

        float cleanForLog = ctx != null ? ctx.PetStatus.Clean : save.clean;
        Debug.Log($"[OnComplete] clean={cleanForLog} cleanAmount={cleanAmount} bathCountToday={save.bathCountToday} lastBathDate={save.lastBathDate} shampooId={_shampooId} activity={save.personalityActivity} dependency={save.personalityDependency} diligence={save.personalityDiligence} honesty={save.personalityHonesty} sensitivity={save.personalitySensitivity}");

        // 保存は1回だけ。SavePetStatus() が SaveToSave() → SaveManager.Save() まで行う。
        if (ctx != null) ctx.SavePetStatus();
        else             SaveManager.Instance.Save();

        BathJustCompleted = true;
        BathJustCleanAmount = cleanAmount;
        SceneManager.LoadScene("Care");
    }

    // ── プライベートヘルパー ──────────────────────────────────────────────────

    // シャンプー別の清潔回復量（requirements.md §16）。未知のIDは せっけん と同じ +40 にフォールバックする。
    private float GetCleanAmount()
    {
        switch (_shampooId)
        {
            case "ichigo":
            case "hoshizora":
            case "rainbow":
                return 60f;
            default:
                return 40f;
        }
    }

    private void ApplyPersonality(SaveData save)
    {
        switch (_shampooId)
        {
            case "ichigo":
                save.personalityDependency = Mathf.Clamp(save.personalityDependency + 2, -100, 100);
                break;
            case "hoshizora":
                save.personalityDiligence = Mathf.Clamp(save.personalityDiligence + 2, -100, 100);
                break;
            case "rainbow":
                int idx = Random.Range(0, 5);
                switch (idx)
                {
                    case 0: save.personalityActivity    = Mathf.Clamp(save.personalityActivity    + 1, -100, 100); break;
                    case 1: save.personalityDependency  = Mathf.Clamp(save.personalityDependency  + 1, -100, 100); break;
                    case 2: save.personalityDiligence   = Mathf.Clamp(save.personalityDiligence   + 1, -100, 100); break;
                    case 3: save.personalityHonesty     = Mathf.Clamp(save.personalityHonesty     + 1, -100, 100); break;
                    case 4: save.personalitySensitivity = Mathf.Clamp(save.personalitySensitivity + 1, -100, 100); break;
                }
                break;
        }
    }

    private void ResetBathCountIfNewDay(SaveData save)
    {
        string today = System.DateTime.Now.ToString("yyyy-MM-dd");
        if (save.lastBathDate != today)
            save.bathCountToday = 0;
    }
}
