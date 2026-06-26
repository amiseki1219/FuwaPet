using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Random = UnityEngine.Random;

namespace OyatsuPuzzle
{
    // PuzzleClearPanel に付けるクラッカー風スプライト演出。
    // PuzzleClearPanel 表示時に Play(onComplete) を呼ぶと、パネル中央付近から装飾スプライトが
    // 「パーン」と飛び出し、ふわっと落ちてフェードアウトする。演出完了後に onComplete を呼ぶ
    // （= ステージクリア結果画面へ進む）。
    //
    // ・ParticleSystem は使わず UI Image を生成する疑似パーティクル方式。
    // ・PuzzleGamePanel 側の GoalItemSlot キラン演出(SpawnSparkleBurst 等)とは完全に独立。
    // ・飛ばすスプライトは clearCrackerSprites（7枠）に Inspector から割り当てる。null 枠はスキップ。
    public class PuzzleClearCrackerEffectUI : MonoBehaviour
    {
        [Header("Cracker Sprites (6種まで割り当て可能)")]
        [Tooltip("PuzzleClearPanelでクラッカー風に飛ばす装飾Sprite。6種類（星・ハート・音符・紙吹雪など）。未設定枠はスキップ。全て未設定のときのみ淡色で代用。")]
        [SerializeField] private Sprite[] clearCrackerSprites = new Sprite[6];

        [Header("Cracker Motion")]
        [Tooltip("飛び出す個数。")]
        [SerializeField] private int clearCrackerCount = 16;
        [Tooltip("1個あたりの寿命(秒)。飛び出し＋落下の合計時間。")]
        [SerializeField] private float clearCrackerDuration = 0.9f;
        [Tooltip("左右方向の広がり(±px)。")]
        [SerializeField] private float clearCrackerSpreadX = 320f;
        [Tooltip("上方向の広がり(px)。")]
        [SerializeField] private float clearCrackerSpreadY = 420f;
        [Tooltip("発生中心のオフセット（このパネル中心基準。x右+/y上+）。")]
        [SerializeField] private Vector2 clearCrackerStartOffset = Vector2.zero;
        [Tooltip("スプライト1個の最小サイズ(px)。")]
        [SerializeField] private float clearCrackerSizeMin = 40f;
        [Tooltip("スプライト1個の最大サイズ(px)。")]
        [SerializeField] private float clearCrackerSizeMax = 76f;
        [Tooltip("終盤フェードアウト時間(秒)。")]
        [SerializeField] private float clearCrackerFadeDuration = 0.35f;

        [Header("Spawn Parent (省略可)")]
        [Tooltip("クラッカーを出す親。未設定なら自身(PuzzleClearPanel)の RectTransform を使用。")]
        [SerializeField] private RectTransform crackerParent;

        // 割り当て済みのクラッカー用Sprite群（6枠）。他の演出（背景装飾など）から再利用できるよう公開する。
        // 読み取り専用用途のみ（中身の書き換えは想定しない）。
        public Sprite[] ClearCrackerSprites => clearCrackerSprites;

        // クラッカー演出を1回だけ再生し、完了後に onComplete を呼ぶ。
        // 呼び出し側（PuzzleClearPanel 表示時に1回）が二重発火しなければ多重再生しない。
        public void Play(Action onComplete)
        {
            StartCoroutine(PlayRoutine(onComplete));
        }

        private IEnumerator PlayRoutine(Action onComplete)
        {
            float dur = SpawnCracker();
            yield return new WaitForSeconds(dur > 0f ? dur : 0.3f);
            onComplete?.Invoke();
        }

        // クラッカー風の疑似パーティクル（UI Image）を中央から飛び散らせる。所要秒数を返す。
        private float SpawnCracker()
        {
            var parent = crackerParent != null ? crackerParent : transform as RectTransform;
            if (parent == null) return 0f;
            int count = Mathf.Max(0, clearCrackerCount);
            if (count == 0) return 0f;

            // 割り当て済み(null以外)のSpriteだけを抽出。未設定枠はスキップする。
            var pool = new List<Sprite>();
            if (clearCrackerSprites != null)
                foreach (var s in clearCrackerSprites)
                    if (s != null) pool.Add(s);
            bool hasSprites = pool.Count > 0;

            float life = Mathf.Max(0.1f, clearCrackerDuration);
            float fade = Mathf.Max(0.05f, clearCrackerFadeDuration);
            float up   = life * 0.45f;
            float down = life * 0.55f;
            float sizeMin = Mathf.Min(clearCrackerSizeMin, clearCrackerSizeMax);
            float sizeMax = Mathf.Max(clearCrackerSizeMin, clearCrackerSizeMax);

            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("ClearCracker");
                go.transform.SetParent(parent, false);
                var rt = go.AddComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                float sz = Random.Range(sizeMin, sizeMax);
                rt.sizeDelta        = new Vector2(sz, sz);
                rt.anchoredPosition = clearCrackerStartOffset;   // 既定 0 = パネル中央
                rt.localScale       = Vector3.zero;

                var img = go.AddComponent<Image>();
                img.raycastTarget = false;
                if (hasSprites)
                {
                    // 割り当て済み7種(の中の有効分)からランダム選択。
                    img.sprite         = pool[Random.Range(0, pool.Count)];
                    img.preserveAspect = true;
                }
                else
                {
                    // 全て未設定時のみの代用（淡いパステル色）。Inspectorから割り当て可能。
                    img.color = Color.HSVToRGB(Random.value, 0.45f, 1f);
                }

                // 中央 → 左右上方向へパーン → ふわっと落ちる。各個体でばらつかせる。
                float tx = Random.Range(-clearCrackerSpreadX, clearCrackerSpreadX);
                float ty = Random.Range(0.35f, 1f) * clearCrackerSpreadY;
                Vector2 peak    = clearCrackerStartOffset + new Vector2(tx, ty);
                Vector2 fallEnd = peak + new Vector2(tx * 0.15f, -clearCrackerSpreadY * 0.45f);
                float rot = Random.Range(-200f, 200f);

                var seq = DOTween.Sequence();
                seq.Insert(0f,  rt.DOScale(1f, 0.16f).SetEase(Ease.OutBack));
                seq.Insert(0f,  rt.DOAnchorPos(peak, up).SetEase(Ease.OutCubic));     // 飛び出し
                seq.Insert(up,  rt.DOAnchorPos(fallEnd, down).SetEase(Ease.InCubic)); // 落下
                seq.Insert(0f,  rt.DOLocalRotate(new Vector3(0f, 0f, rot), life, RotateMode.FastBeyond360).SetEase(Ease.Linear));
                seq.Insert(life, img.DOFade(0f, fade));                               // 終盤フェード
                seq.SetTarget(rt);
                seq.OnComplete(() => { if (go != null) Destroy(go); });
            }

            return life + fade + 0.05f;
        }
    }
}
