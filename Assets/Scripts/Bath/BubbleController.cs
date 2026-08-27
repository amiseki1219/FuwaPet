using System.Collections;
using UnityEngine;

/// <summary>
/// キャラの体に付く泡1個ぶんの動き。
///
/// 2026/8/25 の変更:
///   もともとは 3D の球（BubbleSphere.prefab）に付ける想定で、Show(size) に
///   「出したい大きさ」を毎回渡す作りだった。
///   しかし 2D スプライトの泡に切り替えたことで、泡ごとに大きさを変えて
///   Scene に並べたくなった。Show(size) のままだと全部が同じ大きさに揃ってしまう。
///   → Awake で「Scene に置かれたときの大きさ」を覚えておき、
///     引数なしの Show() でそこへ戻すようにした。
///   既存の Show(float) は他から呼ばれていても壊れないよう、そのまま残してある。
/// </summary>
public class BubbleController : MonoBehaviour
{
    // 拡大・縮小の速さ。大きいほど速く出る。
    // 8 だと出きるまで約0.3秒かかり、こすった手ごたえが鈍く感じたので 25 に上げた。
    // Inspector で泡ごとに調整できるようにしてある。
    [Tooltip("泡が出る速さ。大きいほどキビキビ出る（8 でゆっくり、25 で即座）")]
    [SerializeField] private float lerpSpeed = 25f;

    private Vector3 _targetScale;
    private bool _isShowing;
    private bool _isHiding;

    /// <summary>Scene 上で設定されていた本来の大きさ。引数なし Show() の行き先。</summary>
    private Vector3 _initialScale = Vector3.one;

    /// <summary>色を変えるための絵。3D球のときは見つからないので null のままになる。</summary>
    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        // 置かれたときの大きさを覚えてから、見えない状態（0倍）にしておく。
        // ここで 0 にしておかないと、シーンを開いた瞬間に泡が全部見えてしまう。
        _initialScale = transform.localScale;

        // 保険: Scale が 0 のまま保存された Prefab を読み込むと、
        // 「覚えた大きさ」も 0 になり、Show() しても永久に見えなくなる。
        // （Play 中に Prefab を作る／Apply すると、Awake で 0 にした状態が焼き付いてしまう）
        // 0 を読んだときは 1 倍として扱い、最低限見える状態にする。
        if (_initialScale.sqrMagnitude < 1e-12f)
        {
            Debug.LogWarning($"[Bubble] {name} の Scale が 0 でした。1 として扱います（Prefab の Scale を確認してください）");
            _initialScale = Vector3.one;
        }

        transform.localScale = Vector3.zero;

