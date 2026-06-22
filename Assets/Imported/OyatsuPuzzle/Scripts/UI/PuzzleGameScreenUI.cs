using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Random = UnityEngine.Random;

namespace OyatsuPuzzle
{
    internal sealed class SwipeCellHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public int BoardRow { get; set; }
        public int BoardCol { get; set; }
        public Action<int, int, Vector2> OnSwipeDown { get; set; }
        public Action<int, int, Vector2, Vector2> OnSwipeUp { get; set; }

        void IPointerDownHandler.OnPointerDown(PointerEventData e)
            => OnSwipeDown?.Invoke(BoardRow, BoardCol, e.position);

        void IPointerUpHandler.OnPointerUp(PointerEventData e)
            => OnSwipeUp?.Invoke(BoardRow, BoardCol, e.pressPosition, e.position);
    }

    public class PuzzleGameScreenUI : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField] private TMP_Text stageLabelText;
        [SerializeField] private TMP_Text moveCountLabelText;
        [SerializeField] private TMP_Text goalLabelText;
        [SerializeField] private TMP_Text supportMessageText;

        [Header("Board")]
        [SerializeField] private Transform boardRoot;
        [SerializeField] private float swipeThreshold = 5f;

        [Header("Piece Sprites")]
        [Tooltip("PieceType ごとのピース画像。未設定(None)の種類は PieceColor の単色表示にフォールバックします。")]
        [SerializeField] private PieceSprite[] pieceSprites;

        [System.Serializable]
        public class PieceSprite
        {
            public PieceType type;
            public Sprite    sprite;
        }

        [Header("Buttons")]
        [SerializeField] private Button pauseButton;

        [Header("References")]
        [SerializeField] private PuzzleManager          puzzleManager;
        [SerializeField] private PuzzleScreenController screenController;

        private Image[,]           _bgImages;
        private int                _size;
        private int                _selRow = -1;
        private int                _selCol = -1;
        private int                _currentStage;
        private bool               _inputLocked;

        private static readonly Vector3 ScaleNormal   = Vector3.one;
        private static readonly Vector3 ScaleSelected = new Vector3(1.25f, 1.25f, 1f);

        private void Awake()
        {
            if (pauseButton != null)
                pauseButton.onClick.AddListener(() => Debug.Log("[OyatsuPuzzle] Pause tapped."));
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
                Destroy(boardRoot.GetChild(i).gameObject);

            _bgImages = new Image[_size, _size];
            _selRow   = -1;
            _selCol   = -1;

            var grid = boardRoot.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = _size;
                grid.cellSize        = new Vector2(150f, 150f);
                grid.spacing         = new Vector2(8f, 8f);
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

            Debug.Log($"[OyatsuPuzzle] Cell created: {col},{row} piece={PieceDebugLabel(piece)}");
        }

        // ──────────────────────────────────────────
        // Click-select swap
        // ──────────────────────────────────────────

        private void OnCellClicked(int row, int col)
        {
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
            TrySwapCells(r1, c1, row, col);
        }

        private void ApplySelectedScale(int row, int col, bool selected)
        {
            if (_bgImages == null) return;
            _bgImages[row, col].transform.localScale = selected ? ScaleSelected : ScaleNormal;
        }

        // ──────────────────────────────────────────
        // Swipe input
        // ──────────────────────────────────────────

        private void HandleSwipeDown(int row, int col, Vector2 pos)
        {
            if (_inputLocked) return;
            var session = puzzleManager?.CurrentSession;
            if (session == null || !session.IsActive) return;
            string pieceLbl = PieceDebugLabel(session.Board.Grid[row, col]);
            Debug.Log($"[OyatsuPuzzle] Swipe pointer down: cell=({col},{row}) piece={pieceLbl} pos=({(int)pos.x},{(int)pos.y})");
        }

        private void HandleSwipeUp(int row, int col, Vector2 downPos, Vector2 upPos)
        {
            if (_inputLocked) return;
            var session = puzzleManager?.CurrentSession;
            if (session == null || !session.IsActive) return;

            Vector2 delta = upPos - downPos;
            float absX = Mathf.Abs(delta.x);
            float absY = Mathf.Abs(delta.y);
            float max  = Mathf.Max(absX, absY);

            Debug.Log($"[OyatsuPuzzle] Swipe delta x={delta.x:F0} y={delta.y:F0} max={max:F0} threshold={swipeThreshold}");

            if (max < swipeThreshold)
            {
                Debug.Log($"[OyatsuPuzzle] Swipe ignored. Too short. max={max:F0} threshold={swipeThreshold}");
                return;
            }

            int toRow = row, toCol = col;
            if (absX >= absY)
            {
                if (delta.x > 0) { toCol = col + 1; Debug.Log("[OyatsuPuzzle] Swipe direction: Right"); }
                else              { toCol = col - 1; Debug.Log("[OyatsuPuzzle] Swipe direction: Left"); }
            }
            else
            {
                if (delta.y > 0) { toRow = row - 1; Debug.Log("[OyatsuPuzzle] Swipe direction: Up"); }
                else              { toRow = row + 1; Debug.Log("[OyatsuPuzzle] Swipe direction: Down"); }
            }

            TrySwapCells(row, col, toRow, toCol);
        }

        // ──────────────────────────────────────────
        // Swap → ResolveMatches
        // ──────────────────────────────────────────

        private void TrySwapCells(int fromRow, int fromCol, int toRow, int toCol)
        {
            var session = puzzleManager?.CurrentSession;
            if (session == null) return;

            int dx = Mathf.Abs(fromCol - toCol);
            int dy = Mathf.Abs(fromRow - toRow);
            if (dx + dy != 1)
            {
                Debug.Log($"[OyatsuPuzzle] Swap rejected. Not adjacent. dx={dx} dy={dy}");
                return;
            }

            var grid = session.Board.Grid;
            Debug.Log($"[OyatsuPuzzle] TrySwapCells: from=({fromCol},{fromRow}) to=({toCol},{toRow})");
            Debug.Log($"[OyatsuPuzzle] Before: {PieceDebugLabel(grid[fromRow, fromCol])} <-> {PieceDebugLabel(grid[toRow, toCol])}");

            (grid[fromRow, fromCol], grid[toRow, toCol]) = (grid[toRow, toCol], grid[fromRow, fromCol]);
            Debug.Log("[OyatsuPuzzle] Swap preview applied.");
            RefreshBoardVisual(session);

            var matches = FindMatches(session);
            if (matches.Count == 0)
            {
                Debug.Log("[OyatsuPuzzle] No match found after swap.");
                (grid[fromRow, fromCol], grid[toRow, toCol]) = (grid[toRow, toCol], grid[fromRow, fromCol]);
                RefreshBoardVisual(session);
                Debug.Log("[OyatsuPuzzle] Swap reverted.");
                Debug.Log($"[OyatsuPuzzle] Moves unchanged: {session.RemainingMoves}");
                return;
            }

            Debug.Log($"[OyatsuPuzzle] Match found after swap. count={matches.Count}");
            Debug.Log("[OyatsuPuzzle] Swap confirmed.");

            int prev = session.RemainingMoves;
            session.ConsumeMove();
            Debug.Log($"[OyatsuPuzzle] Moves decreased: {prev} -> {session.RemainingMoves}");

            _inputLocked = true;
            Debug.Log("[OyatsuPuzzle] Input locked.");
            ResolveMatches(session, matches);
        }

        // ──────────────────────────────────────────
        // ResolveMatches — cascade loop
        // ──────────────────────────────────────────

        private void ResolveMatches(PuzzleSession session,
            List<(Vector2Int cell, PieceType piece)> firstMatches = null)
        {
            const int maxCascade = 10;
            int cascade = 0;

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

                Debug.Log($"[OyatsuPuzzle] Cascade {cascade + 1} match count={matches.Count}");

                bool cleared = ApplyGoalProgress(session, matches);
                if (cleared) return;

                ClearMatchedCells(session, matches);
                ApplyGravity(session);
                RefillFromTop(session);
                RefreshBoardVisual(session);
                Debug.Log("[OyatsuPuzzle] Board visual refreshed after gravity refill.");

                cascade++;
            }

            if (cascade >= maxCascade)
                Debug.LogWarning("[OyatsuPuzzle] Cascade limit reached.");

            var postCheck = FindMatches(session);
            Debug.Log($"[OyatsuPuzzle] Post-resolve validation: matches={postCheck.Count}");

            bool hasMoves = session.Board.HasAnyPossibleMove();
            Debug.Log($"[OyatsuPuzzle] Possible move validation: {hasMoves}");
            if (!hasMoves)
            {
                Debug.Log("[OyatsuPuzzle] No possible moves after resolve. Shuffling board.");
                SetSupportMessage("No moves. Shuffling...");
                session.Board.ShuffleBoardUntilPlayable();
                RefreshBoardVisual(session);
                StartCoroutine(After(1.5f, () => SetSupportMessage("")));
            }

            // Unlock before fail-check so that game-over lock is set by CheckFail if needed.
            _inputLocked = false;
            Debug.Log("[OyatsuPuzzle] Input unlocked.");

            CheckFail(session);
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
                ResolveMatches(s);
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
            if (moveCountLabelText != null) moveCountLabelText.text = $"Moves {moves}";
        }

        private void SetSupportMessage(string msg)
        {
            if (supportMessageText != null) supportMessageText.text = msg;
        }

        private void RefreshGoalLabel(List<PieceGoal> goals)
        {
            if (goalLabelText == null) return;
            var sb = new StringBuilder("Goal:\n");
            foreach (var g in goals)
#if UNITY_EDITOR
                sb.AppendLine($"  {g.pieceType.ToEnglishName()} {g.clearedCount} / {g.requiredCount}  Target: {PieceDebugLabel(g.pieceType)}");
#else
                sb.AppendLine($"  {g.pieceType.ToEnglishName()} {g.clearedCount} / {g.requiredCount}");
#endif
            goalLabelText.text = sb.ToString();
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
