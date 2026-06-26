using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Random = UnityEngine.Random;

namespace OyatsuPuzzle
{
    // PuzzleStageClearOverlayPanel の背景キラキラ装飾。
    // パネル表示中、画面のあちこちに装飾スプライトが散らばり、その場でふわふわ揺れ・回転・拡大縮小して
    // キラキラ見せる（ループ）。PuzzleClearPanel の「一瞬パーン」クラッカー演出とは別物。
    //
    // ・このコンポーネントを PuzzleStageClearOverlayPanel に付ける。
    // ・OnEnable で生成・再生 / OnDisable で停止・削除（= ShowStageClearResult で表示、HideAll等で非表示）。
    //   そのため PuzzleScreenController 側を変更する必要はない。
    // ・スプライトは PuzzleClearPanel の PuzzleClearCrackerEffectUI が持つ7種を再利用（crackerSpriteSource）。
    //   個別に差し替えたい場合のみ sparkleSpritesOverride を使う。
    // ・raycastTarget=false・背面(SetAsFirstSibling)配置で主要UIの邪魔をしない。
    public class PuzzleStageClearSparkleDecorUI : MonoBehaviour
    {
        [Header("Sprite Source (6種を再利用)")]
        [Tooltip("PuzzleClearPanel の PuzzleClearCrackerEffectUI を割り当て。その clearCrackerSprites(6種) を再利用する。")]
        [SerializeField] private PuzzleClearCrackerEffectUI crackerSpriteSource;
        [Tooltip("任意。ここに入れた場合はこちらを優先使用（未設定なら crackerSpriteSource の6種を使う）。")]
        [SerializeField] private Sprite[] sparkleSpritesOverride;

        [Header("Layout")]
        [Tooltip("装飾を出す親。未設定なら自身(PuzzleStageClearOverlayPanel)の RectTransform を使用。")]
        [SerializeField] private RectTransform decorParent;
        [Tooltip("散らす個数。Inspectorで調整可（24〜30推奨）。")]
        [SerializeField] private int stageClearSparkleCount = 26;
        [Tooltip("散布範囲(px・中心基準の全幅/全高)。この矩形内にランダム配置。")]
        [SerializeField] private Vector2 stageClearSparkleAreaSize = new Vector2(680f, 1240f);
        [Tooltip("散布範囲の中心オフセット（パネル中心基準。x右+/y上+）。")]
        [SerializeField] private Vector2 stageClearSparkleAreaOffset = Vector2.zero;
        [Tooltip("1個の最小サイズ(px)。")]
        [SerializeField] private float stageClearSparkleSizeMin = 34f;
        [Tooltip("1個の最大サイズ(px)。")]
        [SerializeField] private float stageClearSparkleSizeMax = 64f;
        [Tooltip("装飾の不透明度(0〜1)。背景装飾なので少し透かすと馴染む。")]
        [Range(0f, 1f)]
        [SerializeField] private float stageClearSparkleAlpha = 0.85f;

        [Header("Avoid UI (主要UIの上に出さない)")]
        [Tooltip("この矩形(UI)の上には装飾を出さない。RewardPanel/各ボタン/テキスト/StageProgressBg/キャラ等を割り当て。")]
        [SerializeField] private RectTransform[] stageClearSparkleAvoidTargets;
        [Tooltip("避け判定の余白(px)。各UI矩形をこの分だけ外側に広げて避ける。")]
        [SerializeField] private float stageClearSparkleAvoidPadding = 24f;
        [Tooltip("1個あたりの配置リトライ回数。全部UIに被るならその個体はスキップ（=端・余白だけに出る）。")]
        [SerializeField] private int stageClearSparklePlacementAttempts = 24;

