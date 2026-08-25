using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 顔の左右から、弧を描いて泡をぽんぽん飛ばす。
///
/// 【なぜパーティクルではないか】
///   ParticleSystem は「たくさんの粒をばらまく」のが得意で、
///   「決まった場所から、決まった軌道で、一定間隔に1つずつ」は苦手。
///   顔の左右・弧の形・出る間隔をきっちり決めたいので、1つずつ動かす作りにした。
///
/// 【使い方】
///   泡の絵を入れた Image を1つだけ用意して bubbleTemplate に結線する。
///   置く場所は「顔の中心」。そこから左右へ startOffsetX ぶん離れた位置から泡が出る。
///   残りは実行時に複製するので、Scene に何枚も並べる必要はない。
///
///   ※このスクリプトは、泡の Image とは【別の GameObject】に付けること。
///     同じ GameObject に付けると複製が複製を生んで Unity が落ちる。
///
/// 【軌道】
///   二次ベジェ曲線。開始点 → 制御点（外側の中ほど）→ 終点（上・やや外）。
///   まっすぐ上げるだけだと機械的なので、外へ膨らませて「ふわっ」を出している。
/// </summary>
public class SleepyBubbleEmitter : MonoBehaviour
{
    [Header("結線")]
    [Tooltip("泡1つぶんの Image。これを複製して使う。置く位置は「顔の中心」。Raycast Target は OFF")]
    [SerializeField] private Image bubbleTemplate;

    [Header("出しかた")]
    [Tooltip("泡が出る間隔（秒）")]
    [SerializeField] private float interval = 0.35f;

    [Tooltip("1つの泡が飛んで消えるまでの時間（秒）")]
    [SerializeField] private float lifetime = 1.6f;

    [Tooltip("同時に画面に出せる泡の数。足りないと出るのが飛ぶ")]
    [SerializeField] private int poolSize = 8;

    [Tooltip("左右を交互に出す。OFF にすると毎回ランダム")]
    [SerializeField] private bool alternateSides = true;

    [Header("弧のかたち（Canvas のピクセル単位）")]
    [Tooltip("顔の中心から、泡が出はじめる位置までの左右の距離")]
    [SerializeField] private float startOffsetX = 90f;

    [Tooltip("泡が出はじめる高さ。顔の中心より少し下から出したいならマイナス")]
    [SerializeField] private float startOffsetY = 0f;

    [Tooltip("最後にどれだけ上がるか")]
    [SerializeField] private float arcRise = 260f;

    [Tooltip("最後にどれだけ外へ流れるか")]
    [SerializeField] private float arcOutward = 120f;

    [Tooltip("途中でどれだけ外へ膨らむか。0 でまっすぐ")]
    [SerializeField] private float arcBulge = 90f;

    [Header("ばらつき")]
    [Tooltip("出る位置のばらつき（左右）")]
    [SerializeField] private float jitterX = 18f;

    [Tooltip("出る位置のばらつき（上下）")]
    [SerializeField] private float jitterY = 24f;

    [Tooltip("弧の大きさのばらつき。0.2 なら ±20%")]
    [Range(0f, 0.6f)]
    [SerializeField] private float jitterScale = 0.2f;

    [Header("大きさ・濃さ")]
    [SerializeField] private float startScale = 0.6f;
    [SerializeField] private float endScale = 1.1f;

    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 0.95f;

    [Tooltip("消えはじめるタイミング。0.6 なら残り40%でフェードに入る")]
    [Range(0.1f, 1f)]
    [SerializeField] private float fadeStartRatio = 0.55f;

    private Image[] _pool;
    private bool[] _busy;
    private Vector2 _origin;
    private bool _ready;
    private bool _running;
    private int _sideFlip = 1;
    private Coroutine _loop;

