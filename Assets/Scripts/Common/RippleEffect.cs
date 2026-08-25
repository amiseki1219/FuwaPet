using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 押した場所から波紋を何回か広げる。
///
/// 【使い方】
///   波紋の絵を入れた Image を1つだけ用意して rippleTemplate に結線する。
///   残りは実行時に複製するので、Scene に何枚も並べる必要はない。
///
///   ※このスクリプトは、波紋の Image とは【別の GameObject】に付けること。
///     同じ GameObject に付けると複製が複製を生んで Unity が落ちる。
///   Play() を呼ぶと、小さく始まって大きくなりながら薄くなる、を count 回くり返す。
///
/// 【なぜパーティクルではないか】
///   リモコンは Canvas の中の UI。パーティクルを UI の上に出すには
///   Sorting の調整が要るうえ、Canvas のスケールと合わせるのが面倒になる。
///   UI 同士なら重なり順もサイズも素直に決まる。
/// </summary>
public class RippleEffect : MonoBehaviour
{
    [Header("結線")]
    [Tooltip("波紋1枚ぶんの Image。これを複製して使う。Raycast Target は OFF にしておく")]
    [SerializeField] private Image rippleTemplate;

    [Header("出しかた")]
    [Tooltip("何回くり返すか")]
    [SerializeField] private int count = 3;

    [Tooltip("次の波紋が出るまでの間")]
    [SerializeField] private float interval = 0.18f;

    [Tooltip("1枚が広がりきって消えるまでの時間")]
    [SerializeField] private float duration = 0.6f;

    [Header("大きさ・濃さ")]
    [SerializeField] private float startScale = 0.3f;
    [SerializeField] private float endScale = 2.2f;

    [Tooltip("出た瞬間の濃さ。1 で元の絵のまま")]
    [Range(0f, 1f)]
    [SerializeField] private float startAlpha = 0.9f;

    private Image[] _pool;
    private bool _ready;

    /// <summary>Play() から全部消え終わるまでにかかる時間。呼び出し側が待つのに使う。</summary>
    public float TotalDuration => Mathf.Max(0, count - 1) * interval + duration;

    private void Awake()
    {
        if (rippleTemplate == null)
        {
            Debug.LogWarning("[Ripple] rippleTemplate が未結線です。波紋は出ません", this);
            return;
        }

        rippleTemplate.raycastTarget = false;

        // 同じ GameObject に付いていると複製で増えていくので、気づけるようにしておく。
        // 下の Destroy で実害は止まるが、置き場所としては正しくない
        if (rippleTemplate.gameObject == gameObject)
        {
            Debug.LogWarning("[Ripple] RippleEffect は波紋の Image とは別の GameObject に付けてください", this);
        }

        // 波紋は重なって出るので、同時に存在するぶんだけ実体が要る。
        // テンプレート自身を1枚目として使い、足りないぶんを複製する。
        int n = Mathf.Max(1, count);
        _pool = new Image[n];
        _pool[0] = rippleTemplate;

        for (int i = 1; i < n; i++)
        {
            var copy = Instantiate(rippleTemplate, rippleTemplate.transform.parent);

            // ★ここを外すと Unity が落ちる★
            //   このスクリプトを波紋の Image と同じ GameObject に付けていると、
            //   複製された側にも RippleEffect が付いてくる。その Awake がまた複製し、
            //   複製がまた複製し……と指数的に増えてメモリを食い尽くす。
            //   2026/8/25 に実際に Unity がクラッシュした。
            foreach (var dup in copy.GetComponents<RippleEffect>()) Destroy(dup);

            copy.name = $"{rippleTemplate.name}_{i}";
            copy.raycastTarget = false;
            copy.rectTransform.anchoredPosition = rippleTemplate.rectTransform.anchoredPosition;
            _pool[i] = copy;
        }

        foreach (var img in _pool) img.gameObject.SetActive(false);
        _ready = true;
    }

    /// <summary>波紋を出し始める。押した瞬間に呼ぶ。</summary>
    public void Play()
    {
        if (!_ready) return;
        StartCoroutine(PlayCoroutine());
    }

    private IEnumerator PlayCoroutine()
    {
        for (int i = 0; i < _pool.Length; i++)
        {
            StartCoroutine(OneRipple(_pool[i]));
            if (interval > 0f) yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator OneRipple(Image img)
    {
        if (img == null) yield break;

        var rt = img.rectTransform;
        img.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // 広がりは最初が速く、だんだんゆっくり。水面の波紋の感じに近づく
            float eased = 1f - (1f - t) * (1f - t);

            rt.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, eased);

            var c = img.color;
            c.a = Mathf.Lerp(startAlpha, 0f, t);
            img.color = c;

            yield return null;
        }

        img.gameObject.SetActive(false);
    }
}
