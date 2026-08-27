using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using Yurufu.Bath.Foam;

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
    [SerializeField] private Slider gaugeSlider;
    [SerializeField] private RawImage shampooIcon;
    [SerializeField] private TextMeshProUGUI shampooNameText;
    [SerializeField] private TextMeshProUGUI shampooDescriptionText;
    [SerializeField] private GameObject hintText;
    [SerializeField] private GameObject completeButton;

    [Header("UI（A2 で追加）")]
    [Tooltip("あわあわゲージ一式。Canvas/WashPanel/GaugeArea を結線する。\n" +
             "★お風呂開始時に非表示にするだけで、進行度の計算は今までどおり動く")]
    [SerializeField] private GameObject gaugeArea;
    [Tooltip("「流す」ボタン。Canvas/WashPanel/ShowerSButton を結線する。\n" +
             "こすり終わり（Ready）になったときに表示する")]
    [SerializeField] private GameObject showerButton;

    [Header("タッチエフェクト")]
    [SerializeField] private BathTouchEffect touchEffect;

    [Header("手のカーソル")]
    [SerializeField] private RectTransform handCursor;

    [Header("体に付く泡")]
    [Tooltip("こすった場所に泡を置く担当。BathBubblePainter を結線する")]
    [SerializeField] private BathBubblePainter bubblePainter;

    [Header("体に付く泡（新方式・泡シェル）")]
    [Tooltip("ON で新方式を試す。初期化に失敗したら、そのお風呂は自動で旧方式へ戻る")]
    [SerializeField] private bool useNewFoam = true;
    [Tooltip("Bath.unity の BathFoamSystem を結線する。未結線でも旧方式で動く")]
    [SerializeField] private BathFoamController foam;

    private int _scrubCount;
    private bool _isComplete;
    private bool _inputBlocked;
    private bool _isDragging;
    private Vector2 _lastTouchPos;
    private float _accumulatedDistance;
    private string _shampooId;

    /// <summary>
    /// このお風呂で新方式の泡を使うか。
    /// ★useNewFoam ではなくこのフラグで分岐する。
    ///   結線漏れ・未対応キャラ（ここちゃんなど）・初期化失敗のときは false になり、
    ///   旧方式（BathBubblePainter）で洗える状態を必ず保つ。「泡が一切出ない」を作らないため。
    /// </summary>
    private bool _newFoamActiveForSession;

    /// <summary>
    /// 「流す」ボタンを押したか。連打で完了ボタンが二重に出ないようにするための目印。
    /// お風呂を始めるたびに false へ戻す。
    /// </summary>
    private bool _showerPressed;

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

        _showerPressed = false;

        if (completeButton != null) completeButton.SetActive(false);
        if (hintText != null) hintText.SetActive(true);
        if (handCursor != null) handCursor.gameObject.SetActive(false);

        // ★A2：あわあわゲージは画面に出さない（2026/8/27）
        //   「あと何%で終わるか」という作業感を出さないため。requirements.md §5 の
        //   「罪悪感を煽らない」方針にそろえ、進捗は体に付いていく泡そのもので見せる。
        //   ★消すのは見た目だけ。requiredDistancePerScrub / maxScrubCount / UpdateUI() は
        //     一切変えていないので、洗い終わりまでの操作時間はこれまでと同じ。
        if (gaugeArea != null) gaugeArea.SetActive(false);

        // ★A2：「流す」ボタンは Scene 上で active=1 のため、開始時に必ず隠す。
        //   隠さないと、洗う前から画面に出てしまう。
        if (showerButton != null) showerButton.SetActive(false);
        else Debug.LogWarning("[BathWash] Shower Button が未結線です。\n" +
                              "      Hierarchy の Canvas/WashPanel を選び、BathWashManager の \"Shower Button\" 欄に " +
                              "Canvas/WashPanel/ShowerSButton をドラッグしてください");

        Debug.Log($"[BathWash] Initialize: shampooId={shampooId} canvasCamera={_canvasCamera?.name ?? "null"} scrubArea={scrubArea?.name ?? "null"}");

        UpdateUI();
        UpdateShampooInfo(shampooId);

        // シャンプー別に泡の色を切り替える（requirements.md §5）
        touchEffect?.SetShampoo(shampooId);

        // 体に付く泡の準備。前回の泡を片付けて、シャンプーの色を取り込む。
        // SetShampoo より後に呼ぶこと（色の実体は BathTouchEffect が持っているため）
        //
        // ★新方式を先に試し、成功したときだけ新方式を使う。
        //   失敗（未結線・未対応キャラ・例外）なら、このお風呂は旧方式で通す。
        //
        //   ★どちらで動いているかを必ずログに出す。
        //     黙って旧方式に落ちると、何が悪いのか分からないまま時間を溶かすため。
        _newFoamActiveForSession = false;
        if (useNewFoam)
        {
            if (foam == null)
            {
                Debug.LogError("[BathWash] 新方式が ON ですが Foam が未結線です。\n" +
                               "      Hierarchy の Canvas/WashPanel を選び、BathWashManager の \"Foam\" 欄に " +
                               "BathFoamSystem をドラッグしてください");
            }
            else
            {
                _newFoamActiveForSession = foam.TryBeginWash(shampooId);
            }
        }

        if (_newFoamActiveForSession)
        {
            Debug.Log("<color=#00E5FF>[決定]</color> [BathWash] このお風呂は【新方式：泡シェル＋泡3.png】で動きます");
        }
        else
        {
            if (useNewFoam)
                Debug.LogWarning("<color=#00E5FF>[決定]</color> [BathWash] このお風呂は【旧方式：スプライトの泡（球方式）】で動きます。理由は直前のログを見てください");
            else
                Debug.Log("<color=#00E5FF>[決定]</color> [BathWash] このお風呂は【旧方式：スプライトの泡（球方式）】で動きます（Use New Foam が OFF）");

            bubblePainter?.Begin(shampooId);
        }
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
        // 指を離したら、泡を置く線をいったん切る
        if (_newFoamActiveForSession) foam.EndStroke(); else bubblePainter?.EndStroke();
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
            if (_newFoamActiveForSession) foam.EndStroke(); else bubblePainter?.EndStroke();
            UpdateFollowEffects(Vector2.zero, show: false);
            return;
        }

        bool inArea = IsInScrubArea(currentPos);

        Debug.Log($"[BathWash] Update: pos={currentPos} inArea={inArea} scrub={_scrubCount}");

        // ① エフェクトは scrubArea 内のときだけ表示する
        UpdateFollowEffects(currentPos, show: inArea);

        float dist = Vector2.Distance(currentPos, _lastTouchPos);
        _lastTouchPos = currentPos;

        if (!inArea)
        {
            // 範囲の外へ出たら線を切る。次に戻ってきたとき、外を横切った跡が線でつながらないようにするため
            if (_newFoamActiveForSession) foam.EndStroke(); else bubblePainter?.EndStroke();
            return;
        }

        // ② 指の位置に泡を置く（置けない条件は、それぞれの実装側で弾く）
        if (_newFoamActiveForSession) foam.Paint(currentPos);
        else                          bubblePainter?.TryPaint(currentPos);

        // ③ こすり距離が閾値を超えたらカウント
        _accumulatedDistance += dist;
        if (_accumulatedDistance >= requiredDistancePerScrub)
        {
            _accumulatedDistance -= requiredDistancePerScrub;
            _scrubCount++;
            Debug.Log($"[BathWash] ★ scrubCount++ = {_scrubCount}");
            UpdateUI();

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

        // ★A2：こすり終わりで出すのは「おふろ完了！」ではなく「流す」ボタン（2026/8/27）。
        //   CompleteButton は A5（泡が消え終わったタイミング）へ引っ越す。
        if (showerButton != null) showerButton.SetActive(true);

        // 泡側へ「進行度が満タンになった」ことを伝える
        if (_newFoamActiveForSession) foam.OnReady();

        Debug.Log("<color=#00E5FF>[決定]</color> [BathWash] こすり終わり（Ready）。「流す」ボタンを表示しました");
    }

    /// <summary>
    /// 「流す」ボタン（ShowerSButton）を押したとき。Inspector の OnClick から呼ばれる。
    ///
    /// 【いまの中身は暫定です】
    ///   ★A5 で差し替えること。
    ///     本来の流れ: 流す → 雲が出て雫が降る（A3）→ 泡が上から下へ消える（A4）
    ///                → 消え終わったら「おふろ完了！」を出す（A5）
    ///   A3〜A5 がまだ無い状態で何もしないと、お風呂を完了できず、
    ///   清潔値・信頼度の反映まで確認できなくなる。
    ///   そのため、ここでは暫定で「おふろ完了！」ボタンを直接出している。
    ///   A5 を作るときに、この SetActive(true) を「泡が消え終わったときの処理」へ移す。
    ///
    /// 【こすり入力について】
    ///   OnWashComplete() で _isComplete = true になっているため、
    ///   この時点で Update() のこすり判定も泡の追加もすでに止まっている。
    ///   ここで改めて止める処理は要らない。
    /// </summary>
    public void OnShowerButton()
    {
        // 連打対策。二度押しで完了ボタンが二重に出たり、A3 以降で雨が二重に降ったりしないようにする
        if (_showerPressed) return;
        _showerPressed = true;

        if (showerButton != null) showerButton.SetActive(false);

        // 指のエフェクトが出たままにならないよう、念のため止める
        touchEffect?.StopContinuous();
        if (handCursor != null) handCursor.gameObject.SetActive(false);

        Debug.Log("<color=#00E5FF>[決定]</color> [BathWash] 「流す」を押しました（A3〜A5 は未実装のため、暫定で「おふろ完了！」を表示します）");

        // ★暫定。A5 で「泡が消え終わったら」へ移す
        if (completeButton != null) completeButton.SetActive(true);
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
