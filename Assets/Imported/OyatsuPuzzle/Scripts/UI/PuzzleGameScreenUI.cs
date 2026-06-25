using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Random = UnityEngine.Random;

namespace OyatsuPuzzle
{
    internal sealed class SwipeCellHandler : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public int BoardRow { get; set; }
        public int BoardCol { get; set; }
        public Action<int, int, Vector2> OnSwipeDown { get; set; }
        public Action<int, int, Vector2, Vector2> OnSwipeUp { get; set; }
        public Action<int, int, PointerEventData> OnDragBegin  { get; set; }
        public Action<int, int, PointerEventData> OnDragMove   { get; set; }
        public Action<int, int, PointerEventData> OnDragFinish { get; set; }

        void IPointerDownHandler.OnPointerDown(PointerEventData e)
            => OnSwipeDown?.Invoke(BoardRow, BoardCol, e.position);

        void IPointerUpHandler.OnPointerUp(PointerEventData e)
            => OnSwipeUp?.Invoke(BoardRow, BoardCol, e.pressPosition, e.position);

        void IBeginDragHandler.OnBeginDrag(PointerEventData e)
            => OnDragBegin?.Invoke(BoardRow, BoardCol, e);

        void IDragHandler.OnDrag(PointerEventData e)
            => OnDragMove?.Invoke(BoardRow, BoardCol, e);

        void IEndDragHandler.OnEndDrag(PointerEventData e)
            => OnDragFinish?.Invoke(BoardRow, BoardCol, e);
    }

    public class PuzzleGameScreenUI : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField] private TMP_Text stageLabelText;
        [SerializeField] private TMP_Text moveCountLabelText;
        [SerializeField] private TMP_Text supportMessageText;

        [Header("Board")]
        [SerializeField] private Transform boardRoot;

        [Header("Drag Follow")]
        [Tooltip("この距離(px)以上ドラッグして離すとスワップ確定。未満なら元に戻る。")]
        [SerializeField] private float dragSwapThreshold = 40f;
        [Tooltip("指/マウスへの追従割合（0〜1）。小さいほど控えめに追従。")]
        [SerializeField] private float dragFollowStrength = 0.75f;
        [Tooltip("追従できる上限（セル何個分まで離れられるか）。")]
        [SerializeField] private float dragFollowMaxCells = 0.9f;

        [Header("Collect Effect")]
        [Tooltip("回収アイコンがスロットへ飛ぶ時間(秒)。大きいほどゆっくり＝見やすい。")]
        [SerializeField] private float collectFlyDuration = 0.6f;
        [Tooltip("複数同時回収時、1個ごとに飛び出しを遅らせる間隔(秒)。0で同時。")]
        [SerializeField] private float collectFlyStagger = 0.06f;
        [Tooltip("到着時の装飾用スプライト(星パーティクル)。未設定なら菱形で代用（Overlayでも見える）。キラン1回につき星2個＋回収アイテム1個＋追加装飾2個の計5要素を出す。")]
        [SerializeField] private Sprite sparkleSprite;
        [Tooltip("回収アイテム画像のサイズ倍率（star基準=sparkleSizeMax比）。主役を大きく見せる。")]
        [SerializeField] private float collectItemSizeMul = 1.35f;
        [Tooltip("星の色（アルファは下の sparklePeakAlpha で柔らかくする）。回収アイテム画像は白(素の色)で表示。")]
        [SerializeField] private Color sparkleColor = new Color(1f, 0.95f, 0.55f, 1f);
        [Tooltip("装飾1個の最小サイズ(px相当)。大きめ装飾向け。")]
        [SerializeField] private float sparkleSizeMin = 56f;
        [Tooltip("装飾1個の最大サイズ(px相当)。大きめ装飾向け。")]
        [SerializeField] private float sparkleSizeMax = 90f;
        [Tooltip("出現時のピークのアルファ(0〜1)。少し透けさせて柔らかく見せる。")]
        [Range(0f, 1f)]
        [SerializeField] private float sparklePeakAlpha = 0.9f;
        [Tooltip("ふわっと漂う移動時間(秒)。大きいほどゆっくり漂う。動きすぎ注意。")]
        [SerializeField] private float sparkleDuration = 0.6f;
        [Tooltip("フェードアウト時間(秒)。")]
        [SerializeField] private float sparkleFadeDuration = 0.45f;
        [Tooltip("同じSlotでこの秒数以内の再発火を抑制（連続回収時の連打防止）。")]
        [SerializeField] private float sparkleCooldown = 0.3f;

        [Header("Collect Extra Decor Sprites")]
        [Tooltip("GoalItemSlot到着時の追加装飾1。音符などを割り当てる。未設定ならその枠は出さない（星2＋アイテム1にdegrade）。")]
        [SerializeField] private Sprite collectExtraDecorSprite1;
        [Tooltip("GoalItemSlot到着時の追加装飾2。星以外の装飾などを割り当てる。未設定ならその枠は出さない。")]
        [SerializeField] private Sprite collectExtraDecorSprite2;
        [Tooltip("GoalItemSlot到着時の追加装飾3。さらに別の装飾を割り当てる。未設定ならその枠は出さない。")]
        [SerializeField] private Sprite collectExtraDecorSprite3;

        [Header("Collect Decor Layout (px相当・スロット中心からのオフセット)")]
        [Tooltip("星1の表示位置（スロット中心からのオフセット。x右+ / y上+）。参考画像準拠＝右上。")]
        [SerializeField] private Vector2 collectDecorStar1Offset   = new Vector2( 52f,  48f); // 右上
        [Tooltip("星2の表示位置。参考画像準拠＝上中央やや左（高め）。")]
        [SerializeField] private Vector2 collectDecorStar2Offset   = new Vector2(-22f,  62f); // 上中央やや左
        [Tooltip("回収アイテム画像（主役）の表示位置。参考画像準拠＝中央。")]
        [SerializeField] private Vector2 collectDecorItemOffset    = new Vector2(  0f,   6f); // 中央
        [Tooltip("追加装飾1（ピンク等）の表示位置。参考画像準拠＝左上。")]
        [SerializeField] private Vector2 collectExtraDecor1Offset  = new Vector2(-64f,  44f); // 左上
        [Tooltip("追加装飾2（紫等）の表示位置。参考画像準拠＝右(中段)。")]
        [SerializeField] private Vector2 collectExtraDecor2Offset  = new Vector2( 76f,  16f); // 右(中段)
        [Tooltip("追加装飾3（音符等）の表示位置。参考画像準拠＝左下。")]
        [SerializeField] private Vector2 collectExtraDecor3Offset  = new Vector2(-80f, -18f); // 左下
        [Tooltip("追加装飾(collectExtraDecorSprite1/2)のサイズ倍率。音符など縦長の見え方を微調整。")]
        [SerializeField] private float collectExtraDecorScale = 1.0f;
        [Tooltip("追加装飾の初期傾きの最大角度(±度)。音符を少し傾けて自然に見せる。0で正立。")]
        [SerializeField] private float collectExtraDecorRotationRange = 12f;

        [Header("Piece Sprites")]
        [Tooltip("PieceType ごとのピース画像。未設定(None)の種類は PieceColor の単色表示にフォールバックします。")]
        [SerializeField] private PieceSprite[] pieceSprites;

        [System.Serializable]
        public class PieceSprite
        {
            public PieceType type;
            public Sprite    sprite;
        }

        [Header("Goal / Collect Sprites")]
        [Tooltip("ゴールUI（あつめるもの）と回収演出で使う画像（プレーン版）。UI/PlayUI/PuzzleGamePanelUI のプレーン画像を割り当てる。未設定の種類は pieceSprites（ピース版）へ自動フォールバック。")]
        [SerializeField] private PieceSprite[] goalSprites;

        [Header("Collect Decor Item Sprites")]
        [Tooltip("GoalItemSlot到着時の『ふわっと装飾』の中に出すプレーン画像専用。ここだけに使う（GoalItemCard表示・飛行アイコンには未使用）。UI/PlayUI/PuzzleGamePanelUI のプレーン画像を割り当てる。未設定の種類は装飾アイテムを sparkleSprite（星）にフォールバック。")]
        [SerializeField] private PieceSprite[] collectDecorItemSprites;

        [Header("Goal Item Slots")]
        [Tooltip("あつめるもの表示用スロット（最大3）。session.Goals の順に画像＋個数を反映。未使用スロットは自動で非表示。")]
        [SerializeField] private GoalItemSlot[] goalItemSlots;

        [System.Serializable]
        public class GoalItemSlot
        {
            public GameObject      root;
            public Image           itemImage;
            public TextMeshProUGUI countText;
        }

        [Header("Goal Count Colors")]
        [Tooltip("CountText の色分け（Rich Text）。現在数 / スラッシュ / 必要数。")]
        [SerializeField] private Color currentCountColor  = new Color(1f,     0.498f, 0.659f); // #FF7FA8 現在数(ピンク)
        [SerializeField] private Color slashColor         = new Color(0.784f, 0.706f, 0.627f); // #C8B4A0 スラッシュ
        [SerializeField] private Color requiredCountColor = new Color(0.663f, 0.435f, 0.235f); // #A96F3C 必要数(ブラウン)

        [Header("Buttons")]
        [SerializeField] private Button pauseButton;

        [Header("Return Confirm")]
        [SerializeField] private Button     returnButton;
        [SerializeField] private GameObject returnConfirmPanel;
        [SerializeField] private Button     returnContinueButton;
        [SerializeField] private Button     returnQuitButton;

        [Header("References")]
        [SerializeField] private PuzzleManager          puzzleManager;
        [SerializeField] private PuzzleScreenController screenController;

        [Header("Support Messages")]
        [Tooltip("状況別の応援メッセージ。各タイミングでランダム表示。空配列ならそのタイミングは無表示。")]
        [SerializeField] private string[] startMessages =
        {
            "いっしょにがんばろう♪",
            "パズルスタート！おやつ集めよう〜",
            "目標のおやつをそろえてね♪",
            "リラックスしていこ〜",
        };
        [SerializeField] private string[] matchMessages =
        {
            "そろったね♪",
            "ナイス〜！",
            "いいかんじ♪",
            "上手だよ〜",
        };
        [SerializeField] private string[] goalProgressMessages =
        {
            "集まってきたよ♪",
            "その調子〜！",
            "目標に近づいてる！",
            "いいペースだね♪",
        };
        [SerializeField] private string[] almostGoalMessages =
        {
            "もうちょっとで目標達成♪",
            "あと1個！見つけたらクリアだよ♪",
            "最後のおやつを探そう〜",
            "あと少しでクリアだね♪",
        };
        [SerializeField] private string[] lowMovesMessages =
        {
            "のこり手数に気をつけてね",
            "あと少し、慎重にいこう",
            "ここから大事だよ〜",
            "目標ピースをねらっていこう",
        };
        [SerializeField] private string[] lastMoveMessages =
        {
            "ラスト1手！目標ピースをねらおう",
            "最後の一手、いけるよ！",
            "ここで決めたいね…！",
        };
        [SerializeField] private string[] invalidSwapMessages =
        {
            "惜しい！ほかの組み合わせを探そう",
            "そこはそろわなかったみたい",
            "別の場所も見てみよ〜",
            "うーん、ちがう場所がよさそう",
        };
        [SerializeField] private string[] shuffleMessages =
        {
            "動かせる場所がないから、まぜまぜするね♪",
            "ピースをまぜまぜ中…",
            "新しい並びにするね♪",
            "ちょっとだけシャッフルするよ",
        };
        [SerializeField] private string[] comboMessages =
        {
            "おおっ、連続で消えたよ！",
            "いい連鎖だね♪",
            "どんどん集まってる〜！",
            "ぽんぽん消えて気持ちいいね♪",
        };

        [Header("Support Message Timing")]
        [SerializeField] private float supportHoldSeconds      = 2.5f;
        [SerializeField] private float supportStartHoldSeconds = 3.5f;

        private Image[,]           _bgImages;
        private int                _size;
        private int                _selRow = -1;
        private int                _selCol = -1;
        private int                _currentStage;
        private bool               _inputLocked;
        private string             _lastSupportMsg;
        private Coroutine          _supportClearCo;
        private bool               _lowMovesWarned;

        // ドラッグ操作（つかんで動かす）用の状態
        private Canvas             _canvas;
        private Camera             _uiCamera;
        private bool               _dragActive;
        private bool               _suppressClickFromDrag;
        private Image              _dragClone;
        private int                _dragFromRow = -1;
        private int                _dragFromCol = -1;
        private Vector3            _dragBaseWorldPos;
        private float              _dragFollowMaxWorld;
        private Transform          _dragOverlayParent;

        // 同じ GoalItemSlot へのキラキラ連打を抑えるクールダウン（slot → 次に出せる時刻）
        private readonly Dictionary<GoalItemSlot, float> _sparkleNextTime = new Dictionary<GoalItemSlot, float>();


        private void Awake()
        {
            EnsureCanvas();

            if (pauseButton != null)
                pauseButton.onClick.AddListener(() => Debug.Log("[OyatsuPuzzle] Pause tapped."));

            if (returnButton != null)
                returnButton.onClick.AddListener(ShowReturnConfirm);
            if (returnContinueButton != null)
                returnContinueButton.onClick.AddListener(HideReturnConfirm);
            if (returnQuitButton != null)
                returnQuitButton.onClick.AddListener(OnReturnQuit);

            // 確認パネルは初期非表示
            if (returnConfirmPanel != null)
                returnConfirmPanel.SetActive(false);
        }

        // ──────────────────────────────────────────
        // Return confirm dialog（パネル表示のみ。回数/セッション処理は変更しない）
        // ──────────────────────────────────────────

        private void ShowReturnConfirm()
        {
            Debug.Log("[OyatsuPuzzle] Return pressed. Showing confirm dialog.");
            if (returnConfirmPanel != null) returnConfirmPanel.SetActive(true);
        }

        private void HideReturnConfirm()
        {
            Debug.Log("[OyatsuPuzzle] Return confirm: Continue. Closing dialog.");
            if (returnConfirmPanel != null) returnConfirmPanel.SetActive(false);
        }

        private void OnReturnQuit()
        {
            // 既存の安全な戻り処理（Clear/Fail 画面と同じ ShowStart）に接続。
            // プレイ回数はスタート時に消費済みのため、ここでは変更しない。
            Debug.Log("[OyatsuPuzzle] Return confirm: Quit. Returning to StartPanel (play count unchanged).");
            if (returnConfirmPanel != null) returnConfirmPanel.SetActive(false);
            if (screenController != null) screenController.ShowStart();
        }

        public void Refresh()
        {
            var session = puzzleManager?.CurrentSession;
            if (session == null)
            {
                Debug.LogError("[OyatsuPuzzle] Refresh: CurrentSession is null", this);
                return;
            }

            session.OnMovesChanged -= HandleMovesChanged;
            session.OnGoalsChanged -= HandleGoalsChanged;
            session.OnMovesChanged += HandleMovesChanged;
            session.OnGoalsChanged += HandleGoalsChanged;

            _currentStage = session.StageData.stageNumber;
            _inputLocked  = false;

            Debug.Log($"[OyatsuPuzzle] Stage started: Stage {_currentStage}");
            Debug.Log($"[OyatsuPuzzle] Moves={session.RemainingMoves}");
            foreach (var g in session.Goals)
                Debug.Log($"[OyatsuPuzzle] Goal: {g.pieceType.ToEnglishName()} 0 / {g.requiredCount}");

            LogPieceWeights(_currentStage);

            if (stageLabelText != null)
                stageLabelText.text = $"Stage {_currentStage}";

            RefreshMovesLabel(session.RemainingMoves);
            RefreshGoalLabel(session.Goals);
            BuildBoard(session);

            // ステージ開始メッセージ（少し長めに表示）
            _lowMovesWarned = false;
            ShowSupport(startMessages, supportStartHoldSeconds);
        }

        // ──────────────────────────────────────────
        // Piece weight log
        // ──────────────────────────────────────────

        private static void LogPieceWeights(int stage)
        {
            Debug.Log($"[OyatsuPuzzle] Piece weights for Stage {stage}:");
            foreach (var (piece, weight) in PuzzleBoard.GetWeightTable(stage))
                Debug.Log($"[OyatsuPuzzle] {piece} weight={weight}");
        }

        // ──────────────────────────────────────────
        // Board build
        // ──────────────────────────────────────────

        private void BuildBoard(PuzzleSession session)
        {
            if (boardRoot == null)
            {
                Debug.LogError("[OyatsuPuzzle] BuildBoard: boardRoot is null", this);
                return;
            }

            _size = session.Board.Size;
            Debug.Log($"[OyatsuPuzzle] BuildBoard started. width={_size} height={_size}");

            for (int i = boardRoot.childCount - 1; i >= 0; i--)
            {
                var child = boardRoot.GetChild(i);
                child.DOKill(); // 旧セルに残ったTweenを停止してから破棄
                Destroy(child.gameObject);
            }

            _bgImages = new Image[_size, _size];
            _selRow   = -1;
            _selCol   = -1;

            var grid = boardRoot.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = _size; // 盤面サイズに追従（5×5→5 / 6×6→6）

                // 盤面サイズで分岐（案B）。6×6以上はセルを小さく詰める。Stage1〜3(5×5)は従来設定を維持。
                if (_size >= 6)
                {
                    grid.cellSize = new Vector2(125f, 125f);
                    grid.spacing  = new Vector2(4f, 4f);
                }
                else
                {
                    grid.cellSize = new Vector2(150f, 150f);
                    grid.spacing  = new Vector2(8f, 8f);
                }
            }

            for (int row = 0; row < _size; row++)
                for (int col = 0; col < _size; col++)
                    CreateCell(row, col, session.Board.Grid[row, col]);

            Debug.Log($"[OyatsuPuzzle] BuildBoard completed. cells={_size * _size}");

            var initCheck = FindMatches(session);
            Debug.Log($"[OyatsuPuzzle] Initial board validation: matches={initCheck.Count}");
            LogBoardDistribution(session);

