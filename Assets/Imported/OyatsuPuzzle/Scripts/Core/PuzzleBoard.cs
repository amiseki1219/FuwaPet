using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OyatsuPuzzle
{
    // 盤面ロジック（MonoBehaviour 不依存・純粋C#）
    public class PuzzleBoard
    {
        public int Size { get; }
        public PieceType[,] Grid { get; private set; }

        private readonly int _stageNumber;

        public event Action<List<(int row, int col, PieceType type)>> OnPiecesCleared;
        public event Action OnBoardChanged;

        public PuzzleBoard(int size, int stageNumber = 0)
        {
            Size         = size;
            _stageNumber = stageNumber;
            Grid         = new PieceType[size, size];
            GenerateNoMatchBoard();
        }

        // ──────────────────────────────────────────
        // 公開API
        // ──────────────────────────────────────────

        public bool TrySwap(int r1, int c1, int r2, int c2)
        {
            if (!IsAdjacent(r1, c1, r2, c2)) return false;
            Swap(r1, c1, r2, c2);
            if (!HasAnyMatch())
            {
                Swap(r1, c1, r2, c2);
                return false;
            }
            ProcessMatches();
            return true;
        }

        // ──────────────────────────────────────────
        // 初期盤面生成（マッチなし保証）
        // ──────────────────────────────────────────

        private void GenerateNoMatchBoard()
        {
            const int maxBoardRetries = 10;
            for (int attempt = 1; attempt <= maxBoardRetries; attempt++)
            {
                FillNoMatch();
                if (HasAnyMatch())
                {
                    Debug.Log($"[OyatsuPuzzle] Initial board match detected. Regenerating board. attempt={attempt + 1}");
                    continue;
                }

                LogBoardDistribution();

                bool tooMany = CheckTargetPieceCount(attempt);
                if (tooMany) continue;

                bool hasMoves = HasAnyPossibleMove();
                Debug.Log($"[OyatsuPuzzle] Possible move validation: {hasMoves}");
                if (!hasMoves)
                {
                    Debug.Log($"[OyatsuPuzzle] No possible moves. Regenerating board. attempt={attempt + 1}");
                    continue;
                }

                Debug.Log($"[OyatsuPuzzle] Initial board generated. attempt={attempt}");
                int initMatches = FindMatches().Count;
                Debug.Log($"[OyatsuPuzzle] Initial board validation: matches={initMatches}");
                Debug.Log("[OyatsuPuzzle] Target piece distribution OK.");
                return;
            }
            Debug.LogWarning("[OyatsuPuzzle] Initial board still has issues after max retries. Using last board.");
            LogBoardDistribution();
        }

        // Returns true if any target piece exceeds its cap (triggers re-generation).
        private bool CheckTargetPieceCount(int attempt)
        {
            var counts = CountPieces();
            switch (_stageNumber)
            {
                case 1:
                    return CheckCap(counts, attempt, PieceType.Niboshi, 7);
                case 2:
                    return CheckCap(counts, attempt, PieceType.Niboshi, 7)
                        || CheckCap(counts, attempt, PieceType.Biscuit, 7);
                case 3:
                    return CheckCap(counts, attempt, PieceType.Coin, 8);
                case 4:
                    return CheckCap(counts, attempt, PieceType.Niboshi, 9)
                        || CheckCap(counts, attempt, PieceType.Biscuit, 9)
                        || CheckCap(counts, attempt, PieceType.CarrotStick, 9);
                case 5:
                    return CheckCap(counts, attempt, PieceType.Pudding, 9)
                        || CheckCap(counts, attempt, PieceType.HeartMacaron, 9)
                        || CheckCap(counts, attempt, PieceType.StrawberryCake, 9);
                default:
                    return false;
            }
        }

        private bool CheckCap(Dictionary<PieceType, int> counts, int attempt, PieceType piece, int max)
        {
            counts.TryGetValue(piece, out int n);
            if (n <= max) return false;
            Debug.Log($"[OyatsuPuzzle] Target piece too many. Regenerating board. stage={_stageNumber} piece={piece} count={n} max={max} attempt={attempt + 1}");
            return true;
        }

        private Dictionary<PieceType, int> CountPieces()
        {
            var counts = new Dictionary<PieceType, int>();
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                {
                    var t = Grid[r, c];
                    if (t == PieceType.None) continue;
                    if (!counts.ContainsKey(t)) counts[t] = 0;
                    counts[t]++;
                }
            return counts;
        }

        private void FillNoMatch()
        {
            // Initialize grid to None so WouldMatchAt only sees already-placed cells.
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    Grid[r, c] = PieceType.None;

            for (int r = 0; r < Size; r++)
            {
                for (int c = 0; c < Size; c++)
                {
                    PieceType pick = PieceType.Niboshi;
                    for (int retry = 0; retry < 20; retry++)
                    {
                        pick = WeightedRandomPiece(_stageNumber);
                        if (!WouldMatchAt(r, c, pick)) break;
                    }
                    Grid[r, c] = pick;
                }
            }
        }

        // Check whether placing t at (r,c) would form a run of 3+ in any direction.
        // Only checks already-filled cells (left/up) — right/down are None so they never match.
        private bool WouldMatchAt(int r, int c, PieceType t)
        {
            if (t == PieceType.None) return false;

            // Horizontal: count left neighbours that equal t
            int left = 0;
            for (int dc = 1; c - dc >= 0 && Grid[r, c - dc] == t; dc++) left++;
            // Right neighbours that have already been placed (None if not yet placed)
            int right = 0;
            for (int dc = 1; c + dc < Size && Grid[r, c + dc] == t; dc++) right++;
            if (left + 1 + right >= 3) return true;

            // Vertical: count upward neighbours
            int up = 0;
            for (int dr = 1; r - dr >= 0 && Grid[r - dr, c] == t; dr++) up++;
            // Downward neighbours (None if not yet placed)
            int down = 0;
            for (int dr = 1; r + dr < Size && Grid[r + dr, c] == t; dr++) down++;
            if (up + 1 + down >= 3) return true;

            return false;
        }

        // ──────────────────────────────────────────
        // 連鎖処理
        // ──────────────────────────────────────────

        private void ProcessMatches()
        {
            int safety = 50;
            while (HasAnyMatch() && safety-- > 0)
            {
                var matched = FindMatches();
                OnPiecesCleared?.Invoke(matched);
                foreach (var (r, c, _) in matched)
                    Grid[r, c] = PieceType.None;
                Fall();
                Refill();
                OnBoardChanged?.Invoke();
            }
        }

        private void Fall()
        {
            for (int c = 0; c < Size; c++)
                for (int r = Size - 1; r >= 0; r--)
                    if (Grid[r, c] == PieceType.None)
                        FallColumn(r, c);
        }

        private void FallColumn(int emptyRow, int col)
        {
            for (int r = emptyRow - 1; r >= 0; r--)
            {
                if (Grid[r, col] != PieceType.None)
                {
                    Grid[emptyRow, col] = Grid[r, col];
                    Grid[r, col] = PieceType.None;
                    emptyRow--;
                }
            }
        }

        private void Refill()
        {
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    if (Grid[r, c] == PieceType.None)
                        Grid[r, c] = WeightedRandomPiece(_stageNumber);
        }

        // ──────────────────────────────────────────
        // マッチ検出（3個以上）
        // ──────────────────────────────────────────

        public bool HasAnyMatch() => FindMatches().Count > 0;

        public List<(int row, int col, PieceType type)> FindMatches()
        {
            var matched = new HashSet<(int, int)>();

            for (int r = 0; r < Size; r++)
            {
                int c = 0;
                while (c < Size)
                {
                    int len = RunLength(r, c, 0, 1);
                    if (len >= 3)
                        for (int k = 0; k < len; k++) matched.Add((r, c + k));
                    c += len;
                }
            }

            for (int c = 0; c < Size; c++)
            {
                int r = 0;
                while (r < Size)
                {
                    int len = RunLength(r, c, 1, 0);
                    if (len >= 3)
                        for (int k = 0; k < len; k++) matched.Add((r + k, c));
                    r += len;
                }
            }

            var result = new List<(int, int, PieceType)>();
            foreach (var (r, c) in matched)
                result.Add((r, c, Grid[r, c]));
            return result;
        }

        private int RunLength(int r, int c, int dr, int dc)
        {
            PieceType t = Grid[r, c];
            if (t == PieceType.None) return 1; // None is never part of a match run
            int len = 1;
            while (true)
            {
                int nr = r + dr * len;
                int nc = c + dc * len;
                if (nr < 0 || nr >= Size || nc < 0 || nc >= Size) break;
                if (Grid[nr, nc] != t) break;
                len++;
            }
            return len;
        }

        // ──────────────────────────────────────────
        // WeightedRandom（ステージ別出現率）
        // ──────────────────────────────────────────

        public static PieceType WeightedRandomPiece(int stage)
        {
            var table = GetWeightTable(stage);
            int total = 0;
            foreach (var (_, w) in table) total += w;
            int roll = Random.Range(0, total);
            int acc  = 0;
            foreach (var (piece, w) in table)
            {
                acc += w;
                if (roll < acc) return piece;
            }
            return table[0].piece;
        }

        public static (PieceType piece, int weight)[] GetWeightTable(int stage) => stage switch
        {
            // Goal: Niboshi x5  — N≈24%
            1 => new[]
            {
                (PieceType.Niboshi,        24),
                (PieceType.Biscuit,        16),
                (PieceType.CarrotStick,    15),
                (PieceType.Coin,           15),
                (PieceType.StarCookie,     15),
                (PieceType.Pudding,        15),
            },
            // Goal: Niboshi x5, Biscuit x3  — N+B≈44%
            2 => new[]
            {
                (PieceType.Niboshi,        22),
                (PieceType.Biscuit,        22),
                (PieceType.CarrotStick,    14),
                (PieceType.Coin,           14),
                (PieceType.StarCookie,     14),
                (PieceType.Pudding,        14),
            },
            // Goal: Coin x8  — O≈26%
            3 => new[]
            {
                (PieceType.Coin,           26),
                (PieceType.Niboshi,        16),
                (PieceType.Biscuit,        16),
                (PieceType.CarrotStick,    16),
                (PieceType.StarCookie,     13),
                (PieceType.Pudding,        13),
            },
            // Goal: Niboshi x6, Biscuit x4, CarrotStick x4  — N+B+C≈54%
            4 => new[]
            {
                (PieceType.Niboshi,        18),
                (PieceType.Biscuit,        18),
                (PieceType.CarrotStick,    18),
                (PieceType.Coin,           16),
                (PieceType.StarCookie,     15),
                (PieceType.Pudding,        15),
            },
            // Goal: Pudding x5, HeartMacaron x5, StrawberryCake x5  — P+H+K≈54%
            5 => new[]
            {
                (PieceType.Pudding,        18),
                (PieceType.HeartMacaron,   18),
                (PieceType.StrawberryCake, 18),
                (PieceType.Niboshi,        12),
                (PieceType.Biscuit,        12),
                (PieceType.CarrotStick,    11),
                (PieceType.Coin,           11),
            },
            // Fallback: balanced across all 8 pieces
            _ => new[]
            {
                (PieceType.Niboshi,        13),
                (PieceType.Biscuit,        13),
                (PieceType.CarrotStick,    13),
                (PieceType.StrawberryCake, 13),
                (PieceType.Pudding,        12),
                (PieceType.Coin,           12),
                (PieceType.StarCookie,     12),
                (PieceType.HeartMacaron,   12),
            },
        };

        // ──────────────────────────────────────────
        // ログ
        // ──────────────────────────────────────────

        private void LogBoardDistribution()
        {
            var counts = CountPieces();
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
        // 有効手判定
        // ──────────────────────────────────────────

        public bool HasAnyPossibleMove() => FindFirstPossibleMove(out _, out _, out _, out _);

        // Returns true and fills from/to coordinates when a valid swap exists.
        public bool FindFirstPossibleMove(out int fromRow, out int fromCol, out int toRow, out int toCol)
        {
            for (int r = 0; r < Size; r++)
            {
                for (int c = 0; c < Size; c++)
                {
                    if (c + 1 < Size && TrialSwapHasMatch(r, c, r, c + 1))
                    {
                        fromRow = r; fromCol = c; toRow = r; toCol = c + 1;
                        return true;
                    }
                    if (r + 1 < Size && TrialSwapHasMatch(r, c, r + 1, c))
                    {
                        fromRow = r; fromCol = c; toRow = r + 1; toCol = c;
                        return true;
                    }
                }
            }
            fromRow = fromCol = toRow = toCol = -1;
            return false;
        }

        private bool TrialSwapHasMatch(int r1, int c1, int r2, int c2)
        {
            (Grid[r1, c1], Grid[r2, c2]) = (Grid[r2, c2], Grid[r1, c1]);
            bool matched = HasAnyMatch();
            (Grid[r1, c1], Grid[r2, c2]) = (Grid[r2, c2], Grid[r1, c1]);
            return matched;
        }

        // ──────────────────────────────────────────
        // シャッフル（詰み解消）
        // ──────────────────────────────────────────

        public void ShuffleBoardUntilPlayable()
        {
            const int maxShuffleAttempts = 20;

            var pieces = new List<PieceType>(Size * Size);
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    pieces.Add(Grid[r, c]);

            for (int attempt = 1; attempt <= maxShuffleAttempts; attempt++)
            {
                Debug.Log($"[OyatsuPuzzle] Shuffle board attempt={attempt}");

                // Fisher-Yates shuffle
                for (int i = pieces.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    (pieces[i], pieces[j]) = (pieces[j], pieces[i]);
                }

                int idx = 0;
                for (int r = 0; r < Size; r++)
                    for (int c = 0; c < Size; c++)
                        Grid[r, c] = pieces[idx++];

                bool noMatch    = !HasAnyMatch();
                bool hasMove    = HasAnyPossibleMove();
                Debug.Log($"[OyatsuPuzzle] Shuffle result: matches={(noMatch ? 0 : 1)}+ possibleMove={hasMove}");

                if (noMatch && hasMove)
                {
                    Debug.Log("[OyatsuPuzzle] Board shuffled successfully.");
                    return;
                }
            }

            Debug.LogWarning("[OyatsuPuzzle] Shuffle failed. Regenerating board.");
            FillNoMatch();
        }

        // ──────────────────────────────────────────
        // ユーティリティ
        // ──────────────────────────────────────────

        private void Swap(int r1, int c1, int r2, int c2)
            => (Grid[r1, c1], Grid[r2, c2]) = (Grid[r2, c2], Grid[r1, c1]);

        private bool IsAdjacent(int r1, int c1, int r2, int c2)
            => Math.Abs(r1 - r2) + Math.Abs(c1 - c2) == 1;
    }
}
