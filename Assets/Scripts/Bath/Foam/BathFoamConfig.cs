using UnityEngine;

namespace Yurufu.Bath.Foam
{
    /// <summary>
    /// 泡の見た目パラメータ。BathFoamController が SerializeField で1つ持つ。
    /// 値は試作（FoamPrototype）で調整した結果をそのまま引き継いでいる。
    /// </summary>
    [System.Serializable]
    public class BathFoamConfig
    {
        [Header("マスク")]
        [Tooltip("マスクの横。Head/Body とも 512 を想定")]
        public int maskWidth = 512;

        [Tooltip("マスクの縦。左右ミラーを上下に分けるため横の2倍にする")]
        public int maskHeight = 1024;

        [Header("ブラシ")]
        [Tooltip("ブラシの半径（元メッシュのUV単位）。UVは0〜1なので 0.03 で直径6%ぶん")]
        [Range(0.002f, 0.25f)] public float brushRadius = 0.035f;

        [Tooltip("ブラシの縁のぼかし。0=くっきり 1=ふんわり")]
        [Range(0f, 1f)] public float brushSoftness = 0.55f;

        [Tooltip("1回の塗りでマスクがどれだけ濃くなるか。重ね塗りで濃くなる")]
        [Range(0.02f, 1f)] public float paintStrength = 0.5f;

        [Header("シェル")]
        [Tooltip("土台（泡シェル）を表示するか。★既定は OFF＝泡3.png の粒だけで見せる。\n" +
                 "ON にすると、キャラ表面に密着した土台が出る（ベタ塗りに見えることがある）")]
        public bool shellVisible = false;

        [Tooltip("土台の膨らみ。マスクが無くても常にこのぶん膨らむ")]
        [Range(0f, 0.5f)] public float shellOffset = 0.02f;

        [Tooltip("塗った所だけ追加で厚くなる量。★泡らしさに一番効く")]
        [Range(0f, 0.5f)] public float maskDisplace = 0.06f;

        [Tooltip("泡の粒の細かさ。大きいほど細かい粒")]
        public float bubbleScale = 40f;

        [Tooltip("粒の凹凸の強さ。0 だとつるっとした膜、上げるとぼこぼこになる")]
        [Range(0f, 1f)] public float bubbleDepth = 0.45f;

        [Tooltip("泡の色。白そのものだと強すぎるので、わずかにピンク寄りにしてある")]
        public Color foamColor = new Color(1f, 0.955f, 0.965f, 1f);

        [Tooltip("輪郭を崩すノイズの細かさ。大きいほど細かい")]
        public float noiseScale = 24f;

        [Tooltip("ノイズの強さ。強すぎると泡が虫食いになる")]
        [Range(0f, 1f)] public float noiseStrength = 0.35f;

        [Tooltip("これ未満のマスク値は表示しない")]
        [Range(0.01f, 0.99f)] public float clipThreshold = 0.25f;

        [Tooltip("縁の明るさ。泡の立体感を少しだけ足す")]
        [Range(0f, 2f)] public float rimStrength = 0.5f;

        // ── 泡粒（泡3.png）Phase 2A ────────────────────────────────────────
        // 泡シェルが「土台」、こちらが「モコモコした粒と輪郭」。役割が違う。
        // 泡3.png だけでキャラ全体を覆う旧方式には戻さない。あくまで装飾。

        [Header("泡粒（泡3.png）")]
        [Tooltip("Bubble Density: UV上でこの距離だけ進むごとに粒を1個置く。小さいほど密")]
        [Range(0.002f, 0.15f)] public float grainDensity = 0.02f;

        [Tooltip("粒の上限個数。★上限に達したら、それ以上は増やさない（古い粒は消さない）。\n" +
                 "200 では足りなかったため 400 に倍増（2026/8/27）。スライダーの上限も 800 まで伸ばしてある")]
        [Range(1, 800)] public int grainMaxCount = 400;

        [Tooltip("Min Size 相当。小さい粒の大きさ（ワールド単位）")]
        [Range(0.01f, 1.5f)] public float grainSizeS = 0.16f;

        [Tooltip("中くらいの粒。既存 BubbleSprite.prefab の実表示 0.263 unit に合わせてある")]
        [Range(0.01f, 1.5f)] public float grainSizeM = 0.26f;

        [Tooltip("Max Size 相当。大きい粒の大きさ")]
        [Range(0.01f, 1.5f)] public float grainSizeL = 0.40f;

        [Tooltip("小の出やすさ")] [Range(0f, 10f)] public float grainWeightS = 5f;
        [Tooltip("中の出やすさ")] [Range(0f, 10f)] public float grainWeightM = 3f;
        [Tooltip("大の出やすさ")] [Range(0f, 10f)] public float grainWeightL = 2f;

        [Tooltip("各段階の中での大きさのゆらぎ（±割合）。同じ絵の繰り返し感を減らす")]
        [Range(0f, 0.6f)] public float grainSizeJitter = 0.12f;

        [Tooltip("Surface Lift: 表面の法線方向へ持ち上げる量。泡シェルの少し手前に出すために使う")]
        [Range(0f, 0.6f)] public float grainLift = 0.10f;

        [Tooltip("Alpha: 粒全体の濃さ。泡3.png の最大アルファは 220 なので 1.0 でも完全不透明にはならない")]
        [Range(0f, 2f)] public float grainAlpha = 1f;

        [Tooltip("粒ごとの Alpha のゆらぎ（±割合）")]
        [Range(0f, 0.8f)] public float grainAlphaJitter = 0.15f;

        [Tooltip("Color Tint: 控えめな色付けだけに使う。既定は白＝素材そのままの色")]
        public Color grainTint = Color.white;

        [Tooltip("Rotation Range: 置いた瞬間の回転の振れ幅（度）。回り続けはしない")]
        [Range(0f, 180f)] public float grainRotationRange = 15f;

        [Tooltip("Random Flip: 粒ごとに左右反転する。泡3.png は左右非対称なので効果がある")]
        public bool grainRandomFlip = true;

        /// <summary>切り分けテスト用に値だけ差し替えたコピーを作る。</summary>
        public BathFoamConfig Clone() => (BathFoamConfig)MemberwiseClone();
    }
}
