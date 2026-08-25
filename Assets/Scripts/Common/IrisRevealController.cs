using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 画面を覆っている幕の中央に穴を開け、その穴を広げて次の画面を見せる（アイリスイン）。
///
/// 【どこに置くか】
///   「暗い状態から始まって、明るく開いていく」画面に1つ。いまは Care だけ。
///
/// 【使い方】
///   幕（RawImage）は普段オフにしてある。PlayReveal() を呼ぶと
///   「閉じた状態で表示 → 少し待つ → 穴が広がる → 自分でオフになる」まで面倒を見る。
///
/// 【仕組み】
///   Sleep の暗転と同じで、RawImage の UV Rect を動かしているだけ。
///   テクスチャは中央が透明・外周が不透明のもの（NightVeil.png）。
///     UV スケールが大きい … テクスチャが縮んで敷かれる → 穴が小さい＝閉じている
///     UV スケールが小さい … 中央だけ拡大される         → 穴が画面より大きい＝開ききった
///   シェーダーもマテリアルも要らない。
/// </summary>
public class IrisRevealController : MonoBehaviour
{
    [Header("結線")]
    [Tooltip("画面全体を覆う RawImage。Texture に NightVeil.png を入れる。Raycast Target は OFF")]
    [SerializeField] private RawImage veil;

    [Header("見え方")]
    [Tooltip("幕の色。Sleep の暗転と同じネイビーにすると、切り替わりが分からなくなる")]
    [SerializeField] private Color veilColor = new Color(0.106f, 0.129f, 0.251f, 1f); // #1B2140

    [Tooltip("閉じているときの UV スケール。Sleep の Night Uv End と同じ値にすること。\n" +
             "穴の直径 ≒ 画面幅 × 0.32 ÷ この値。40 で 8px 相当になり、ほぼ完全に閉じる")]
    [SerializeField] private float uvClosed = 40f;

    [Tooltip("開ききったときの UV スケール。小さいほど完全に透明になる")]
    [SerializeField] private float uvOpen = 0.10f;

    [Tooltip("開き始めるまでの間。0.2〜0.5 くらい置くと、画面が切り替わった感じが出る")]
    [SerializeField] private float holdBeforeOpen = 0.4f;

    [Tooltip("穴が広がりきるまでの時間")]
    [SerializeField] private float duration = 2.5f;

    [Tooltip("もやを揺らす幅。0 で揺れなし")]
    [SerializeField] private float driftAmplitude = 0.014f;

    [Tooltip("もやの揺れる速さ")]
    [SerializeField] private float driftSpeed = 0.8f;

    private Coroutine _running;

    /// <summary>PlayReveal() を呼んでから穴が開ききるまでの時間。呼び出し側が待つのに使う。</summary>
    public float TotalDuration => holdBeforeOpen + duration;

    private void Awake()
    {
        if (veil == null)
        {
            Debug.LogWarning("[Iris] veil が未結線です。アイリス演出は出ません", this);
            return;
        }

        veil.raycastTarget = false;
        veil.gameObject.SetActive(false);
    }

    /// <summary>
    /// 幕を閉じた状態で出してから開く。
    /// 画面が表示される前に呼ぶこと（Awake か Start の先頭）。遅いと一瞬中身が見えてしまう。
    /// </summary>
    public void PlayReveal()
    {
        if (veil == null) return;

        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(RevealCoroutine());
    }

    private IEnumerator RevealCoroutine()
    {
        veil.color = veilColor;
        veil.raycastTarget = false;
        MakeSquare(veil);
        CloseFully(veil);                 // Sleep の終わりと同じ「完全な真っネイビー」から始める
        veil.gameObject.SetActive(true);

        // 完全に閉じたまま少し待つ
        float elapsed = 0f;
        while (elapsed < holdBeforeOpen)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (duration > 0f)
        {
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                ApplyVeil(veil, LerpUv(uvClosed, uvOpen, t), Time.time, driftAmplitude, driftSpeed);
                yield return null;
            }
        }