#if UNITY_EDITOR
            CreateLegend();
            CreateDebugButtons(session);
#endif
        }

        private void CreateLegend()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            const string legendName = "PieceLegend";
            var existing = canvas.transform.Find(legendName);
            if (existing != null) Destroy(existing.gameObject);

            var go = new GameObject(legendName);
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 0f);
            rt.anchorMax        = new Vector2(1f, 0f);
            rt.pivot            = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-10f, 10f);
            rt.sizeDelta        = new Vector2(130f, 168f);

            var bg = go.AddComponent<Image>();
            bg.color         = new Color(0f, 0f, 0f, 0.55f);
            bg.raycastTarget = false;

            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(go.transform, false);
            var lblRT = lblGO.AddComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = new Vector2(8f, 6f);
            lblRT.offsetMax = new Vector2(-8f, -6f);

            var tmp = lblGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "Legend:\n"
                + "NI Niboshi\n"
                + "BI Biscuit\n"
                + "CA Carrot\n"
                + "CO Coin\n"
                + "ST Star\n"
                + "PU Pudding\n"
                + "HM Macaron\n"
                + "CK Cake";
            tmp.fontSize      = 14f;
            tmp.alignment     = TextAlignmentOptions.TopLeft;
            tmp.color         = Color.white;
            tmp.raycastTarget = false;
        }

        private void CreateCell(int row, int col, PieceType piece)
        {
            var go = new GameObject($"Cell_{col}_{row}");
            go.transform.SetParent(boardRoot, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(95f, 95f);

            var img = go.AddComponent<Image>();
            ApplyPieceVisual(img, piece);
            _bgImages[row, col] = img;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            int r = row, c = col;
            btn.onClick.AddListener(() => OnCellClicked(r, c));

            var swipe = go.AddComponent<SwipeCellHandler>();
            swipe.BoardRow    = row;
            swipe.BoardCol    = col;
            swipe.OnSwipeDown = HandleSwipeDown;
            swipe.OnSwipeUp   = HandleSwipeUp;
            swipe.OnDragBegin  = HandleDragBegin;
            swipe.OnDragMove   = HandleDrag;
            swipe.OnDragFinish = HandleDragEnd;

            Debug.Log($"[OyatsuPuzzle] Cell created: {col},{row} piece={PieceDebugLabel(piece)}");
        }

        // ──────────────────────────────────────────
        // Click-select swap
        // ──────────────────────────────────────────

        private void OnCellClicked(int row, int col)
        {
            // 直前にドラッグ操作が走ったジェスチャーのクリックは握りつぶす（タップ選択との二重発火防止）
            if (_suppressClickFromDrag) { _suppressClickFromDrag = false; Debug.Log("[OyatsuPuzzle] Click suppressed (drag gesture)."); return; }
            if (_inputLocked) { Debug.Log("[OyatsuPuzzle] Input ignored. inputLocked=true"); return; }
            var session = puzzleManager?.CurrentSession;
            if (session == null || !session.IsActive) return;

            string pieceLbl = PieceDebugLabel(session.Board.Grid[row, col]);

            if (_selRow < 0)
            {
                _selRow = row;
                _selCol = col;
                ApplySelectedScale(row, col, true);
                Debug.Log($"[OyatsuPuzzle] FIRST SELECT: cell=({col},{row}) piece={pieceLbl}");
                return;
            }

            if (row == _selRow && col == _selCol)
            {
                ApplySelectedScale(_selRow, _selCol, false);
                _selRow = -1;
                _selCol = -1;
                Debug.Log("[OyatsuPuzzle] Selection cleared.");
                return;
            }

            Debug.Log($"[OyatsuPuzzle] SECOND SELECT: cell=({col},{row}) piece={pieceLbl}");
            int r1 = _selRow, c1 = _selCol;
            ApplySelectedScale(r1, c1, false);
            _selRow = -1;
            _selCol = -1;
            StartCoroutine(TrySwapCellsRoutine(r1, c1, row, col));
        }

        private void ApplySelectedScale(int row, int col, bool selected)
        {
            if (_bgImages == null) return;
            var img = _bgImages[row, col];
            if (img == null) return;
            var t = img.transform;
            t.DOKill();                                       // 連打時にTweenが重ならないように
            float target = selected ? 1.18f : 1f;
            t.DOScale(target, 0.14f).SetEase(Ease.OutBack);   // ぷにっと（選択中は大きいまま維持）
        }

        // ──────────────────────────────────────────
        // Swipe input
        // ──────────────────────────────────────────

        private void HandleSwipeDown(int row, int col, Vector2 pos)
        {
            // 新しいジェスチャー開始。前ジェスチャーのクリック抑制フラグをリセット。
            _suppressClickFromDrag = false;
            if (_inputLocked) return;
            var session = puzzleManager?.CurrentSession;
            if (session == null || !session.IsActive) return;

            // 押した瞬間の「つかんだ感」：セルを少しだけ拡大（scaleのみ＝GridLayoutGroup非競合）
            var img = _bgImages != null ? _bgImages[row, col] : null;
            if (img != null)
            {
                img.transform.DOKill();
                img.transform.DOScale(1.12f, 0.08f).SetEase(Ease.OutBack);
            }

            string pieceLbl = PieceDebugLabel(session.Board.Grid[row, col]);
            Debug.Log($"[OyatsuPuzzle] Pointer down (grab): cell=({col},{row}) piece={pieceLbl} pos=({(int)pos.x},{(int)pos.y})");
        }

        private void HandleSwipeUp(int row, int col, Vector2 downPos, Vector2 upPos)
        {
            // ドラッグ操作だった場合はスワップ確定をドラッグ側に任せる（二重スワップ防止）
            if (_dragActive) return;

            // タップ（ドラッグ未満）：つかみポップを戻す。選択スケールは OnCellClicked が再適用する。
            var img = _bgImages != null ? _bgImages[row, col] : null;
            if (img != null)
            {
                img.transform.DOKill();
                float target = (row == _selRow && col == _selCol) ? 1.18f : 1f;
                img.transform.DOScale(target, 0.10f).SetEase(Ease.OutQuad);
            }
        }

        // ──────────────────────────────────────────
        // Drag input（つかんで動かす → 離してスワップ）
        // ──────────────────────────────────────────

        private void HandleDragBegin(int row, int col, PointerEventData e)
        {
            if (_inputLocked) return;
            var session = puzzleManager?.CurrentSession;
            if (session == null || !session.IsActive) return;
            if (_bgImages == null) return;
            var img = _bgImages[row, col];
            if (img == null) return;

            _suppressClickFromDrag = true; // このジェスチャーのクリックは無効化
            EnsureCanvas();

            // 競合回避：進行中のタップ選択があれば解除する
            if (_selRow >= 0)
            {
                ApplySelectedScale(_selRow, _selCol, false);
                _selRow = -1;
                _selCol = -1;
            }

            _dragActive  = true;
            _inputLocked = true; // ドラッグ中は他入力をロック
            _dragFromRow = row;
            _dragFromCol = col;

            var rt = img.transform as RectTransform;
            img.transform.DOKill();
            img.transform.localScale = Vector3.one;
            _dragBaseWorldPos = rt.position;

            float cellWorld = rt.rect.height * Mathf.Abs(rt.lossyScale.y);
            if (cellWorld <= 0.01f) cellWorld = rt.rect.height; // フォールバック
            _dragFollowMaxWorld = cellWorld * Mathf.Max(0.1f, dragFollowMaxCells);

            _dragOverlayParent = boardRoot.parent != null ? boardRoot.parent : boardRoot;
            _dragClone = MakeSwapClone(img, _dragOverlayParent, _dragBaseWorldPos);
            SetCellAlpha(img, 0f); // 元セルは透明化（クローンをつかんでいる見た目）

            Debug.Log($"[OyatsuPuzzle] Drag begin: cell=({col},{row})");
        }

        private void HandleDrag(int row, int col, PointerEventData e)
        {
            if (!_dragActive || _dragClone == null) return;
            if (!ScreenToOverlayWorld(e.pressPosition, out var pressW)) return;
            if (!ScreenToOverlayWorld(e.position,      out var curW))   return;

            // 指/マウス方向へ「少しだけ」追従（strengthで控えめに、上限でクランプ）
            Vector3 d = (curW - pressW) * Mathf.Clamp01(dragFollowStrength);
            if (d.magnitude > _dragFollowMaxWorld)
                d = d.normalized * _dragFollowMaxWorld;
            _dragClone.transform.position = _dragBaseWorldPos + d;
        }

        private void HandleDragEnd(int row, int col, PointerEventData e)
        {
            if (!_dragActive) return;
            _dragActive = false;

            if (_dragClone == null) { _inputLocked = false; return; }

            var session = puzzleManager?.CurrentSession;
            if (session == null || !session.IsActive)
            {
                StartCoroutine(ReturnDragCloneRoutine());
                return;
            }

            Vector2 delta = e.position - e.pressPosition;
            float absX = Mathf.Abs(delta.x);
            float absY = Mathf.Abs(delta.y);
            float max  = Mathf.Max(absX, absY);

            // しきい値未満の小さなドラッグ → 元に戻す
            if (max < dragSwapThreshold)
            {
                Debug.Log($"[OyatsuPuzzle] Drag too short. max={max:F0} threshold={dragSwapThreshold}. Reverting.");
                StartCoroutine(ReturnDragCloneRoutine());
                return;
            }

            int toRow = _dragFromRow, toCol = _dragFromCol;
            if (absX >= absY) toCol += (delta.x > 0 ? 1 : -1);
            else              toRow += (delta.y > 0 ? -1 : 1);

            // 盤面外方向 → 元に戻す
            if (toRow < 0 || toRow >= _size || toCol < 0 || toCol >= _size)
            {
                Debug.Log($"[OyatsuPuzzle] Drag out of board. to=({toCol},{toRow}). Reverting.");
                StartCoroutine(ReturnDragCloneRoutine());
                return;
            }

            Debug.Log($"[OyatsuPuzzle] Drag release: from=({_dragFromCol},{_dragFromRow}) to=({toCol},{toRow})");

            // 有効な隣セル：つかんでいるクローンをそのまま使って共通の TrySwapCellsRoutine へ
            var clone = _dragClone;
            _dragClone = null; // 所有権をルーチンへ委譲（ルーチンが破棄する）
            StartCoroutine(TrySwapCellsRoutine(_dragFromRow, _dragFromCol, toRow, toCol, clone));
        }

        // ドラッグを途中でやめた / 無効方向だったときに、クローンを元位置へ戻して破棄する
        private IEnumerator ReturnDragCloneRoutine()
        {
            var clone = _dragClone;
            _dragClone = null;
            int r = _dragFromRow, c = _dragFromCol;

            if (clone != null)
            {
                const float backDur = 0.16f;
                clone.transform.DOKill();
                clone.transform.DOMove(_dragBaseWorldPos, backDur).SetEase(Ease.OutQuad);
                clone.transform.DOScale(1f, backDur).SetEase(Ease.OutQuad);
                yield return new WaitForSeconds(backDur);
                if (clone != null) Destroy(clone.gameObject);
            }

            if (_bgImages != null && r >= 0 && r < _size && c >= 0 && c < _size && _bgImages[r, c] != null)
            {
                SetCellAlpha(_bgImages[r, c], 1f);
                PuruPuruCell(r, c); // 戻ったあとに小さくぷるっと
            }

            _dragFromRow = -1;
            _dragFromCol = -1;
            _inputLocked = false;
        }

        // スクリーン座標 → オーバーレイ親のワールド座標
        private bool ScreenToOverlayWorld(Vector2 screen, out Vector3 world)
        {
            world = default;
            var rect = (_dragOverlayParent != null ? _dragOverlayParent : boardRoot) as RectTransform;
            if (rect == null) return false;
            return RectTransformUtility.ScreenPointToWorldPointInRectangle(rect, screen, _uiCamera, out world);
        }

        private void EnsureCanvas()
        {
            if (_canvas != null) return;
            _canvas   = GetComponentInParent<Canvas>();
            _uiCamera = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? _canvas.worldCamera
                : null;
        }

        // ──────────────────────────────────────────
        // Swap → ResolveMatches
        // ──────────────────────────────────────────

        // draggedClone != null の場合はドラッグ中につかんでいたクローンをそのまま流用する
        // （null の場合は従来どおりタップ選択スワップとしてクローンを生成）。
        private IEnumerator TrySwapCellsRoutine(int fromRow, int fromCol, int toRow, int toCol, Image draggedClone = null)
        {
            var session = puzzleManager?.CurrentSession;
            if (session == null)
            {
                if (draggedClone != null) Destroy(draggedClone.gameObject);
                _inputLocked = false;
                yield break;
            }

            int dx = Mathf.Abs(fromCol - toCol);
            int dy = Mathf.Abs(fromRow - toRow);
            if (dx + dy != 1)
            {
                Debug.Log($"[OyatsuPuzzle] Swap rejected. Not adjacent. dx={dx} dy={dy}");
                if (draggedClone != null)
                {
                    Destroy(draggedClone.gameObject);
                    if (_bgImages != null && _bgImages[fromRow, fromCol] != null)
                        SetCellAlpha(_bgImages[fromRow, fromCol], 1f);
                    _inputLocked = false;
                }
                yield break;
            }
            if (_bgImages == null)
            {
                if (draggedClone != null) Destroy(draggedClone.gameObject);
                _inputLocked = false;
                yield break;
            }

            var imgA = _bgImages[fromRow, fromCol];
            var imgB = _bgImages[toRow, toCol];
            if (imgA == null || imgB == null)
            {
                if (draggedClone != null) Destroy(draggedClone.gameObject);
                _inputLocked = false;
                yield break;
            }

            _inputLocked = true;

            var grid = session.Board.Grid;
            Debug.Log($"[OyatsuPuzzle] TrySwapCells: from=({fromCol},{fromRow}) to=({toCol},{toRow}) drag={(draggedClone != null)}");

            // クローン画像で「つかんで移動」を表現（GridLayoutGroupは無効化しない）
            var rtA = imgA.transform as RectTransform;
            var rtB = imgB.transform as RectTransform;
            Vector3 posA = rtA.position;
            Vector3 posB = rtB.position;

            Transform overlayParent = boardRoot.parent != null ? boardRoot.parent : boardRoot;
            const float moveDur = 0.18f;

            // 先に「データ上」だけ入れ替えてマッチ判定する（見た目はまだ一切動かさない）。
            // これにより無効時は2ピースを入れ替えて見せずに済む。
            (grid[fromRow, fromCol], grid[toRow, toCol]) = (grid[toRow, toCol], grid[fromRow, fromCol]);
            var matches = FindMatches(session);
            bool valid = matches.Count > 0;
            if (!valid)
            {
                // 無効：データを即座に元へ戻す（盤面は最初から最後まで入れ替わらない）
                (grid[fromRow, fromCol], grid[toRow, toCol]) = (grid[toRow, toCol], grid[fromRow, fromCol]);
            }

            // 掴んでいるピースのクローン（cloneA）を用意。ドラッグ時は流用、タップ時は生成。
            Image cloneA;
            if (draggedClone != null)
            {
                // ドラッグ中のクローンを継続使用（imgA は既に透明化済み）
                cloneA = draggedClone;
                cloneA.transform.DOKill();
            }
            else
            {
                imgA.transform.DOKill(); imgA.transform.localScale = Vector3.one;
                cloneA = MakeSwapClone(imgA, overlayParent, posA);
                SetCellAlpha(imgA, 0f); // from セルだけ透明化
            }

            if (!valid)
            {
                // ===== 無効スワップ：掴んだピースだけが相手の上に乗りかけて、ぷるんと戻る =====
                // 相手(to)ピースは一切動かさない／透明化しない／クローンも作らない。
                Debug.Log("[OyatsuPuzzle] No match. Lean over to-piece and bounce back (to-piece stays put).");

                // to ピースの中心までは行かず、少し重なる位置まで（乗りかけ）
                const float overlap = 0.6f;
                Vector3 leanPos = Vector3.Lerp(posA, posB, overlap);

                cloneA.transform.DOMove(leanPos, moveDur).SetEase(Ease.OutQuad);
                cloneA.transform.DOScale(1.12f, moveDur).SetEase(Ease.OutQuad); // 乗りかけ＝少し持ち上がる
                yield return new WaitForSeconds(moveDur);

                // ぷるんと小さく揺れる（クローンの scale パンチ）
                cloneA.transform.DOPunchScale(new Vector3(0.16f, 0.16f, 0f), 0.26f, 10, 0.9f);
                yield return new WaitForSeconds(0.26f);

                // 掴んだピースだけが元位置へぷるんと戻る
                cloneA.transform.DOMove(posA, moveDur).SetEase(Ease.OutBack);
                cloneA.transform.DOScale(1f, moveDur).SetEase(Ease.OutBack);
                yield return new WaitForSeconds(moveDur);

                Destroy(cloneA.gameObject);
                SetCellAlpha(imgA, 1f); // from セルの表示を戻す（to セルは一度も触っていない）

                ShowSupport(invalidSwapMessages, supportHoldSeconds);
                Debug.Log($"[OyatsuPuzzle] Moves unchanged: {session.RemainingMoves}");
                _inputLocked = false;
                yield break;
            }

            // ===== 有効スワップ：従来どおり2ピースが入れ替わる演出 =====
            Debug.Log($"[OyatsuPuzzle] Match found after swap. count={matches.Count}");
            imgB.transform.DOKill(); imgB.transform.localScale = Vector3.one;
            var cloneB = MakeSwapClone(imgB, overlayParent, posB);
            SetCellAlpha(imgB, 0f);

            cloneA.transform.DOMove(posB, moveDur).SetEase(Ease.OutQuad);
            cloneA.transform.DOScale(1.08f, moveDur).SetEase(Ease.OutQuad);
            cloneB.transform.DOMove(posA, moveDur).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(moveDur);

            Destroy(cloneA.gameObject);
            Destroy(cloneB.gameObject);
            SetCellAlpha(imgA, 1f);
            SetCellAlpha(imgB, 1f);
            RefreshBoardVisual(session);

            int prev = session.RemainingMoves;
            session.ConsumeMove();
            Debug.Log($"[OyatsuPuzzle] Moves decreased: {prev} -> {session.RemainingMoves}");

            // _inputLocked は継続。ResolveMatchesRoutine 完了時に解除される。
            yield return ResolveMatchesRoutine(session, matches);
        }

        // ──────────────────────────────────────────
        // ResolveMatches — cascade loop
        // ──────────────────────────────────────────

        private IEnumerator ResolveMatchesRoutine(PuzzleSession session,
            List<(Vector2Int cell, PieceType piece)> firstMatches = null)
        {
            const int maxCascade = 10;
            int cascade = 0;
            int matchRounds = 0;
            int beforeCleared = SumCleared(session);

            Debug.Log("[OyatsuPuzzle] ResolveMatches started.");

            while (cascade < maxCascade)
            {
                var matches = (cascade == 0 && firstMatches != null)
                    ? firstMatches
                    : FindMatches(session);

                if (matches.Count == 0)
                {
                    Debug.Log($"[OyatsuPuzzle] ResolveMatches finished. cascades={cascade}");
                    break;
                }

                matchRounds++;
                Debug.Log($"[OyatsuPuzzle] Cascade {cascade + 1} match count={matches.Count}");

                bool cleared = ApplyGoalProgress(session, matches);

                // ゴール対象は「あつめるもの」へ回収演出、それ以外は消去ポップ。完了を待ってから盤面更新。
                yield return PlayMatchResolveRoutine(session, matches);

                if (cleared)
                {
                    // ステージクリア（ApplyGoalProgress 内でクリア遷移を予約済み）。ここで終了。
                    yield break;
                }

                ClearMatchedCells(session, matches);
                ApplyGravity(session);
                RefillFromTop(session);
                RefreshBoardVisual(session);
                ResetCellScales(); // ポップで縮んだセルを必ず scale 1 に戻す
                Debug.Log("[OyatsuPuzzle] Board visual refreshed after gravity refill.");

                cascade++;
            }

            if (cascade >= maxCascade)
                Debug.LogWarning("[OyatsuPuzzle] Cascade limit reached.");

            var postCheck = FindMatches(session);
            Debug.Log($"[OyatsuPuzzle] Post-resolve validation: matches={postCheck.Count}");

            bool hasMoves = session.Board.HasAnyPossibleMove();
            Debug.Log($"[OyatsuPuzzle] Possible move validation: {hasMoves}");
            bool shuffled = false;
            if (!hasMoves)
            {
                Debug.Log("[OyatsuPuzzle] No possible moves after resolve. Shuffling board.");
                ShowSupport(shuffleMessages, supportHoldSeconds);
                session.Board.ShuffleBoardUntilPlayable();
                RefreshBoardVisual(session);
                ResetCellScales();
                shuffled = true;
            }

            // 応援メッセージ（シャッフル時はシャッフル文言を優先）
            if (!shuffled)
            {
                bool combo = matchRounds >= 2;
                bool goalProgressed = SumCleared(session) > beforeCleared;
                UpdateSupportAfterMove(session, combo, goalProgressed);
            }

            // Unlock before fail-check so that game-over lock is set by CheckFail if needed.
            _inputLocked = false;
            Debug.Log("[OyatsuPuzzle] Input unlocked.");

            CheckFail(session);
        }

        // ──────────────────────────────────────────
        // Phase1 アニメーション（scale / shake のみ・GridLayoutGroup非競合）
        // ──────────────────────────────────────────

        // マッチ解決の見せ方：ゴール対象ピースは「あつめるもの」スロットへ飛ばし(回収演出)、
        // それ以外はその場でポップ(膨らむ→縮む→フェード)する。両方を同時に開始し完了まで待つ。
        private IEnumerator PlayMatchResolveRoutine(PuzzleSession session,
            List<(Vector2Int cell, PieceType piece)> matches)
        {
            if (_bgImages == null || matches == null || matches.Count == 0) yield break;

            // ゴールの PieceType → 表示中スロット の対応（session.Goals[i] ↔ goalItemSlots[i]）
            var slotByType = new Dictionary<PieceType, GoalItemSlot>();
            if (session != null && session.Goals != null && goalItemSlots != null)
            {
                int n = Mathf.Min(session.Goals.Count, goalItemSlots.Length);
                for (int i = 0; i < n; i++)
                {
                    var slot = goalItemSlots[i];
                    if (slot != null && slot.itemImage != null && slot.root != null && slot.root.activeInHierarchy)
                        slotByType[session.Goals[i].pieceType] = slot;
                }
            }

            Transform overlayParent = boardRoot.parent != null ? boardRoot.parent : boardRoot;
            float maxWait = 0f;
            int flyIndex = 0; // 複数回収のスタッガー（順番にずらして飛ばす）
            var popCells = new List<(Vector2Int cell, PieceType piece)>();

            foreach (var (cell, piece) in matches)
            {
                int r = cell.y, c = cell.x;
                if (r < 0 || r >= _size || c < 0 || c >= _size) continue;
                var img = _bgImages[r, c];
                if (img == null) continue;

                // ゴール対象＆スロット表示中＆Sprite あり → 回収演出。それ以外/Sprite欠落 → 通常ポップ。
                Sprite collectSprite = GoalSpriteFor(piece);
                if (collectSprite != null && slotByType.TryGetValue(piece, out var slot))
                {
                    float d = StartCollectFly(img, collectSprite, piece, slot, overlayParent, flyIndex);
                    flyIndex++;
                    maxWait = Mathf.Max(maxWait, d);
                }
                else
                {
                    popCells.Add((cell, piece));
                }
            }

            float popDur = StartMatchPops(popCells);
            maxWait = Mathf.Max(maxWait, popDur);

            if (maxWait > 0f) yield return new WaitForSeconds(maxWait);
        }

        // 従来の消去ポップ（膨らむ→縮む→フェード）を開始する。待ちは呼び出し側。所要秒数を返す。
        private float StartMatchPops(List<(Vector2Int cell, PieceType piece)> cells)
        {
            if (_bgImages == null || cells == null || cells.Count == 0) return 0f;

            const float bulge  = 0.11f; // 1.0 → 1.20
            const float hold   = 0.07f; // 1.20 をキープ（余韻）
            const float shrink = 0.25f; // 1.20 → 0.0（alpha も同時にフェード）
            bool any = false;

            foreach (var (cell, _) in cells)
            {
                int r = cell.y, c = cell.x;
                if (r < 0 || r >= _size || c < 0 || c >= _size) continue;
                var img = _bgImages[r, c];
                if (img == null) continue;

                var t = img.transform;
                t.DOKill();
                img.DOKill();
                t.localScale = Vector3.one;
                SetCellAlpha(img, 1f);
                DOTween.Sequence()
                    .Append(t.DOScale(1.20f, bulge).SetEase(Ease.OutQuad))
                    .AppendInterval(hold)
                    .Append(t.DOScale(0f, shrink).SetEase(Ease.InQuad))
                    .Join(img.DOFade(0f, shrink).SetEase(Ease.InQuad)); // 縮みと同時にフェードアウト
                any = true;
            }

            return any ? (bulge + hold + shrink) : 0f; // ~0.43s
        }

        // 回収演出：マッチしたピースのクローンを GoalItemSlot へ飛ばす（Overlay・GridLayoutGroup非競合）。
        // 元セルは透明化（後段の ResetCellScales で復帰）。所要秒数を返す。Sprite/参照欠落時は 0 を返す。
        // flyIndex: 複数同時回収時のスタッガー番号（順番にずらして飛ばす）。
        private float StartCollectFly(Image cellImg, Sprite sprite, PieceType piece, GoalItemSlot slot, Transform overlayParent, int flyIndex)
        {
            var srcRt = cellImg != null ? cellImg.transform as RectTransform : null;
            var targetRt = (slot != null && slot.itemImage != null) ? slot.itemImage.transform as RectTransform : null;
            if (srcRt == null || targetRt == null || sprite == null) return 0f;

            Vector3 startPos = srcRt.position;
            Vector3 endPos   = targetRt.position;

            // 元セルはポップせず即座に透明化（飛ぶクローンへ置き換わる見た目）
            cellImg.transform.DOKill();
            cellImg.DOKill();
            cellImg.transform.localScale = Vector3.one;
            SetCellAlpha(cellImg, 0f);

            var go = new GameObject("CollectFly");
            go.transform.SetParent(overlayParent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = srcRt.rect.size;
            var img = go.AddComponent<Image>();
            img.sprite         = sprite;
            img.preserveAspect = true;
            img.raycastTarget  = false;
            rt.position   = startPos;
            rt.localScale = Vector3.one;

            float delay  = Mathf.Max(0f, collectFlyStagger) * Mathf.Max(0, flyIndex);
            const float popDur = 0.12f;
            float flyDur = Mathf.Max(0.05f, collectFlyDuration); // Inspectorで調整可（既定0.6=ゆっくり）

            var seq = DOTween.Sequence();
            if (delay > 0f) seq.AppendInterval(delay);            // 自分の順番までは元位置で待機
            seq.Append(rt.DOScale(1.25f, popDur).SetEase(Ease.OutBack))  // ひと膨らみ＝拾い上げ
                .Append(rt.DOMove(endPos, flyDur).SetEase(Ease.InBack))  // スロットへ吸い込まれる
                .Join(rt.DOScale(0.45f, flyDur).SetEase(Ease.InQuad))    // 小さくなりながら
                .OnComplete(() =>
                {
                    if (go != null) Destroy(go);
                    PulseSlot(slot);                 // 到着時にスロットアイコンをぷるっと
                    // キラン演出：星×2 ＋ 回収アイテムのプレーン画像×1。
                    // 装飾アイテム画像は装飾専用の CollectDecorSpriteFor(piece) で解決する。
                    // （飛ぶアイコンの sprite=GoalSpriteFor とは別管理。goalSprites には依存しない）
                    SpawnSparkleBurst(slot, overlayParent, CollectDecorSpriteFor(piece));
                });

            return delay + popDur + flyDur;
        }

        // 回収到着時、ゴールスロットのアイコンを軽くパンチスケール（scaleのみ＝GridLayoutGroup非競合）。
        private void PulseSlot(GoalItemSlot slot)
        {
            if (slot == null || slot.itemImage == null) return;
            var t = slot.itemImage.transform;
            t.DOKill();
            t.localScale = Vector3.one;
            t.DOPunchScale(new Vector3(0.20f, 0.20f, 0f), 0.25f, 10, 0.9f);
        }

        // 到着時装飾の種類。色味・サイズ・未設定時の挙動を種類ごとに分ける。
        private enum DecorKind { Star, Item, Extra }

        // 到着時の「キラッ」演出（SparkleDecor）。ScreenSpaceOverlay でも確実に見えるよう、
        // ParticleSystem ではなく UI Image を使う。1回の回収につき 1 回だけ、
        // 【星(左上) / 星(右上) / 回収アイテムのプレーン画像(中央・主役) / 追加デコ(左下) / 追加デコ(右下)】の
        // 最大5要素を、Inspector で指定した各オフセット位置にふわっと出して柔らかく消す。
        // 位置は collectDecor*Offset（px相当・スロット中心基準）で個別に調整可能。
        // itemSprite: 回収した PieceType のプレーン画像（CollectDecorSpriteFor の結果。null可）。
        private void SpawnSparkleBurst(GoalItemSlot slot, Transform parent, Sprite itemSprite)
        {
            if (slot == null || slot.itemImage == null || parent == null) return;

            // 「1個集めるごとに1回キラン」。同じSlotで sparkleCooldown 秒以内の連打は抑制。
            float now = Time.unscaledTime;
            if (_sparkleNextTime.TryGetValue(slot, out float next) && now < next) return;
            _sparkleNextTime[slot] = now + Mathf.Max(0f, sparkleCooldown);

            var slotRt = slot.itemImage.transform as RectTransform;
            if (slotRt == null) return;

            Vector3 center = slotRt.position;
            float scale = Mathf.Abs(slotRt.lossyScale.y); // local(px相当) → world 変換係数
            if (scale <= 0.0001f) scale = 1f;

            float sizeMin = Mathf.Min(sparkleSizeMin, sparkleSizeMax);
            float sizeMax = Mathf.Max(sparkleSizeMin, sparkleSizeMax);
            float peakAlpha = Mathf.Clamp01(sparklePeakAlpha) * Mathf.Clamp01(sparkleColor.a);

            // 1回のキランで出す装飾を組み立てる：星2 ＋ アイテム1(主役・中央) ＋ 追加デコ3 ＝ 最大6。
            // 各要素の表示位置は Inspector の collectDecor*Offset（px相当・中心基準）で個別指定。
            // ・Item: itemSprite(プレーン画像)を白色・大きめで表示。null時は星にフォールバック。
            // ・Star: sparkleSprite を sparkleColor で淡く色付け。null時は菱形代用。
            // ・Extra: collectExtraDecorSprite1/2/3(音符など)を白色で表示。★未設定ならその枠は出さない(degrade)。
            var decors = new List<(Sprite sprite, DecorKind kind, Vector2 offset)>(6)
            {
                (sparkleSprite, DecorKind.Star, collectDecorStar1Offset), // 星1
                (sparkleSprite, DecorKind.Star, collectDecorStar2Offset), // 星2
                (itemSprite != null ? itemSprite : sparkleSprite,
                 itemSprite != null ? DecorKind.Item : DecorKind.Star, collectDecorItemOffset), // 回収アイテム(主役)
            };
            if (collectExtraDecorSprite1 != null) decors.Add((collectExtraDecorSprite1, DecorKind.Extra, collectExtraDecor1Offset)); // 追加デコ1
            if (collectExtraDecorSprite2 != null) decors.Add((collectExtraDecorSprite2, DecorKind.Extra, collectExtraDecor2Offset)); // 追加デコ2
            if (collectExtraDecorSprite3 != null) decors.Add((collectExtraDecorSprite3, DecorKind.Extra, collectExtraDecor3Offset)); // 追加デコ3

            for (int i = 0; i < decors.Count; i++)
            {
                var (decorSprite, kind, offset) = decors[i];
                bool isItem = kind == DecorKind.Item;

                var go = new GameObject(isItem ? "CollectItemDecor" : "CollectSparkleDecor");
                go.transform.SetParent(parent, false);
                var rt = go.AddComponent<RectTransform>();

                // アイテム(主役)は大きく、星はやや小さめ、追加デコは中間×倍率。主役を分かりやすく。
                float sz;
                switch (kind)
                {
                    case DecorKind.Item:  sz = sizeMax * Mathf.Max(0.1f, collectItemSizeMul); break;                       // 主役：大きめ
                    case DecorKind.Extra: sz = Mathf.Lerp(sizeMin, sizeMax, 0.5f) * Mathf.Max(0.1f, collectExtraDecorScale); break; // 中間×倍率(音符調整用)
                    default:              sz = Mathf.Lerp(sizeMin, sizeMax, 0.35f); break;                                 // 星：やや小さめ
                }
                rt.sizeDelta = new Vector2(sz, sz);

                var img = go.AddComponent<Image>();
                img.sprite = decorSprite; // null可（星が未設定なら菱形で代用）
                // アイテム・追加デコはプレーン/独自色なので白(=素の色)。星のみ sparkleColor で淡く色付け。
                var startColor = (kind == DecorKind.Star)
                    ? new Color(sparkleColor.r, sparkleColor.g, sparkleColor.b, peakAlpha)
                    : new Color(1f, 1f, 1f, peakAlpha);
                img.color          = startColor;
                img.preserveAspect = true;
                img.raycastTarget  = false;

                // ★Inspector のオフセット(px相当)で配置。重なる場合は collectDecor*Offset で調整。
                Vector3 startPos = center + new Vector3(offset.x, offset.y, 0f) * scale;
                rt.position = startPos;

                // 傾き：星は控えめ固定、追加デコは collectExtraDecorRotationRange で調整、アイテムは正立。
                float baseRot   = (sparkleSprite == null && kind == DecorKind.Star) ? 45f : 0f; // 星が未設定なら菱形向き
                float jitterRot;
                switch (kind)
                {
                    case DecorKind.Item:  jitterRot = 0f; break;                                                  // 主役：正立
                    case DecorKind.Extra: jitterRot = ((i % 2 == 0) ? -1f : 1f) * Mathf.Abs(collectExtraDecorRotationRange); break; // 音符などを傾ける
                    default:              jitterRot = (i % 2 == 0) ? -8f : 8f; break;                             // 星：±8度
                }
                rt.localRotation = Quaternion.Euler(0f, 0f, baseRot + jitterRot);

                // スケールは 0 からではなく 0.75〜0.85 から 1.0 へ（ぽわっと膨らむ）。
                float startScale = isItem ? 0.85f : 0.78f;
                rt.localScale = Vector3.one * startScale;

                // 移動は「その場で 8〜18px ほど上へふわっと漂う」程度（動かしすぎない）。
                float driftUp = (isItem ? 10f : 14f) * scale; // px相当 → world
                Vector3 driftTarget = startPos + new Vector3(0f, driftUp, 0f);

                const float popT = 0.22f; // ふわっと出る（アイテムは気持ち大きくポップ）
                float peakScale = isItem ? 1.10f : ((i % 2 == 0) ? 1.04f : 1.0f); // 主役だけ少し膨らむ
                float swayZ = baseRot + (isItem ? 0f : ((i % 2 == 0) ? 6f : -6f)); // 漂う間のごく緩い回転
                float moveDur = Mathf.Max(0.05f, sparkleDuration);
                float fadeDur = Mathf.Max(0.05f, sparkleFadeDuration);
                DOTween.Sequence()
                    .Append(rt.DOScale(peakScale, popT).SetEase(Ease.OutBack))          // その場でぽわっと出る
                    .Append(rt.DOMove(driftTarget, moveDur).SetEase(Ease.OutSine))      // ほんの少し上へ漂う
                    .Join(rt.DOLocalRotate(new Vector3(0f, 0f, swayZ), moveDur).SetEase(Ease.OutSine))
                    .Join(img.DOFade(0f, fadeDur).SetEase(Ease.InQuad)
                             .SetDelay(Mathf.Max(0f, moveDur - fadeDur)))              // 終盤にふっと消える
                    .OnComplete(() => { if (go != null) Destroy(go); });
            }
        }

        // 全セルの scale を 1 に戻す（ポップ/選択の残りを消す）。
        private void ResetCellScales()
        {
            if (_bgImages == null) return;
            for (int r = 0; r < _size; r++)
                for (int c = 0; c < _size; c++)
                {
                    var img = _bgImages[r, c];
                    if (img == null) continue;
                    img.transform.DOKill();
                    img.DOKill();
                    img.transform.localScale = Vector3.one;
                    SetCellAlpha(img, 1f); // ポップのフェードで下げた alpha を戻す
                }
        }

        // スワップ移動演出用のクローン画像を生成（Overlay）。GridLayoutGroupの無い親に置く。
        private Image MakeSwapClone(Image src, Transform parent, Vector3 worldPos)
        {
            var go = new GameObject("SwapClone");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            var srcRt = src.transform as RectTransform;
            if (srcRt != null) rt.sizeDelta = srcRt.rect.size;
            var img = go.AddComponent<Image>();
            img.sprite         = src.sprite;
            var col = src.color; col.a = 1f; img.color = col; // 確実に見える状態で複製
            img.preserveAspect = src.preserveAspect;
            img.raycastTarget  = false;
            rt.position   = worldPos;
            rt.localScale = Vector3.one * 1.08f; // 少し大きめ＝つかんでいる感
            return img;
        }

        private static void SetCellAlpha(Image img, float a)
        {
            if (img == null) return;
            var c = img.color; c.a = a; img.color = c;
        }

        // ぷるっと小さく揺れる（scaleパンチのみ＝GridLayoutGroupと競合しない）。無効スワップの戻り演出用。
        private void PuruPuruCell(int row, int col)
        {
            if (_bgImages == null || row < 0 || row >= _size || col < 0 || col >= _size) return;
            var img = _bgImages[row, col];
            if (img == null) return;
            var t = img.transform;
            t.DOKill();
            t.localScale = Vector3.one;
            t.DOPunchScale(new Vector3(0.16f, 0.16f, 0f), 0.32f, 10, 0.9f);
        }

        private void ShakeCell(int row, int col, float dur)
        {
            if (_bgImages == null || row < 0 || row >= _size || col < 0 || col >= _size) return;
            var img = _bgImages[row, col];
            if (img == null) return;
            var rt = img.transform as RectTransform;
            if (rt == null) return;
            rt.DOKill();
            // GridLayoutGroup と競合しないよう小さな横シェイクのみ（終了時に元位置へ自動復帰）
            rt.DOShakeAnchorPos(dur, new Vector2(12f, 0f), 14, 90f, false, true);
        }

        // ──────────────────────────────────────────
        // FindMatches (3個以上対応)
        // ──────────────────────────────────────────

        private List<(Vector2Int cell, PieceType piece)> FindMatches(PuzzleSession session)
        {
            var grid    = session.Board.Grid;
            var matched = new HashSet<Vector2Int>();

            // Horizontal
            for (int row = 0; row < _size; row++)
            {
                int col = 0;
                while (col < _size)
                {
                    PieceType t = grid[row, col];
                    if (t == PieceType.None) { col++; continue; }
                    int len = 1;
                    while (col + len < _size && grid[row, col + len] == t) len++;
                    if (len >= 3)
                    {
                        Debug.Log($"[OyatsuPuzzle] Horizontal match found. row={row} startX={col} length={len} piece={PieceDebugLabel(t)}{(len >= 4 ? $" *** {len}-match ***" : "")}");
                        for (int k = 0; k < len; k++)
                        {
                            var cell = new Vector2Int(col + k, row);
                            if (matched.Add(cell))
                                Debug.Log($"[OyatsuPuzzle] Matched cell: {col + k},{row} piece={PieceDebugLabel(t)}");
                        }
                    }
                    col += len;
                }
            }

            // Vertical
            for (int c = 0; c < _size; c++)
            {
                int row = 0;
                while (row < _size)
                {
                    PieceType t = grid[row, c];
                    if (t == PieceType.None) { row++; continue; }
                    int len = 1;
                    while (row + len < _size && grid[row + len, c] == t) len++;
                    if (len >= 3)
                    {
                        Debug.Log($"[OyatsuPuzzle] Vertical match found. col={c} startY={row} length={len} piece={PieceDebugLabel(t)}{(len >= 4 ? $" *** {len}-match ***" : "")}");
                        for (int k = 0; k < len; k++)
                        {
                            var cell = new Vector2Int(c, row + k);
                            if (matched.Add(cell))
                                Debug.Log($"[OyatsuPuzzle] Matched cell: {c},{row + k} piece={PieceDebugLabel(t)}");
                        }
                    }
                    row += len;
                }
            }

            var result = new List<(Vector2Int, PieceType)>();
            foreach (var cell in matched)
                result.Add((cell, grid[cell.y, cell.x]));
            return result;
        }

        // ──────────────────────────────────────────
        // Goal progress
        // ──────────────────────────────────────────

        // Returns true if the stage transitioned to Clear.
        private bool ApplyGoalProgress(PuzzleSession session, List<(Vector2Int cell, PieceType piece)> matches)
        {
            bool goalsChanged = false;

            foreach (var goal in session.Goals)
            {
                int count = 0;
                foreach (var (_, piece) in matches)
                    if (piece == goal.pieceType) count++;

                if (count > 0)
                {
                    goal.clearedCount += count;
                    Debug.Log($"[OyatsuPuzzle] Matched {goal.pieceType.ToEnglishName()} count={count}");
                    goalsChanged = true;
                }
            }

            if (!goalsChanged) return false;

            foreach (var goal in session.Goals)
                Debug.Log($"[OyatsuPuzzle] Goal progress: {goal.pieceType.ToEnglishName()} {goal.clearedCount} / {goal.requiredCount}");

            bool cleared = session.IsCleared;
            Debug.Log($"[OyatsuPuzzle] Stage clear check: {cleared}");
            session.NotifyGoalsChanged();

            if (cleared)
            {
                Debug.Log("[OyatsuPuzzle] Stage clear.");
                int prev = session.StageData.stageNumber;
                puzzleManager?.FinishClear();
                Debug.Log($"[OyatsuPuzzle] Stage advanced: {prev} -> {PuzzleProgressManager.CurrentStage}");
                _inputLocked = true;
                StartCoroutine(After(0.4f, () => screenController?.ShowClear()));
                return true;
            }

            return false;
        }

        // ──────────────────────────────────────────
        // Fall-and-refill (replaces ReplaceMatchedPieces)
        // ──────────────────────────────────────────

        private void ClearMatchedCells(PuzzleSession session, List<(Vector2Int cell, PieceType piece)> matches)
        {
            var grid = session.Board.Grid;
            foreach (var (cell, _) in matches)
                grid[cell.y, cell.x] = PieceType.None;
            Debug.Log($"[OyatsuPuzzle] Cleared cells count={matches.Count}");
            Debug.Log($"[OyatsuPuzzle] None count after clear={CountNone(grid)}");
        }

        private void ApplyGravity(PuzzleSession session)
        {
            var grid = session.Board.Grid;
            for (int col = 0; col < _size; col++)
            {
                int writeRow = _size - 1;
                for (int row = _size - 1; row >= 0; row--)
                {
                    if (grid[row, col] != PieceType.None)
                    {
                        grid[writeRow, col] = grid[row, col];
                        if (writeRow != row)
                            grid[row, col] = PieceType.None;
                        writeRow--;
                    }
                }
                for (int row = writeRow; row >= 0; row--)
                    grid[row, col] = PieceType.None;
            }
            Debug.Log("[OyatsuPuzzle] Gravity applied.");
            Debug.Log($"[OyatsuPuzzle] None count after gravity={CountNone(grid)}");
        }

        private void RefillFromTop(PuzzleSession session)
        {
            var grid = session.Board.Grid;
            int count = 0;
            for (int row = 0; row < _size; row++)
                for (int col = 0; col < _size; col++)
                    if (grid[row, col] == PieceType.None)
                    {
                        grid[row, col] = PuzzleBoard.WeightedRandomPiece(_currentStage);
                        count++;
                    }
            Debug.Log($"[OyatsuPuzzle] Refilled from top. count={count}");
            int remaining = CountNone(grid);
            if (remaining > 0)
                Debug.LogError($"[OyatsuPuzzle] None count after refill={remaining} — unexpected None cells remain!");
            else
                Debug.Log($"[OyatsuPuzzle] None count after refill=0");
        }

        private int CountNone(PieceType[,] grid)
        {
            int n = 0;
            for (int r = 0; r < _size; r++)
                for (int c = 0; c < _size; c++)
                    if (grid[r, c] == PieceType.None) n++;
            return n;
        }

        // ──────────────────────────────────────────
        // Fail detection
        // ──────────────────────────────────────────

        private void CheckFail(PuzzleSession session)
        {
            Debug.Log("[OyatsuPuzzle] Fail check started.");
            Debug.Log($"[OyatsuPuzzle] Moves remaining: {session.RemainingMoves}");
            Debug.Log($"[OyatsuPuzzle] Goals completed: {session.IsCleared}");

            if (session.RemainingMoves > 0 || session.IsCleared) return;

            Debug.Log("[OyatsuPuzzle] Stage failed.");
            _inputLocked = true;
            puzzleManager?.FinishFail();
            StartCoroutine(After(0.4f, () =>
            {
                Debug.Log("[OyatsuPuzzle] ShowFail called.");
                screenController?.ShowFail();
            }));
        }

#if UNITY_EDITOR
        // ──────────────────────────────────────────
        // Debug buttons (GamePanel)
        // ──────────────────────────────────────────

        private void CreateDebugButtons(PuzzleSession session)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            float y = 10f;
            foreach (var (piece, label, color) in new (PieceType, string, Color)[]
            {
                (PieceType.Niboshi,        "DEBUG FORCE NI MATCH", new Color(0.2f, 0.5f, 0.9f, 0.9f)),
                (PieceType.Biscuit,        "DEBUG FORCE BI MATCH", new Color(0.9f, 0.6f, 0.2f, 0.9f)),
                (PieceType.CarrotStick,    "DEBUG FORCE CA MATCH", new Color(1.0f, 0.5f, 0.2f, 0.9f)),
                (PieceType.Pudding,        "DEBUG FORCE PU MATCH", new Color(1.0f, 0.85f, 0.3f, 0.9f)),
                (PieceType.HeartMacaron,   "DEBUG FORCE HM MATCH", new Color(0.9f, 0.5f, 0.75f, 0.9f)),
                (PieceType.StrawberryCake, "DEBUG FORCE CK MATCH", new Color(1.0f, 0.4f, 0.5f, 0.9f)),
            })
            {
                CreateForceMatchBtn(canvas, $"DebugForce{PieceDebugLabel(piece)}MatchButton", label, color, new Vector2(10f, y), piece);
                y += 60f;
            }

            CreateForceFailBtn(canvas, new Vector2(10f, y));
            y += 60f;
            CreateFindMoveBtn(canvas, new Vector2(10f, y));
        }

        private void CreateForceFailBtn(Canvas canvas, Vector2 pos)
        {
            const string name = "DebugForceFailButton";
            var existing = canvas.transform.Find(name);
            if (existing != null) Destroy(existing.gameObject);

            var go = CreateDebugBtnGO(canvas, name, pos, new Color(0.5f, 0.2f, 0.7f, 0.9f), "DEBUG FORCE FAIL");
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                var s = puzzleManager?.CurrentSession;
                if (s == null) return;
                Debug.Log("[OyatsuPuzzle] DEBUG FORCE FAIL: forcing Moves to 0.");
                s.DebugSetMovesToZero();
                RefreshMovesLabel(s.RemainingMoves);
                CheckFail(s);
            });
        }

        private void CreateFindMoveBtn(Canvas canvas, Vector2 pos)
        {
            const string name = "DebugFindMoveButton";
            var existing = canvas.transform.Find(name);
            if (existing != null) Destroy(existing.gameObject);

            var go = CreateDebugBtnGO(canvas, name, pos, new Color(0.2f, 0.7f, 0.5f, 0.9f), "DEBUG FIND MOVE");
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                var s = puzzleManager?.CurrentSession;
                if (s == null) return;
                bool found = s.Board.FindFirstPossibleMove(out int fr, out int fc, out int tr, out int tc);
                if (found)
                {
                    var matchesAfter = new List<(Vector2Int, PieceType)>();
                    var g = s.Board.Grid;
                    (g[fr, fc], g[tr, tc]) = (g[tr, tc], g[fr, fc]);
                    matchesAfter = FindMatches(s);
                    (g[fr, fc], g[tr, tc]) = (g[tr, tc], g[fr, fc]);
                    Debug.Log($"[OyatsuPuzzle] Possible move found: from=({fc},{fr}) to=({tc},{tr})");
                    Debug.Log($"[OyatsuPuzzle] Move creates match count={matchesAfter.Count}");
                }
                else
                {
                    Debug.Log("[OyatsuPuzzle] No possible move found.");
                }
            });
        }

        private void CreateForceMatchBtn(Canvas canvas, string name, string label, Color color, Vector2 pos, PieceType target)
        {
            var existing = canvas.transform.Find(name);
            if (existing != null) Destroy(existing.gameObject);

            var go = CreateDebugBtnGO(canvas, name, pos, color, label);
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                var s = puzzleManager?.CurrentSession;
                if (s == null) return;
                var g = s.Board.Grid;
                int mid = _size / 2;
                g[mid, 0] = target;
                g[mid, 1] = target;
                g[mid, 2] = target;
                Debug.Log($"[OyatsuPuzzle] DEBUG FORCE MATCH: set {target} at (0,{mid}) (1,{mid}) (2,{mid})");
                RefreshBoardVisual(s);
                StartCoroutine(ResolveMatchesRoutine(s));
            });
        }

        private static GameObject CreateDebugBtnGO(Canvas canvas, string name, Vector2 pos, Color color, string label)
        {
            var go = new GameObject(name);
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot     = new Vector2(0f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta        = new Vector2(230f, 52f);
            var img = go.AddComponent<Image>();
            img.color = color;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(go.transform, false);
            var lblRT = lblGO.AddComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero;
            lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
            var tmp = lblGO.AddComponent<TextMeshProUGUI>();
            tmp.text          = label;
            tmp.fontSize      = 14f;
            tmp.alignment     = TextAlignmentOptions.Center;
            tmp.color         = Color.white;
            tmp.raycastTarget = false;
            return go;
        }
#endif

        // ──────────────────────────────────────────
        // Board distribution log
        // ──────────────────────────────────────────

        private void LogBoardDistribution(PuzzleSession session)
        {
            var grid   = session.Board.Grid;
            var counts = new Dictionary<PieceType, int>();
            for (int row = 0; row < _size; row++)
                for (int col = 0; col < _size; col++)
                {
                    var t = grid[row, col];
                    if (t == PieceType.None) continue;
                    if (!counts.ContainsKey(t)) counts[t] = 0;
                    counts[t]++;
                }
            Debug.Log("[OyatsuPuzzle] Board distribution:");
            PieceType[] order =
            {
                PieceType.Niboshi, PieceType.Biscuit, PieceType.CarrotStick,
                PieceType.Coin, PieceType.StarCookie, PieceType.Pudding,
                PieceType.HeartMacaron, PieceType.StrawberryCake,
            };
            foreach (var pt in order)
                if (counts.TryGetValue(pt, out int n))
                    Debug.Log($"[OyatsuPuzzle] {pt}={n}");
        }

        // ──────────────────────────────────────────
        // Visual refresh
        // ──────────────────────────────────────────

        private void RefreshBoardVisual(PuzzleSession session)
        {
            if (_bgImages == null) return;
            var grid = session.Board.Grid;
            for (int row = 0; row < _size; row++)
                for (int col = 0; col < _size; col++)
                {
                    PieceType p = grid[row, col];
                    ApplyPieceVisual(_bgImages[row, col], p);
                }
            Debug.Log("[OyatsuPuzzle] Board visual refreshed.");
        }

        // ──────────────────────────────────────────
        // Session events
        // ──────────────────────────────────────────

        private void HandleMovesChanged(int moves) => RefreshMovesLabel(moves);
        private void HandleGoalsChanged(List<PieceGoal> goals) => RefreshGoalLabel(goals);

        private void RefreshMovesLabel(int moves)
        {
            if (moveCountLabelText != null) moveCountLabelText.text = $"のこり {moves}";
        }

        private void SetSupportMessage(string msg)
        {
            if (supportMessageText != null) supportMessageText.text = msg;
        }

        // ──────────────────────────────────────────
        // Support message (応援メッセージ) helpers
        // ──────────────────────────────────────────

        // 状況別メッセージをランダム表示（直前と同じ文言は避ける）。holdSeconds 後に自動消去。
        private void ShowSupport(string[] pool, float holdSeconds)
        {
            if (pool == null || pool.Length == 0) return;
            string msg = PickSupport(pool);
            if (string.IsNullOrEmpty(msg)) return;

            SetSupportMessage(msg);
            _lastSupportMsg = msg;

            if (_supportClearCo != null) StopCoroutine(_supportClearCo);
            _supportClearCo = StartCoroutine(ClearSupportAfter(holdSeconds));
        }

        private string PickSupport(string[] pool)
        {
            if (pool.Length == 1) return pool[0];
            string m = pool[Random.Range(0, pool.Length)];
            for (int i = 0; i < 5 && m == _lastSupportMsg; i++)
                m = pool[Random.Range(0, pool.Length)];
            return m;
        }

        private IEnumerator ClearSupportAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            SetSupportMessage("");
            _supportClearCo = null;
        }

        private static int SumCleared(PuzzleSession session)
        {
            if (session == null || session.Goals == null) return 0;
            int n = 0;
            foreach (var g in session.Goals) n += g.clearedCount;
            return n;
        }

        // どれかの目標が残り1〜2個なら true。
        private static bool AnyGoalAlmost(PuzzleSession session)
        {
            if (session == null || session.Goals == null) return false;
            foreach (var g in session.Goals)
            {
                int remaining = g.Remaining;
                if (remaining >= 1 && remaining <= 2) return true;
            }
            return false;
        }

        // 有効マッチ解決後、優先度に従って1つだけ応援メッセージを出す。
        // 優先度: 残り1手 > あと少しで目標 > 目標進捗 > 連鎖 > 残り手数少(初回のみ) > 通常マッチ
        private void UpdateSupportAfterMove(PuzzleSession session, bool combo, bool goalProgressed)
        {
            if (session == null || session.IsCleared || session.RemainingMoves <= 0) return;

            if (session.RemainingMoves == 1)
                ShowSupport(lastMoveMessages, supportHoldSeconds);
            else if (AnyGoalAlmost(session))
                ShowSupport(almostGoalMessages, supportHoldSeconds);
            else if (goalProgressed)
                ShowSupport(goalProgressMessages, supportHoldSeconds);
            else if (combo)
                ShowSupport(comboMessages, supportHoldSeconds);
            else if (session.RemainingMoves <= 5 && !_lowMovesWarned)
            {
                _lowMovesWarned = true;
                ShowSupport(lowMovesMessages, supportHoldSeconds);
            }
            else
                ShowSupport(matchMessages, supportHoldSeconds);
        }

        private void RefreshGoalLabel(List<PieceGoal> goals)
        {
            // 見出し「あつめるもの」はカード画像側で表示。ここでは画像＋個数スロットのみ更新する。
            if (goalItemSlots == null) return;
            for (int i = 0; i < goalItemSlots.Length; i++)
            {
                var slot = goalItemSlots[i];
                if (slot == null) continue;

                bool use = goals != null && i < goals.Count;
                if (slot.root != null) slot.root.SetActive(use);
                if (!use) continue;

                var g = goals[i];

                if (slot.itemImage != null)
                {
                    var sprite = GoalSpriteFor(g.pieceType);
                    if (sprite != null)
                    {
                        slot.itemImage.sprite         = sprite;
                        slot.itemImage.color          = Color.white;
                        slot.itemImage.preserveAspect = true;
                        slot.itemImage.enabled        = true;
                    }
                    else
                    {
                        // 画像未設定はエラーにせず非表示（個数表示は残す）
                        slot.itemImage.enabled = false;
                    }
                }

                if (slot.countText != null)
                {
                    // 表示は必要数で頭打ち（例: 達成後は 6 / 6）。ゴール判定ロジックには影響しない。
                    int shown = Mathf.Min(g.clearedCount, g.requiredCount);

                    // Rich Text で 現在数 / スラッシュ / 必要数 を色分け（CountText は1つのまま）。
                    string cur = ColorUtility.ToHtmlStringRGB(currentCountColor);
                    string sla = ColorUtility.ToHtmlStringRGB(slashColor);
                    string req = ColorUtility.ToHtmlStringRGB(requiredCountColor);

                    slot.countText.richText = true;
                    slot.countText.text =
                        $"<color=#{cur}>{shown}</color><color=#{sla}> / </color><color=#{req}>{g.requiredCount}</color>";
                }
            }
        }

        private IEnumerator After(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        // ──────────────────────────────────────────
        // Piece helpers
        // ──────────────────────────────────────────

        // PieceType に対応する Sprite を返す。未登録 / null は null（単色フォールバック）。
        private Sprite SpriteFor(PieceType piece)
        {
            if (pieceSprites == null) return null;
            foreach (var ps in pieceSprites)
                if (ps != null && ps.type == piece) return ps.sprite;
            return null;
        }

        // ゴールUI（あつめるもの）・回収演出で使う Sprite。goalSprites（プレーン版）を優先し、
        // 未設定/欠落の種類は SpriteFor（pieceSprites=ピース版）へフォールバック（Missing でもエラーにしない）。
        private Sprite GoalSpriteFor(PieceType piece)
        {
            if (goalSprites != null)
                foreach (var ps in goalSprites)
                    if (ps != null && ps.type == piece && ps.sprite != null) return ps.sprite;
            return SpriteFor(piece);
        }

        // GoalItemSlot到着時の「ふわっと装飾」の中に出すプレーン画像専用の解決。
        // collectDecorItemSprites だけを参照する（goalSprites / pieceSprites には依存しない）。
        // 未設定の種類は null を返し、呼び出し側(SpawnSparkleBurst)で星(sparkleSprite)へフォールバックさせる。
        private Sprite CollectDecorSpriteFor(PieceType piece)
        {
            if (collectDecorItemSprites != null)
                foreach (var ps in collectDecorItemSprites)
                    if (ps != null && ps.type == piece && ps.sprite != null) return ps.sprite;
            return null;
        }

        // セル Image にピースの見た目を適用する。
        // Sprite があれば画像表示、無ければ従来の単色表示にフォールバック。
        private void ApplyPieceVisual(Image img, PieceType piece)
        {
            if (img == null) return;
            var sprite = SpriteFor(piece);
            if (sprite != null)
            {
                img.sprite         = sprite;
                img.color          = Color.white;
                img.preserveAspect = true;
            }
            else
            {
                img.sprite = null;
                img.color  = PieceColor(piece);
            }
        }

        private static string PieceDebugLabel(PieceType t)
        {
            switch (t)
            {
                case PieceType.Niboshi:        return "NI";
                case PieceType.Biscuit:        return "BI";
                case PieceType.CarrotStick:    return "CA";
                case PieceType.Coin:           return "CO";
                case PieceType.StarCookie:     return "ST";
                case PieceType.Pudding:        return "PU";
                case PieceType.HeartMacaron:   return "HM";
                case PieceType.StrawberryCake: return "CK";
                default:                       return "--";
            }
        }

        private static Color PieceColor(PieceType t) => t switch
        {
            PieceType.Niboshi        => new Color(0.60f, 0.75f, 0.90f),
            PieceType.Biscuit        => new Color(0.90f, 0.80f, 0.60f),
            PieceType.CarrotStick    => new Color(1.00f, 0.60f, 0.30f),
            PieceType.StrawberryCake => new Color(1.00f, 0.50f, 0.60f),
            PieceType.Pudding        => new Color(1.00f, 0.85f, 0.45f),
            PieceType.Coin           => new Color(0.95f, 0.85f, 0.20f),
            PieceType.StarCookie     => new Color(0.80f, 0.90f, 0.50f),
            PieceType.HeartMacaron   => new Color(0.95f, 0.60f, 0.80f),
            _                        => new Color(0.80f, 0.80f, 0.80f),
        };
    }
}