        // 子に付いている場合もあるので GetComponentInChildren で探す。
        // 第1引数 true は「非アクティブな子も対象にする」の意味。
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }

    private void Update()
    {
        if (_isShowing)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * lerpSpeed);
            if (Vector3.Distance(transform.localScale, _targetScale) < 0.001f)
            {
                transform.localScale = _targetScale;
                _isShowing = false;
            }
        }
        else if (_isHiding)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, Time.deltaTime * lerpSpeed);
            if (transform.localScale.magnitude < 0.001f)
            {
                transform.localScale = Vector3.zero;
                _isHiding = false;
            }
        }
    }

    /// <summary>Scene に置かれたときの大きさへ、ぷにっと戻す。</summary>
    public void Show()
    {
        ShowTo(_initialScale);
    }

    /// <summary>大きさを指定して出す（旧方式・互換のため残している）。</summary>
    public void Show(float size)
    {
        ShowTo(new Vector3(size, size, size));
    }

    private void ShowTo(Vector3 scale)
    {
        StopAllCoroutines();
        _targetScale = scale;
        _isShowing = true;
        _isHiding = false;
    }

    public void Hide()
    {
        StopAllCoroutines();
        _isShowing = false;
        _isHiding = true;
    }

    /// <summary>
    /// 泡の色を変える。シャンプーごとの色を BathWashManager から配ってもらう。
    /// SpriteRenderer が無い（3D球の）ときは何もしない。
    /// </summary>
    public void SetColor(Color color)
    {
        if (_spriteRenderer == null) return;

        // 絵そのものが半透明なので、渡された色のアルファは無視して
        // スプライト側の透け具合をそのまま活かす。
        color.a = 1f;
        _spriteRenderer.color = color;
    }

    /// <summary>もとの大きさ。</summary>
    public Vector3 InitialScale => _initialScale;

    /// <summary>
    /// 「本来の大きさ」を外から差し替える。
    ///
    /// なぜ必要か:
    ///   実行時に複製して泡を置く場合、Awake で覚えるのは複製元の大きさになる。
    ///   泡ごとに大きさをばらつかせたいので、複製した直後にここで上書きする。
    /// </summary>
    public void SetInitialScale(Vector3 scale)
    {
        // 0 を渡されると見えなくなるので保険をかける。
        //
        // しきい値が大きすぎると誤爆する:
        //   ボーンにぶら下げた泡は localScale が 0.002 のように小さくなることがある
        //   （親のスケールが 100 倍なら 0.2 ÷ 100 = 0.002）。
        //   0.0001 で判定していたときは、これを「0」とみなして 1 倍に直してしまい、
        //   見た目が 100 倍の巨大な泡になって画面が真っ白になった。
        //   → 本当に 0 のときだけ働くよう、しきい値をぐっと下げた。
        _initialScale = scale.sqrMagnitude < 1e-12f ? Vector3.one : scale;
    }

    /// <summary>
    /// ふわーっと消す。少し上へ浮きながら、ふくらんで、透明になる。
    ///
    /// Hide()（その場で縮む）との違い:
    ///   流すときは「水で流されて浮いていく」感じを出したいので、
    ///   縮めずに 上昇 ＋ 拡大 ＋ フェードアウト を同時にかけている。
    /// </summary>
    /// <param name="duration">消えるまでの秒数</param>
    /// <param name="rise">どれだけ上へ浮くか（ワールド単位）</param>
    public void FadeAway(float duration, float rise)
    {
        StopAllCoroutines();
        _isShowing = false;
        _isHiding = false;
        StartCoroutine(FadeAwayCoroutine(duration, rise));
    }

    private IEnumerator FadeAwayCoroutine(float duration, float rise)
    {
        Vector3 startPos   = transform.position;
        Vector3 startScale = transform.localScale;
        Vector3 endScale   = startScale * 1.25f;

        Color baseColor = _spriteRenderer != null ? _spriteRenderer.color : Color.white;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // 上へ浮く。最初はゆっくり、後半で速くしたいので t を2乗している
            transform.position   = startPos + Vector3.up * (rise * t * t);
            transform.localScale = Vector3.Lerp(startScale, endScale, t);

            if (_spriteRenderer != null)
            {
                Color c = baseColor;
                c.a = 1f - t;
                _spriteRenderer.color = c;
            }

            yield return null;
        }

        // 完全に消す。位置と色は元に戻しておく（作り直さず使い回す場合に備えて）
        transform.localScale = Vector3.zero;
        transform.position   = startPos;
        if (_spriteRenderer != null) _spriteRenderer.color = baseColor;
    }

    public void PopEffect()
    {
        StopAllCoroutines();
        _isShowing = false;
        _isHiding = false;
        StartCoroutine(PopCoroutine());
    }

    private IEnumerator PopCoroutine()
    {
        Vector3 start = transform.localScale;
        Vector3 peak  = start * 1.4f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 10f;
            transform.localScale = Vector3.Lerp(start, peak, t);
            yield return null;
        }
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 14f;
            transform.localScale = Vector3.Lerp(peak, Vector3.zero, t);
            yield return null;
        }
        transform.localScale = Vector3.zero;
    }
}