        [Header("Motion (その場でゆらゆら・回転・拡大縮小・ループ)")]
        [Tooltip("左右の揺れ幅(±px)。")]
        [SerializeField] private float stageClearSparkleSwayX = 14f;
        [Tooltip("上下の揺れ幅(px)。")]
        [SerializeField] private float stageClearSparkleSwayY = 18f;
        [Tooltip("回転の振れ幅(±度)。")]
        [SerializeField] private float stageClearSparkleRotate = 12f;
        [Tooltip("拡大縮小の振れ幅(割合)。0.12 で ±12% ほど。")]
        [SerializeField] private float stageClearSparkleScalePunch = 0.12f;
        [Tooltip("1往復のアニメ時間 最小(秒)。個体ごとにランダム。")]
        [SerializeField] private float stageClearSparkleAnimDurationMin = 1.2f;
        [Tooltip("1往復のアニメ時間 最大(秒)。個体ごとにランダム。")]
        [SerializeField] private float stageClearSparkleAnimDurationMax = 2.4f;

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly List<Tween>      _tweens  = new List<Tween>();

        // パネル表示時に生成・再生。OnEnable は表示のたびに1回だけ呼ばれるため、先にClearして二重生成を防ぐ。
        private void OnEnable()
        {
            ClearDecor();
            SpawnDecor();
        }

        // パネル非表示・破棄時に停止・削除。
        private void OnDisable()
        {
            ClearDecor();
        }

