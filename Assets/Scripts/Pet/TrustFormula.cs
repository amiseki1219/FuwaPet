using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 信頼度（trust / なかよし度）の「累計pt ⇔ レベル」変換を一箇所に集約する静的クラス。
    /// requirements.md §4「信頼度システム（Lv.100・指数曲線）」を正とする。
    ///
    /// ・最高 Lv.100。Lv.1 の累計必要pt は 0。
    /// ・10レベル刻みの節目（Lv10/20/.../100）の累計必要ptは requirements.md の値に完全一致（ハードコードで固定）。
    /// ・節目と節目の間（および Lv1〜10 の間）は「単調キュービックスプライン補間
    ///   （Fritsch–Carlson / PCHIP 系の単調エルミート補間）」で補間して埋める。
    ///     - 節目11点（Lv1=0 と上記10個）を必ず通る。
    ///     - 区間をまたいで傾きが滑らかに接続するため、節目直後の「必要ptが急に軽くなる段差」が出ない。
    ///     - 全区間の傾き（微分値）が非負に保たれるため、厳密な単調増加が保証される。
    /// ・起動時（静的コンストラクタ）に Lv1〜100 の累計ptテーブルを1回だけ生成して保持し、
    ///   以降の判定はこのテーブル（配列）を参照する方式。
    /// </summary>
    public static class TrustFormula
    {
        public const int MaxLevel = 100;

        // ─── 節目（アンカー）───────────────────────────
        // requirements.md §4 の累計必要pt。ここは仕様の正の値なので絶対に変更しない。
        private static readonly int[] AnchorLevels = { 1, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };
        private static readonly int[] AnchorPts =
            { 0, 800, 3000, 8000, 20000, 45000, 90000, 155000, 220000, 270000, 296000 };

        /// <summary>
        /// Lv1〜100 の累計必要ptテーブル。
        /// index と Lv の対応: CumulativePts[i] = Lv(i+1) の累計必要pt
        ///   → CumulativePts[0]  = Lv1   = 0
        ///     CumulativePts[9]  = Lv10  = 800
        ///     CumulativePts[99] = Lv100 = 296000
        /// </summary>
        private static readonly int[] CumulativePts = BuildTable();

        // ─── テーブル生成（起動時1回・PCHIP単調エルミート補間）──────────
        private static int[] BuildTable()
        {
            int m = AnchorLevels.Length; // 節目の数（11）
            double[] x = new double[m];
            double[] y = new double[m];
            for (int i = 0; i < m; i++) { x[i] = AnchorLevels[i]; y[i] = AnchorPts[i]; }

            // 区間幅 h と 割線傾き delta
            double[] h = new double[m - 1];
            double[] delta = new double[m - 1];
            for (int i = 0; i < m - 1; i++)
            {
                h[i] = x[i + 1] - x[i];
                delta[i] = (y[i + 1] - y[i]) / h[i];
            }

            // 各節目での傾き d[]（Fritsch–Carlson）
            double[] d = new double[m];
            for (int k = 1; k < m - 1; k++)
            {
                // 割線の符号が反転／0 の節目は傾き0（極値）→ オーバーシュート防止
                if (Sign(delta[k - 1]) != Sign(delta[k]) || delta[k - 1] == 0.0 || delta[k] == 0.0)
                {
                    d[k] = 0.0;
                }
                else
                {
                    // 重み付き調和平均（区間幅で重み付け）
                    double w1 = 2.0 * h[k] + h[k - 1];
                    double w2 = h[k] + 2.0 * h[k - 1];
                    d[k] = (w1 + w2) / (w1 / delta[k - 1] + w2 / delta[k]);
                }
            }
            // 端点は片側3点式＋形状保存クリップ
            d[0]     = EndpointSlope(h[0], h[1], delta[0], delta[1]);
            d[m - 1] = EndpointSlope(h[m - 2], h[m - 3], delta[m - 2], delta[m - 3]);

            // 各Lvを三次エルミートで評価
            int[] table = new int[MaxLevel]; // index 0..99 = Lv1..Lv100
            for (int lv = 1; lv <= MaxLevel; lv++)
            {
                int k = 0;
                while (k < m - 2 && lv > x[k + 1]) k++;

                double hk = h[k];
                double t = (lv - x[k]) / hk;
                double t2 = t * t;
                double t3 = t2 * t;

                double h00 =  2.0 * t3 - 3.0 * t2 + 1.0;
                double h10 =        t3 - 2.0 * t2 + t;
                double h01 = -2.0 * t3 + 3.0 * t2;
                double h11 =        t3 -       t2;

                double val = y[k] * h00 + hk * d[k] * h10 + y[k + 1] * h01 + hk * d[k + 1] * h11;
                table[lv - 1] = (int)Math.Round(val);
            }

            // 節目を厳密にセット（丸め誤差の保険。仕様値と必ず一致させる）
            for (int i = 0; i < m; i++) table[AnchorLevels[i] - 1] = AnchorPts[i];

            return table;
        }

        private static int Sign(double v) => v > 0.0 ? 1 : (v < 0.0 ? -1 : 0);

        // PCHIP 端点傾き（scipy PchipInterpolator と同一の片側式＋形状保存クリップ）
        private static double EndpointSlope(double h0, double h1, double d0, double d1)
        {
            double dd = ((2.0 * h0 + h1) * d0 - h0 * d1) / (h0 + h1);
            if (Sign(dd) != Sign(d0))
                dd = 0.0;                       // 端点で符号が反転するなら0にクリップ
            else if (Sign(d0) != Sign(d1) && Math.Abs(dd) > 3.0 * Math.Abs(d0))
                dd = 3.0 * d0;                  // 行き過ぎを 3×割線 に制限
            return dd;
        }

        // ─── 公開メソッド（引数 trust = 現在の累計pt）──────────

        /// <summary>現在Lv（1〜100）を返す。</summary>
        public static int GetLevel(int trust)
        {
            if (trust <= 0) return 1;
            if (trust >= CumulativePts[MaxLevel - 1]) return MaxLevel;

            // CumulativePts[L-1] <= trust を満たす最大の L を返す
            for (int L = MaxLevel; L >= 1; L--)
            {
                if (trust >= CumulativePts[L - 1])
                    return L;
            }
            return 1;
        }

        /// <summary>次のLvまでの残りpt。Lv100（カンスト）時は 0 を返す。</summary>
        public static int GetPtsToNextLevel(int trust)
        {
            int level = GetLevel(trust);
            if (level >= MaxLevel) return 0;

            int nextStart = CumulativePts[level]; // Lv(level+1) の累計必要pt
            int remaining = nextStart - trust;
            return remaining > 0 ? remaining : 0;
        }

        /// <summary>現在Lv内での進捗率 0〜1（float）。カンスト時は 1。</summary>
        public static float GetFillAmount(int trust)
        {
            int level = GetLevel(trust);
            if (level >= MaxLevel) return 1f;

            int currentStart = CumulativePts[level - 1];
            int nextStart    = CumulativePts[level];
            float fill = (float)(trust - currentStart) / (nextStart - currentStart);
            return Mathf.Clamp01(fill);
        }

        /// <summary>Lv100（カンスト）到達なら true。</summary>
        public static bool IsMaxLevel(int trust)
        {
            return trust >= CumulativePts[MaxLevel - 1];
        }
    }
}