        ApplyVeil(veil, uvOpen, Time.time, driftAmplitude, driftSpeed);
        veil.gameObject.SetActive(false);
        _running = null;
    }

    // ── Sleep 側と共通で使う処理 ──────────────────────────────────────────────
    //   同じものを2箇所に書かないよう、こちらに置いて SleepSceneManager から呼んでいる。

    /// <summary>
    /// もやの矩形を「画面より大きい正方形」にして中央に置く。
    ///
    /// なぜ必要か:
    ///   Stretch 全面（1080×1920）のままだと、正方形のテクスチャが縦に引き伸ばされ、
    ///   丸いはずの穴が縦長の楕円につぶれる。2026/8/25 の実機確認で判明した。
    ///   長辺に合わせた正方形にすれば真円になり、短辺方向にはみ出したぶんは
    ///   画面外に出るだけなので問題ない。
    /// </summary>
    public static void MakeSquare(RawImage img)
    {
        if (img == null) return;

        var rt = img.rectTransform;

        // 基準は「親」ではなく「所属する Canvas」。
        // 親がパネルなどだと矩形が画面より小さく、幕が画面を覆いきれない。
        // 2026/8/25 の実機確認で、Care 側の幕が上下だけ足りない不具合として出た。
        var canvas = img.canvas;
        var baseRt = canvas != null ? canvas.transform as RectTransform : rt.parent as RectTransform;
        if (baseRt == null) return;

        // 1.2 は余裕。回転や解像度のブレで隅が透けないようにするため
        float side = Mathf.Max(baseRt.rect.width, baseRt.rect.height) * 1.2f;
        if (side <= 0f) return;   // レイアウト未確定。念のため

        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(side, side);
    }

    /// <summary>
    /// 幕を完全に閉じる（画面全体を単色で塗る）。
    ///
    /// なぜ UV スケールを上げるだけでは足りないか:
    ///   テクスチャの中央には必ず透明な芯があるので、いくら縮めても小さな点が残る。
    ///   UV 40 でも画面幅の 0.8%（1080px なら 8px）が抜けたままになる。
    ///
    ///   そこで、閉じ切るときだけテクスチャの「角」を映す。
    ///   角は完全に不透明なので、そこだけを引き伸ばせば画面全体が単色になる。
    /// </summary>
    public static void CloseFully(RawImage img)
    {
        if (img == null) return;
        img.uvRect = new Rect(0f, 0f, 0.01f, 0.01f);
    }

    /// <summary>
    /// UV スケールを from → to へ補間する。
    ///
    /// なぜ単純な Lerp ではないか:
    ///   UV スケールは「穴の見かけの大きさ」に逆比例する（scale 8 なら穴は 1/8 の大きさ）。
    ///   8 → 0.1 をそのまま線形に動かすと、前半はほとんど穴が変わらず、
    ///   最後にドンと開いて見える。見かけの大きさ（1/scale）のほうを補間すると、
    ///   目で見て一定の速さで開いていく。
    /// </summary>
    public static float LerpUv(float from, float to, float t)
    {
        from = Mathf.Max(0.01f, from);
        to   = Mathf.Max(0.01f, to);
        return 1f / Mathf.Lerp(1f / from, 1f / to, t);
    }

    /// <summary>
    /// UV Rect を中心そろえで設定する。
    /// x, y をずらすことで、もやがゆっくり漂って見える（同じ形のまま止まらない）。
    /// </summary>
    public static void ApplyVeil(RawImage img, float scale, float time, float amplitude, float speed)
    {
        if (img == null) return;

        scale = Mathf.Max(0.01f, scale);

        // 揺れ幅は scale に比例させる。拡大時に揺れが大きく見えすぎるのを防ぐため
        float dx = Mathf.Sin(time * speed)         * amplitude * scale;
        float dy = Mathf.Cos(time * speed * 0.83f) * amplitude * scale;

        float origin = (1f - scale) * 0.5f;
        img.uvRect = new Rect(origin + dx, origin + dy, scale, scale);
    }
}
