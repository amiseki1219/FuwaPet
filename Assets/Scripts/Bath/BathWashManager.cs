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
        // ★2026/8/28：内部IDを "ichigo" から "ohisama" へ改名した。
        //   Bath.unity の BathTouchEffect.shampooSets[].shampooId が先に "ohisama" になっていて
        //   ID が食い違い、飾りパーティクルが せっけん の設定へ黙って落ちていたため、コード側をそろえた。
        //   シャンプーIDはセーブデータに保存していないので、既存セーブへの影響はない。
        new ShampooData { id = "ohisama",   displayName = "おひさまシャンプー", imageName = "OhisamaImage", description = "おひさまにあたったようないい香り。\n使うたびに甘えん坊になっちゃう？" },
        new ShampooData { id = "hoshizora", displayName = "ほしぞらシャンプー", imageName = "HoshiImage",   description = "星空みたいな神秘的な香り。\nコツコツがんばる気持ちが芽生えるかも" },
        new ShampooData { id = "rainbow",   displayName = "レインボーせっけん", imageName = "RainbowImage", description = "7色の泡があふれだす！\nどんな変化が起きるかはおたのしみ♪"         },
    };

    // お風呂1回あたりの信頼度加点。requirements.md §5「お世話ボタン効果一覧」で +3pt と確定している。
    private const int TrustPerBath = 3;

    // 性格パラメータの表示名。★並び順は ApplyPersonality() の抽選番号と一致させること。
    // 0=活動性(おてんば) 1=甘えん坊度(甘えん坊) 2=勤勉さ(しっかりもの)
    // 3=素直さ(素直) 4=感受性(優しさ)（内部名は requirements.md §6、表示名は §5 の対応表）
    // ★表示名の正本は ParamNames.cs。ここに文字列を直書きしないこと。
    private static readonly string[] PersonalityNames = ParamNames.Personality;

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

    [Header("タイトル・リザルト（A5）")]
    [Tooltip("洗う画面の見出し。Canvas/WashPanel/Title を結線する。\n" +
             "雲の演出が始まったら隠し、泡を流し終わったら文言を変えて出し直す")]
    [SerializeField] private GameObject titleRoot;

    [Tooltip("見出しの文字。Canvas/WashPanel/Title/Text (TMP) を結線する")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Tooltip("泡を流し終わったあとの見出し文言")]
    [SerializeField] private string finishedTitle = "おふろタイム終了";

    [Tooltip("シャンプーの説明欄。Canvas/WashPanel/ShampooInfoArea を結線する。\n" +
             "リザルトを出すときに隠す")]
    [SerializeField] private GameObject shampooInfoArea;

    [Tooltip("リザルトのカード。Canvas/WashPanel/ResultCard を結線する")]
    [SerializeField] private GameObject resultCard;

    [Tooltip("リザルトの見出し。ResultCard/TitleText を結線する")]
    [SerializeField] private TextMeshProUGUI resultTitleText;

    [Tooltip("リザルトの中身。ResultCard/ResultText を結線する")]
    [SerializeField] private TextMeshProUGUI resultText;

    [Header("雲の演出（A3）")]
    [Tooltip("「流す」を押したときに左上から流れてくる雲。\n" +
             "Canvas/WashPanel の子に置いた雲の Image を結線する。\n" +
             "未結線でも雲が出ないだけで、お風呂は最後まで進む")]
    [SerializeField] private BathCloudAnimator cloud;

    [Header("雫の演出（A4）")]
    [Tooltip("雲の下から降ってくる雫。★Canvas の外（シーン直下）に置いた GameObject を結線する。\n" +
             "未結線でも雫が出ないだけで、お風呂は最後まで進む")]
    [SerializeField] private BathDropletRain droplets;

    [Tooltip("泡を上から下へ消すのにかける秒数。★等速で下がる")]
    [Range(0.5f, 15f)]
    [SerializeField] private float rinseDuration = 5f;

    [Tooltip("雫を降らせてから、泡を消し始めるまでの待ち時間（秒）。\n" +
             "★0 にすると、雫がキャラに届く前に泡が消え始めてしまい、\n" +
             "  飛沫が床の高さでしか出なくなる。雫の落下時間ぶんだけ待つ")]
    [Range(0f, 3f)]
    [SerializeField] private float rainLeadSeconds = 1f;

    [Header("表情（A2.7）")]
    [Tooltip("キャラが実行時に生成される親。Bath.unity の CharacterDisplayAnchor を結線する。\n" +
             "★キャラは実行時に生成されるため、Scene ビュー（非Play時）には存在しない")]
    [SerializeField] private Transform characterAnchor;

    [Tooltip("この進行度を超えたら Relaxed にする（0〜1）。既定 0.5 = 50%\n" +
             "★洗い中に使う表情は Normal と Relaxed の2つだけ。\n" +
             "  Happy は A5（お風呂完了）まで取っておく（2026/8/28 決定）")]
    [Range(0f, 1f)]
    [SerializeField] private float relaxedThreshold = 0.5f;

    // 表情キー。CharacterFaceController が持つ9種のうち、お風呂で使うのはこの3つ。
    // ★「Smile」というキーは存在しない。あみまるさんが言う Smile は Happy のこと。
    private const string FaceKeyNormal  = "Normal";
    private const string FaceKeyRelaxed = "Relaxed";

    // ★A5（お風呂完了）で使う予定のキー。洗い中には使わない。
    private const string FaceKeyHappy   = "Happy";

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

    /// <summary>
    /// レインボーせっけんで上がる性格パラメータの番号（0〜4）。
    /// ★お風呂を始めるときに1回だけ抽選して覚える。
    ///   リザルトに出す内容と、実際にセーブへ書く内容を必ず一致させるため。
    ///   表示のときと保存のときで別々に抽選すると、画面と結果が食い違う。
    /// </summary>
    private int _rainbowPickedIndex = -1;

    /// <summary>見出しの元の文言。お風呂を始めるたびにここへ戻す。</summary>
    private string _originalTitle;

    /// <summary>いま適用している表情キー。同じ表情を毎回入れ直さないための目印。</summary>
    private string _currentFaceKey;

    /// <summary>表情コンポーネントが見つからない警告を、1回だけ出すための目印。</summary>
    private bool _faceWarned;

    private System.Collections.IEnumerator _sliderCoroutine;

    // Screen Space Camera 対応：scrubArea 判定に使うカメラ
    private Camera _canvasCamera;

    // ── ライフサイクル ────────────────────────────────────────────────────────

    private void Awake()
    {
        // 見出しの元の文言を覚えておく。お風呂を始めるたびにここへ戻すため
        if (titleText != null) _originalTitle = titleText.text;

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

        // ★A3：前回のお風呂の雲が残らないよう、開始時に必ず隠して初期位置へ戻す
        cloud?.HideImmediate();

        // ★A4：前回の雫が残らないよう、開始時に消す
        droplets?.ClearAll();

        // ★A5：リザルトを隠し、見出しと説明欄を元に戻す
        if (resultCard      != null) resultCard.SetActive(false);
        if (shampooInfoArea != null) shampooInfoArea.SetActive(true);
        if (titleRoot       != null) titleRoot.SetActive(true);
        if (titleText != null && !string.IsNullOrEmpty(_originalTitle)) titleText.text = _originalTitle;

        // ★レインボーせっけんの抽選はここで1回だけ行う。
        //   リザルトの表示と、実際にセーブへ書く内容を必ず一致させるため。
        _rainbowPickedIndex = Random.Range(0, PersonalityNames.Length);

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

        // ★A2.7：洗い進みに応じて表情を変える（Normal → Relaxed → Happy）。
        //   UpdateUI() は Initialize() と「こすりカウントが増えた瞬間」からしか呼ばれないので、
        //   毎フレーム処理にはならない。
        UpdateWashFace(pct * 0.01f);
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

    // ── 表情（A2.7） ──────────────────────────────────────────────────────────

    /// <summary>
    /// お風呂に入った直後に、状態パラメータに関係なく必ず Normal にする。
    /// BathSceneManager.Start() から呼ばれる。
    ///
    /// 【なぜ固定で上書きしてよいのか】
    ///   CharacterFaceController.SetExpression() は _overrideExpression に値を入れる作りで、
    ///   そのあと Start() の RefreshExpression() が走っても固定側が優先される。
    ///   → 呼ぶ順番がどちらでも Normal のままになる（コードで確認済み）。
    ///
    /// 【WashPanel が非アクティブでも呼べる】
    ///   コルーチンを使っていないため。SetActive(true) を待つ必要はない。
    /// </summary>
    public void SetFaceNormalOnEnter()
    {
        _currentFaceKey = null;   // 前のお風呂の値を引きずらない
        ApplyFace(FaceKeyNormal);
    }

    /// <summary>
    /// 進行度（0〜1）から表情を決めて適用する。
    ///
    /// ★洗い中に使うのは Normal と Relaxed の2つだけ（2026/8/28 決定）。
    ///   Normal →(relaxedThreshold)→ Relaxed。洗い終わり（100%）も Relaxed のまま。
    ///   Happy は A5（お風呂完了・リザルト画面）まで取っておく。
    ///   ＝ 完了したときの Happy を「ごほうび」として際立たせるため。
    /// </summary>
    private void UpdateWashFace(float progress01)
    {
        string key = progress01 >= relaxedThreshold ? FaceKeyRelaxed : FaceKeyNormal;
        ApplyFace(key);
    }

    /// <summary>
    /// 表情を固定する。同じ表情が続くときは何もしない。
    ///
    /// 【なぜ3種類も探すのか】
    ///   表情の持ち方がキャラで分かれている。
    ///     ぴよこ / える / ここ / ぱる … CharacterFaceController
    ///     ぽこ                       … FaceController または PokoFaceController
    ///   どれが付いているかは実行時に生成されたキャラで決まるので、見つかった順に使う。
    ///   ★どの経路で動いたかを必ず1行ログに出す（黙って何もしない状態を作らないため）。
    /// </summary>
    private void ApplyFace(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (_currentFaceKey == key) return;      // 同じ表情なら触らない

        if (characterAnchor == null)
        {
            if (!_faceWarned)
            {
                _faceWarned = true;
                Debug.LogWarning("[BathWash] Character Anchor が未結線のため、表情を切り替えられません。\n" +
                                 "      Hierarchy の Canvas/WashPanel を選び、BathWashManager の \"Character Anchor\" 欄に " +
                                 "CharacterDisplayAnchor をドラッグしてください");
            }
            return;
        }

        var charFace = characterAnchor.GetComponentInChildren<CharacterFaceController>(true);
        if (charFace != null)
        {
            charFace.SetExpression(key);
            _currentFaceKey = key;
            Debug.Log($"<color=#00E5FF>[決定]</color> [BathWash] 表情を {key} にしました（CharacterFaceController）");
            return;
        }

        var legacyFace = characterAnchor.GetComponentInChildren<FaceController>(true);
        if (legacyFace != null)
        {
            legacyFace.SetExpression(key);
            _currentFaceKey = key;
            Debug.Log($"<color=#00E5FF>[決定]</color> [BathWash] 表情を {key} にしました（FaceController）");
            return;
        }

        var pokoFace = characterAnchor.GetComponentInChildren<PokoFaceController>(true);
        if (pokoFace != null)
        {
            pokoFace.SetExpression(key);
            _currentFaceKey = key;
            Debug.Log($"<color=#00E5FF>[決定]</color> [BathWash] 表情を {key} にしました（PokoFaceController）");
            return;
        }

        if (!_faceWarned)
        {
            _faceWarned = true;
            Debug.LogWarning($"[BathWash] '{characterAnchor.name}' の下に表情コンポーネントが見つかりません。表情は切り替わりません");
        }
    }

    // ── ボタンハンドラ ────────────────────────────────────────────────────────

    /// <summary>
    /// スキップ。演出（雲・雫・泡を流す）を全部飛ばして、いっきにリザルトまで進める。
    ///
    /// ★清潔値・信頼度・性格の反映は変えない。
    ///   完了ボタンを押したときの OnComplete() が今までどおり行う。
    ///   ＝ スキップしても、こすり切った場合と同じ結果になる。
    /// </summary>
    public void OnSkip()
    {
        if (_isComplete && _showerPressed) return;   // 連打で二重に走らせない

        _scrubCount    = maxScrubCount;
        _isComplete    = true;
        _isDragging    = false;
        _showerPressed = true;   // 「流す」を押したのと同じ扱いにする

        touchEffect?.StopContinuous();
        if (hintText     != null) hintText.SetActive(false);
        if (handCursor   != null) handCursor.gameObject.SetActive(false);
        if (showerButton != null) showerButton.SetActive(false);

        UpdateUI();   // 進行度を100%にしてUIとセリフを合わせる

        // 演出は出さずに片付ける
        cloud?.HideImmediate();
        droplets?.ClearAll();

        if (_newFoamActiveForSession && foam != null) foam.ClearFoamImmediate();
        else                                          bubblePainter?.ClearAll();

        Debug.Log("<color=#00E5FF>[決定]</color> [BathWash] スキップされました。演出を飛ばしてリザルトを表示します");

        ShowCompleteButton();
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

        // ★A5：雲の演出が始まるので見出しを隠す
        if (titleRoot != null) titleRoot.SetActive(false);

        Debug.Log("<color=#00E5FF>[決定]</color> [BathWash] 「流す」を押しました。雲 → 雫 → 泡を流す、の順に進みます");

        // ★A3：雲を画面左上から流し、着いたら雫を降らせ始める（A4 へつなぐ）
        if (cloud != null)
        {
            cloud.PlayEnter(BeginRinseSequence);
        }
        else
        {
            // 黙って何もしない状態を作らない。未結線なら理由を1行残して、雲を飛ばして先へ進む
            Debug.LogWarning("[BathWash] Cloud が未結線のため、雲の演出は飛ばして泡を流します。\n" +
                             "      Canvas/WashPanel を選び、BathWashManager の \"Cloud\" 欄に雲の Image を結線してください");
            BeginRinseSequence();
        }
    }

    // ── 流す演出の進行（A4 → A5） ─────────────────────────────────────────────

    /// <summary>
    /// 雲が定位置に着いたところから呼ばれる。雫を降らせ、泡を上から消し始める。
    /// </summary>
    private void BeginRinseSequence()
    {
        droplets?.StartRain();

        // ★先に雫を降らせ、キャラに届くころに泡を消し始める。
        //   同時に始めると、雫が落ちてくる前に境界が下まで行ってしまい、
        //   飛沫が床の高さでしか出なくなる（2026/8/28 の実機確認で判明）。
        StartCoroutine(StartRinseAfterLead());
    }

    private System.Collections.IEnumerator StartRinseAfterLead()
    {
        if (rainLeadSeconds > 0f) yield return new WaitForSeconds(rainLeadSeconds);

        if (_newFoamActiveForSession && foam != null)
        {
            foam.StartRinse(rinseDuration, OnRinseFinished);
        }
        else
        {
            // 旧方式のときは泡を上から消す仕組みが無い。
            // ★黙って止まらないよう、同じ秒数だけ待ってから完了へ進める。
            Debug.LogWarning("[BathWash] 旧方式のため、泡を上から消す演出は行いません（時間だけ待って完了へ進みます）");
            yield return new WaitForSeconds(rinseDuration);
            OnRinseFinished();
        }
    }

    /// <summary>
    /// 泡が消え切ったとき。雫を止め、雲を退散させ、そのあと完了ボタンを出す。
    /// ★OnComplete() の中身は触らない。ここは「完了ボタンを出す」までが担当。
    /// </summary>
    private void OnRinseFinished()
    {
        droplets?.StopRain();

        // ★A2.7：完了なので表情を Happy にする（洗い中は Relaxed までに留めてある）
        ApplyFace(FaceKeyHappy);

        if (cloud != null) cloud.PlayExit(ShowCompleteButton);
        else               ShowCompleteButton();
    }

    /// <summary>完了ボタンを出す。雲の退散が終わってから呼ばれる。</summary>
    private void ShowCompleteButton()
    {
        // 表情は Happy。OnRinseFinished でも呼んでいるが、同じ表情なら何もしないので二重でも安全
        ApplyFace(FaceKeyHappy);

        // ★A5：見出しを「おふろタイム終了」にして出し直す
        if (titleText != null && !string.IsNullOrEmpty(finishedTitle)) titleText.text = finishedTitle;
        if (titleRoot != null) titleRoot.SetActive(true);

        // ★A5：シャンプーの説明を隠して、リザルトを出す
        if (shampooInfoArea != null) shampooInfoArea.SetActive(false);
        BuildResultTexts();
        if (resultCard != null) resultCard.SetActive(true);

        if (completeButton != null) completeButton.SetActive(true);

        Debug.Log("<color=#00E5FF>[決定]</color> [BathWash] お風呂の演出が終わりました。リザルトと完了ボタンを表示します");
    }

    // リザルト1行分の項目名を、全角スペースで6文字ぶんの幅にそろえる。
    // 表示名の長さがまちまち（キレイ=3／しっかりもの=6）なので、そろえないと ＋ の位置がガタつく。
    // ★6 は現時点の最長「しっかりもの」に合わせた値。これより長い表示名を足すときは広げること。
    private const int ResultNameWidth = 6;

    private static string PadName(string name)
    {
        if (string.IsNullOrEmpty(name)) return new string('\u3000', ResultNameWidth);
        int pad = ResultNameWidth - name.Length;
        return pad > 0 ? name + new string('\u3000', pad) : name;
    }

    /// <summary>
    /// リザルトの文言を組み立てる。
    ///
    /// ★ここで出す数字は、OnComplete() が実際にセーブへ書く数字と同じ元から取っている。
    ///   （清潔値は GetCleanAmount()、信頼度は TrustPerBath、性格は _rainbowPickedIndex）
    ///   表示用に別計算を作らないこと。作ると必ずズレる。
    ///
    /// ★未対応：清潔値が 100 で頭打ちになる場合、実際の増分は表示より少なくなる。
    ///   これは S-2（清潔値の実増分を表示に使う）で直す。いまは回復量をそのまま出している。
    /// </summary>
    private void BuildResultTexts()
    {
        string charName = CharacterNames.ResolveDisplayName(SaveManager.Instance?.Data);
        if (string.IsNullOrEmpty(charName)) charName = "この子";

        if (resultTitleText != null)
            resultTitleText.text = $"{charName}がピカピカになったよ";

        if (resultText == null) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"{PadName(ParamNames.Clean)}{ParamNames.PtWide(Mathf.RoundToInt(GetCleanAmount()))}");
        sb.Append($"{PadName(ParamNames.Trust)}{ParamNames.PtWide(TrustPerBath)}");

        string personality = GetPersonalityResultLine();
        if (!string.IsNullOrEmpty(personality))
        {
            sb.AppendLine();
            sb.Append(personality);
        }

        resultText.text = sb.ToString();
    }

    /// <summary>
    /// シャンプーごとの性格パラメータ変化を1行で返す。変化が無いシャンプーは null。
    /// ★ApplyPersonality() と同じ分岐にそろえてある。片方だけ直さないこと。
    /// </summary>
    private string GetPersonalityResultLine()
    {
        switch (_shampooId)
        {
            case "ohisama":
                return $"{PadName(ParamNames.Dependency)}{ParamNames.PtWide(2)}";
            case "hoshizora":
                return $"{PadName(ParamNames.Diligence)}{ParamNames.PtWide(2)}";
            case "rainbow":
                int idx = (_rainbowPickedIndex >= 0 && _rainbowPickedIndex < PersonalityNames.Length)
                    ? _rainbowPickedIndex : 0;
                return $"{PadName(PersonalityNames[idx])}{ParamNames.PtWide(1)}";
            default:
                return null;   // せっけんは性格が変わらない
        }
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
            case "ohisama":
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
            case "ohisama":
                save.personalityDependency = Mathf.Clamp(save.personalityDependency + 2, -100, 100);
                break;
            case "hoshizora":
                save.personalityDiligence = Mathf.Clamp(save.personalityDiligence + 2, -100, 100);
                break;
            case "rainbow":
                // ★Initialize() で抽選した番号を使う。ここで引き直すとリザルト表示とズレる
                int idx = (_rainbowPickedIndex >= 0 && _rainbowPickedIndex < PersonalityNames.Length)
                    ? _rainbowPickedIndex : Random.Range(0, PersonalityNames.Length);
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
