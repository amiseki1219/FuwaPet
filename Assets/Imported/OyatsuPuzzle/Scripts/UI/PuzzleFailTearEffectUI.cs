using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Random = UnityEngine.Random;

namespace OyatsuPuzzle
{
    // PuzzleFailPanel に付ける「ぽろり」涙演出。クリアのクラッカー（PuzzleClearCrackerEffectUI）の失敗版。
    // PuzzleFailPanel 表示時に Play(onComplete) を呼ぶと、上の方から涙スプライトがぽろぽろ落ちて
    // フェードアウトし、演出完了後に onComplete を呼ぶ（= 失敗結果画面 PuzzleStageFailOverlayPanel へ進む）。
    //
    // ・ParticleSystem は使わず UI Image を生成する疑似パーティクル方式（クラッカーと同じ作り）。
    // ・クリア演出（クラッカー）とは完全に独立。共通化はしない。
    // ・涙スプライトは tearSprites に Inspector から割り当てる。未設定枠はスキップ。全て未設定なら淡色で代用。
    public class PuzzleFailTearEffectUI : MonoBehaviour
    {
        [Header("Tear Sprites (6種まで割り当て可能)")]
        [Tooltip("ぽろり落ちる涙/しずく Sprite。未設定枠はスキップ。全て未設定のときのみ淡色で代用。")]
        [SerializeField] private Sprite[] tearSprites = new Sprite[6];

        [Header("Tear Motion")]
        [Tooltip("落とす個数。")]
        [SerializeField] private int tearCount = 10;
        [Tooltip("1個あたりの寿命(秒)。落下の合計時間。")]
        [SerializeField] private float tearDuration = 1.0f;
        [Tooltip("発生位置の左右の広がり(±px)。")]
        [SerializeField] private float tearSpreadX = 240f;
        [Tooltip("落下距離(px)。")]
        [SerializeField] private float tearFallDistance = 360f;
        [Tooltip("発生中心のオフセット（このパネル中心基準。x右+/y上+）。既定は少し上。")]
        [SerializeField] private Vector2 tearStartOffset = new Vector2(0f, 160f);
        [Tooltip("スプライト1個の最小サイズ(px)。")]
        [SerializeField] private float tearSizeMin = 28f;
        [Tooltip("スプライト1個の最大サイズ(px)。")]
        [SerializeField] private float tearSizeMax = 52f;
        [Tooltip("1個ごとの落下開始のばらつき(秒)。")]
        [SerializeField] private float tearStagger = 0.06f;
        [Tooltip("終盤フェードアウト時間(秒)。")]
        [SerializeField] private float tearFadeDuration = 0.3f;

        [Header("Spawn Parent (省略可)")]
        [Tooltip("涙を出す親。未設定なら自身(PuzzleFailPanel)の RectTransform を使用。")]
        [SerializeField] private RectTransform tearParent;

        // 涙演出を1回だけ再生し、完了後に onComplete を呼ぶ。
        public void Play(Action onComplete)
        {
            StartCoroutine(PlayRoutine(onComplete));
        }

        private IEnumerator PlayRoutine(Action onComplete)
        {
            float dur = SpawnTears();
            yield return new WaitForSeconds(dur > 0f ? dur : 0.4f);
            onComplete?.Invoke();
        }

        // 涙の疑似パーティクル（UI Image）を上から落とす。所要秒数を返す。
        private float SpawnTears()
        {
            var parent = tearParent != null ? tearParent : transform as RectTransform;
            if (parent == null) return 0f;
            int count = Mathf.Max(0, tearCount);
            if (count == 0) return 0f;

            var pool = new List<Sprite>();
            if (tearSprites != null)
                foreach (var s in tearSprites)
                    if (s != null) pool.Add(s);
            bool hasSprites = pool.Count > 0;

            float life = Mathf.Max(0.1f, tearDuration);
            float fade = Mathf.Max(0.05f, tearFadeDuration);
            float sizeMin = Mathf.Min(tearSizeMin, tearSizeMax);
            float sizeMax = Mathf.Max(tearSizeMin, tearSizeMax);
            float maxStagger = Mathf.Max(0f, tearStagger) * count;

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("FailTear");
                go.transform.SetParent(parent, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                float sz = Random.Range(sizeMin, sizeMax);
                rt.sizeDelta = new Vector2(sz, sz);

                float sx = Random.Range(-tearSpreadX, tearSpreadX);
                Vector2 start = tearStartOffset + new Vector2(sx, Random.Range(-20f, 40f));
                rt.anchoredPosition = start;
                rt.localScale = Vector3.zero;

                var img = go.AddComponent<Image>();
                img.raycastTarget = false;
                if (hasSprites)
                {
                    img.sprite         = pool[Random.Range(0, pool.Count)];
                    img.preserveAspect = true;
                }
                else
                {
                    // 全て未設定時のみの代用（淡い水色）。Inspectorから割り当て可能。
                    img.color = new Color(0.6f, 0.8f, 1f, 0.9f);
                }

                float delay   = Random.Range(0f, maxStagger);
                float drift    = sx * 0.1f;
                Vector2 fallEnd = start + new Vector2(drift, -tearFallDistance);

                var seq = DOTween.Sequence();
                seq.AppendInterval(delay);
                seq.Append(rt.DOScale(1f, 0.14f).SetEase(Ease.OutBack));   // ぷくっと出る
                seq.Join(rt.DOAnchorPos(fallEnd, life).SetEase(Ease.InCubic)); // ぽろりと落下
                seq.Insert(delay + life * 0.7f, img.DOFade(0f, fade));     // 終盤フェード
                seq.SetTarget(rt);
                seq.OnComplete(() => { if (go != null) Destroy(go); });
            }

            return maxStagger + life + fade + 0.05f;
        }
    }
}