    private void Awake()
    {
        if (bubbleTemplate == null)
        {
            Debug.LogWarning("[SleepyBubble] bubbleTemplate が未結線です。泡は出ません", this);
            return;
        }

        if (bubbleTemplate.gameObject == gameObject)
        {
            Debug.LogWarning("[SleepyBubble] このスクリプトは泡の Image とは別の GameObject に付けてください", this);
        }

        bubbleTemplate.raycastTarget = false;
        _origin = bubbleTemplate.rectTransform.anchoredPosition;

        int n = Mathf.Max(1, poolSize);
        _pool = new Image[n];
        _busy = new bool[n];
        _pool[0] = bubbleTemplate;

        for (int i = 1; i < n; i++)
        {
            var copy = Instantiate(bubbleTemplate, bubbleTemplate.transform.parent);

            // 自分自身が複製されると無限に増えるので、コピーからは取り除く
            foreach (var dup in copy.GetComponents<SleepyBubbleEmitter>()) Destroy(dup);

            copy.name = $"{bubbleTemplate.name}_{i}";
            copy.raycastTarget = false;
            _pool[i] = copy;
        }

        foreach (var img in _pool) img.gameObject.SetActive(false);
        _ready = true;
    }

    /// <summary>泡を出しはじめる。</summary>
    public void Play()
    {
        if (!_ready || _running) return;
        _running = true;
        _loop = StartCoroutine(EmitLoop());
    }

    /// <summary>新しく出すのをやめる。飛んでいる途中の泡はそのまま消えるまで飛ぶ。</summary>
    public void StopEmitting()
    {
        _running = false;
        if (_loop != null) { StopCoroutine(_loop); _loop = null; }
    }

    private IEnumerator EmitLoop()
    {
        while (_running)
        {
            int index = FindFree();
            if (index >= 0) StartCoroutine(FlyOne(index, NextSide()));

            yield return new WaitForSeconds(Mathf.Max(0.05f, interval));
        }
    }

    private int FindFree()
    {
        for (int i = 0; i < _pool.Length; i++)
        {
            if (!_busy[i]) return i;
        }
        return -1;   // 全部飛んでいる。今回は見送る
    }

    /// <summary>次に出す側。+1 が右、-1 が左。</summary>
    private int NextSide()
    {
        if (!alternateSides) return Random.value < 0.5f ? -1 : 1;
        _sideFlip = -_sideFlip;
        return _sideFlip;
    }

    private IEnumerator FlyOne(int index, int side)
    {
        var img = _pool[index];
        if (img == null) yield break;

        _busy[index] = true;

        var rt = img.rectTransform;

        // 弧の3点を決める（二次ベジェ）
        float scale = 1f + Random.Range(-jitterScale, jitterScale);

        Vector2 p0 = _origin + new Vector2(
            side * startOffsetX + Random.Range(-jitterX, jitterX),
            startOffsetY + Random.Range(-jitterY, jitterY));

        Vector2 p2 = p0 + new Vector2(side * arcOutward * scale, arcRise * scale);

        // 制御点は中ほどの高さで、さらに外へ。これが「膨らみ」になる
        Vector2 p1 = p0 + new Vector2(side * (arcOutward + arcBulge) * scale, arcRise * 0.45f * scale);

        var color = img.color;
        rt.anchoredPosition = p0;
        rt.localScale = Vector3.one * startScale;
        color.a = 0f;
        img.color = color;
        img.gameObject.SetActive(true);

        float elapsed = 0f;
        float life = Mathf.Max(0.1f, lifetime);

        while (elapsed < life)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / life);

            // 上がるほどゆっくりに。上に行くにつれ勢いが抜ける感じ
            float eased = 1f - (1f - t) * (1f - t);

            rt.anchoredPosition = QuadraticBezier(p0, p1, p2, eased);
            rt.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, eased) * scale;

            // 出はじめはさっと濃く、後半でゆっくり消える
            float alpha = t < 0.15f
                ? Mathf.Lerp(0f, maxAlpha, t / 0.15f)
                : (t < fadeStartRatio
                    ? maxAlpha
                    : Mathf.Lerp(maxAlpha, 0f, (t - fadeStartRatio) / (1f - fadeStartRatio)));

            color.a = alpha;
            img.color = color;

            yield return null;
        }

        img.gameObject.SetActive(false);
        _busy[index] = false;
    }

    private static Vector2 QuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }
}