        private void SpawnDecor()
        {
            var parent = decorParent != null ? decorParent : transform as RectTransform;
            if (parent == null) return;

            int count = Mathf.Max(0, stageClearSparkleCount);
            if (count == 0) return;

            // 使用スプライトのプール。override優先、なければ source(7種) を使用。null枠はスキップ。
            Sprite[] src = (sparkleSpritesOverride != null && sparkleSpritesOverride.Length > 0)
                ? sparkleSpritesOverride
                : (crackerSpriteSource != null ? crackerSpriteSource.ClearCrackerSprites : null);
            var pool = new List<Sprite>();
            if (src != null)
                foreach (var s in src)
                    if (s != null) pool.Add(s);
            bool hasSprites = pool.Count > 0;

            float halfW = Mathf.Abs(stageClearSparkleAreaSize.x) * 0.5f;
            float halfH = Mathf.Abs(stageClearSparkleAreaSize.y) * 0.5f;
            float sizeMin = Mathf.Min(stageClearSparkleSizeMin, stageClearSparkleSizeMax);
            float sizeMax = Mathf.Max(stageClearSparkleSizeMin, stageClearSparkleSizeMax);
            float durMin = Mathf.Max(0.1f, Mathf.Min(stageClearSparkleAnimDurationMin, stageClearSparkleAnimDurationMax));
            float durMax = Mathf.Max(0.1f, Mathf.Max(stageClearSparkleAnimDurationMin, stageClearSparkleAnimDurationMax));
            float alpha = Mathf.Clamp01(stageClearSparkleAlpha);

            // 避けるUIの矩形を decorParent ローカル空間で構築（各UIをpadding分だけ外側に広げる）。
            var avoidRects = BuildAvoidRects(parent);
            int attempts = Mathf.Max(1, stageClearSparklePlacementAttempts);

            for (int i = 0; i < count; i++)
            {
                float sz = Random.Range(sizeMin, sizeMax);
                float hs = sz * 0.5f;

                // 主要UIに被らない配置を再抽選で探す。見つからなければこの個体はスキップ（端・余白だけに出す）。
                bool placed = false;
                Vector2 basePos = Vector2.zero;
                for (int a = 0; a < attempts; a++)
                {
                    Vector2 cand = stageClearSparkleAreaOffset + new Vector2(Random.Range(-halfW, halfW), Random.Range(-halfH, halfH));
                    if (!OverlapsAnyAvoid(avoidRects, cand, hs)) { basePos = cand; placed = true; break; }
                }
                if (!placed) continue;

                var go = new GameObject("StageClearSparkle");
                go.transform.SetParent(parent, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(sz, sz);
                rt.anchoredPosition = basePos;
                rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-stageClearSparkleRotate, stageClearSparkleRotate));
                rt.localScale = Vector3.one;
                // decorParent(背景装飾専用Layer)がある場合はそのLayerの描画順に従う（reorderしない）。
                // 未指定で親=overlay panel 直下の場合のみ前面へ出し、背景カード(不透明Panel)に隠れないようにする。
                if (decorParent == null) rt.SetAsLastSibling();

                var img = go.AddComponent<Image>();
                img.raycastTarget = false;
                if (hasSprites)
                {
                    img.sprite         = pool[Random.Range(0, pool.Count)];
                    img.preserveAspect = true;
                    var c = Color.white; c.a = alpha; img.color = c;
                }
                else
                {
                    // 全て未設定時のみ淡色フォールバック（エラーにしない）。
                    var c = Color.HSVToRGB(Random.value, 0.4f, 1f); c.a = alpha; img.color = c;
                }

                // 個体ごとに時間・方向・開始遅延をランダムにして、全部が同じ動きにならないようにする。
                float dSway  = Random.Range(durMin, durMax);
                float dRot   = Random.Range(durMin, durMax);
                float dScale = Random.Range(durMin, durMax);

                Vector2 swayTarget = basePos + new Vector2(
                    Random.Range(-stageClearSparkleSwayX, stageClearSparkleSwayX),
                    Random.Range(stageClearSparkleSwayY * 0.3f, stageClearSparkleSwayY));
                float rotTarget   = rt.localRotation.eulerAngles.z + Random.Range(-stageClearSparkleRotate, stageClearSparkleRotate);
                float scaleTarget = 1f + Mathf.Abs(stageClearSparkleScalePunch);

                var t1 = rt.DOAnchorPos(swayTarget, dSway).SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo).SetDelay(Random.Range(0f, dSway));
                var t2 = rt.DOLocalRotate(new Vector3(0f, 0f, rotTarget), dRot).SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo).SetDelay(Random.Range(0f, dRot));
                var t3 = rt.DOScale(scaleTarget, dScale).SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo).SetDelay(Random.Range(0f, dScale));

                _tweens.Add(t1); _tweens.Add(t2); _tweens.Add(t3);
                _spawned.Add(go);
            }
        }

        // 避けUI群を space(decorParent) ローカル空間の矩形(padding込み)に変換して返す。
        private List<Rect> BuildAvoidRects(RectTransform space)
        {
            var rects = new List<Rect>();
            if (stageClearSparkleAvoidTargets == null || space == null) return rects;
            float pad = Mathf.Max(0f, stageClearSparkleAvoidPadding);
            var corners = new Vector3[4];
            foreach (var t in stageClearSparkleAvoidTargets)
            {
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                t.GetWorldCorners(corners);
                Vector2 mn = new Vector2(float.MaxValue, float.MaxValue);
                Vector2 mx = new Vector2(float.MinValue, float.MinValue);
                for (int k = 0; k < 4; k++)
                {
                    Vector3 lp = space.InverseTransformPoint(corners[k]);
                    if (lp.x < mn.x) mn.x = lp.x;
                    if (lp.y < mn.y) mn.y = lp.y;
                    if (lp.x > mx.x) mx.x = lp.x;
                    if (lp.y > mx.y) mx.y = lp.y;
                }
                rects.Add(Rect.MinMaxRect(mn.x - pad, mn.y - pad, mx.x + pad, mx.y + pad));
            }
            return rects;
        }

        // 候補位置(中心cand・半サイズhs)の矩形が、いずれかの避け矩形と重なるか。
        private static bool OverlapsAnyAvoid(List<Rect> avoidRects, Vector2 cand, float hs)
        {
            if (avoidRects == null || avoidRects.Count == 0) return false;
            var box = Rect.MinMaxRect(cand.x - hs, cand.y - hs, cand.x + hs, cand.y + hs);
            for (int i = 0; i < avoidRects.Count; i++)
                if (avoidRects[i].Overlaps(box)) return true;
            return false;
        }

        private void ClearDecor()
        {
            for (int i = 0; i < _tweens.Count; i++)
            {
                var t = _tweens[i];
                if (t != null && t.IsActive()) t.Kill();
            }
            _tweens.Clear();

            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null) Destroy(_spawned[i]);
            }
            _spawned.Clear();
        }
    }
}
