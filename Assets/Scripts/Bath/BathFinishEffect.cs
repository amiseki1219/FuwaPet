using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;   // FormerlySerializedAs（流れ星 → 降る星への改名で結線を引き継ぐ）

/// <summary>
/// お風呂の「完了演出」（A5.5）。
///
/// 【いつ動くか】
///   泡を流し終わり → 雲が退散し終わった直後に BathWashManager から Play() が呼ばれる。
///   演出が終わると onFinished を呼び返し、そこでリザルトが出る。
///
///   泡が消える → 雲が退散 →【ここ】→ リザルト →（キラキラは漂ったまま）→ Care へ
///
/// 【位置の決め方＝画面座標で決める】★2026/8/29 に方式を変更
///   最初は ParticleSystem の Shape（球の半径）と Velocity over Lifetime に任せていたが、
///   ・キラキラがキャラの一部にしか出ない
///   ・流れ星がキャラの手前に出る／ほとんど動かない
///   という結果になった。Scene 側の設定に結果が左右され、こちらから予測できなかった。
///
///   そこで BathDropletRain（雫）と同じ方式にそろえた。
///     カメラからの距離を決める → 画面座標（0〜1）で位置を決める
///     → cam.ScreenToWorldPoint でワールド座標に直す → EmitParams で1粒ずつ置く
///   これなら「画面の右上から左下へ」「キャラより奥」を数値で確実に指定できる。
///   Sleep の SleepyBubbleEmitter が「顔の中心から左右へ○○px」と画面の単位で
///   位置を決めているのと同じ考え方。
///
///   ★この方式にしたので、ParticleSystem の Shape / Velocity over Lifetime は使わない。
///     Inspector で OFF にしておくこと（ONだと二重に効いてズレる）。
///
/// 【Canvas の外に置く理由】★重要
///   Bath.unity の Canvas は2枚とも Screen Space - Overlay で、
///   Overlay の Canvas は必ず3Dキャラより手前に描かれる。
///   「キャラの後ろに虹」「キャラの後ろに流れ星」は Canvas の子では実現できない。
///   → このコンポーネントが持つオブジェクトは、すべてシーン直下（Canvas の外）に置く。
///      虹だけは World Space の Canvas に入れる（Image の fillAmount を使いたいため）。
///
/// 【iOS ビルドに入る本番コード】
///   UnityEditor / AssetDatabase / Shader.Find / HideFlags は使わない。
///   毎フレームの Debug.Log も出さない。
/// </summary>
public class BathFinishEffect : MonoBehaviour
{
    /// <summary>シャンプー1種類ぶんの完了演出の設定。Inspector から差し替える。</summary>
    [System.Serializable]
    public class ShampooFinishSet
    {
        [Tooltip("BathSceneManager の AllShampoo と同じ ID。normal / ohisama / hoshizora / rainbow")]
        public string shampooId;

        [Tooltip("キラキラの絵。Sparkle Particles と【同じ順番】で対応する。\n" +
                 "要素が空、または対応する ParticleSystem が無い番号では、そのキラキラは出ない")]
        public Sprite[] sparkleSprites;

        [Tooltip("キラキラの色の【候補】。1粒ごとにこの中から1色がランダムで選ばれる。\n" +
                 "★2026/8/29 変更：以前は「Sparkle1 の色 / Sparkle2 の色」という対応表だったが、\n" +
                 "  「白い絵を何色かでバラバラに出したい」という使い方に合わせて候補リストに変えた。\n" +
                 "  降る星（Falling Star Tints）と同じ考え方。\n" +
                 "★空にするとスプライトの色そのまま（白）で出る。\n" +
                 "★出やすさを変えたいときは、同じ色を複数回入れる。\n" +
                 "★【注意】Unity は配列の Size を増やすと (0,0,0,0)＝透明 を入れる。\n" +
                 "  アルファ(A)を 255 にしないと1粒も見えない")]
        public Color[] sparkleTints;

        [Tooltip("キャラの後ろに虹をかけるか（おひさまシャンプー）")]
        public bool useRainbow;

        [Tooltip("キャラの後ろに星を降らせるか（ほしぞらシャンプー）")]
        [FormerlySerializedAs("useShootingStar")]
        public bool useFallingStars;
    }

    // ── キャラの位置 ──────────────────────────────────────────────────────────

    [Header("キャラの位置")]
    [Tooltip("キャラが生成される親。Bath.unity の CharacterDisplayAnchor を結線する。\n" +
             "★キラキラを「キャラの周り」に出すために、ここから体の大きさを測る。\n" +
             "  未結線のときは画面の中央あたりに出す（そのとき警告を1行出す）")]
    [SerializeField] private Transform characterAnchor;

    // ── 虹 ────────────────────────────────────────────────────────────────────

    [Header("虹（おひさまシャンプー）")]
    [Tooltip("World Space Canvas の中に置いた 虹.png の Image を結線する。\n" +
             "★Image Type などはこのスクリプトが実行時に設定するので、Inspector での設定は不要")]
    [SerializeField] private Image rainbowImage;

    [Tooltip("虹が左下から右上へ、かかりきるまでの秒数。\n" +
             "★等速で伸ばす。SmoothStep のような加減速は使わない\n" +
             "  （泡を流すときに、中盤だけ速く見えて失敗した経験があるため）")]
    [Range(0.2f, 5f)]
    [SerializeField] private float rainbowDrawDuration = 1.2f;

    // ── キラキラ ──────────────────────────────────────────────────────────────

    [Header("キラキラ（全シャンプー共通の枠）")]
    [Tooltip("キラキラを出す ParticleSystem。シーン直下に置いたものを結線する。\n" +
             "★1つの ParticleSystem には1枚の絵しか入れられない（同一テクスチャ制約）ため、\n" +
             "  種類を増やすぶんだけ ParticleSystem を並べる。\n" +
             "  各シャンプーの Sparkle Sprites と【同じ順番】で対応する")]
    [SerializeField] private ParticleSystem[] sparkleParticles;

    [Tooltip("演出の開始で「ぱっ」と出す粒の数（1種類あたり）")]
    [Range(0, 100)]
    [SerializeField] private int sparkleBurstCount = 18;

    [Tooltip("そのあと出し続ける量（1種類あたり・毎秒）。\n" +
             "★Care 画面へ移るまで出し続ける。止める処理は入れていない")]
    [Range(0f, 40f)]
    [SerializeField] private float sparkleRatePerSecond = 8f;

    [Tooltip("キラキラを出す範囲。1.0 でキャラのちょうど大きさ、1.2 で2割ぶん外まで広がる")]
    [Range(0.5f, 2.5f)]
    [SerializeField] private float sparkleAreaScale = 1.2f;

    [Tooltip("カメラからキラキラまでの距離。キャラは約12.6 なので、\n" +
             "これより小さくするとキャラの手前に出る")]
    [Range(1f, 30f)]
    [SerializeField] private float sparkleDistance = 11.5f;

    [Tooltip("キラキラ1粒の大きさ（ワールド単位）")]
    [Range(0.05f, 3f)]
    [SerializeField] private float sparkleSize = 0.5f;

    [Tooltip("キラキラ1粒が消えるまでの秒数")]
    [Range(0.2f, 6f)]
    [SerializeField] private float sparkleLifetime = 1.8f;

    [Tooltip("キラキラがふわっと上へ昇る速さ。0 でその場に留まる")]
    [Range(0f, 3f)]
    [SerializeField] private float sparkleRiseSpeed = 0.35f;

    // ── 流れ星 ────────────────────────────────────────────────────────────────

    [Header("降る星（ほしぞらシャンプー）")]
    [Tooltip("星を出す ParticleSystem。シーン直下に置いたものを結線する。\n" +
             "★絵は Texture Sheet Animation に手で入れておくこと。\n" +
             "  降る星は1種類なので、キラキラと違いコードでは差し替えない")]
    [FormerlySerializedAs("shootingStarParticle")]
    [SerializeField] private ParticleSystem fallingStarParticle;

    [Tooltip("カメラから星までの距離。キャラは約12.6 なので、\n" +
             "★これより大きくするとキャラの後ろになる")]
    [Range(1f, 40f)]
    [FormerlySerializedAs("shootingStarDistance")]
    [SerializeField] private float fallingStarDistance = 13.5f;

    [Tooltip("降り始める高さ（画面の割合。1 が上端）。1 より大きくすると画面の外から入ってくる")]
    [Range(0.5f, 1.5f)]
    [SerializeField] private float fallingStarStartY = 1.06f;

    [Tooltip("消える高さ（画面の割合。0 が下端）。0 より小さくすると画面の外へ抜ける")]
    [Range(-0.5f, 0.5f)]
    [SerializeField] private float fallingStarEndY = -0.06f;

    [Tooltip("横に降る範囲（画面の割合。0 が左端、1 が右端）。\n" +
             "少しはみ出させると、画面の端でも切れずに降ってくる")]
    [SerializeField] private Vector2 fallingStarXRange = new Vector2(-0.05f, 1.05f);

    [Tooltip("上から下まで降りきる秒数。★大きいほどゆっくり降る")]
    [Range(1f, 20f)]
    [SerializeField] private float fallingStarFallDuration = 6f;

    [Tooltip("何秒ごとに1つ降らせるか。★小さいほど数が増える")]
    [Range(0.02f, 2f)]
    [SerializeField] private float fallingStarInterval = 0.15f;

    [Tooltip("演出の開始時に、画面全体へばらまく数。\n" +
             "★0 にすると「上から順に降ってくる」（2026/8/29 にこちらを採用）。\n" +
             "  1以上にすると、開始した瞬間から画面全体が星で埋まった状態になる。\n" +
             "  出だしが寂しいと感じたときの逃げ道として欄だけ残してある")]
    [Range(0, 100)]
    [SerializeField] private int fallingStarPrefill = 0;

    [Tooltip("同時に画面へ出せる星の数。\n" +
             "★ParticleSystem の Max Particles がこれより小さいと、超えたぶんは無言で捨てられる")]
    [FormerlySerializedAs("maxStarsOnScreen")]
    [Range(1, 200)]
    [SerializeField] private int maxStarsOnScreen = 40;

    [Tooltip("星の大きさの基準（ワールド単位）。\n" +
             "★イメージ画像の実測は「画面幅の3.4%」＝ この距離で約 0.28")]
    [Range(0.02f, 5f)]
    [FormerlySerializedAs("shootingStarSize")]
    [SerializeField] private float fallingStarSize = 0.28f;

    [Tooltip("大きさのばらつき（基準の何倍〜何倍か）。\n" +
             "★イメージ画像の実測は 0.4倍〜3.4倍。既定は少し控えめの 0.5〜2.2")]
    [SerializeField] private Vector2 fallingStarSizeRange = new Vector2(0.5f, 2.2f);

    [Tooltip("横へのふらつき（ワールド単位／秒）。0 でまっすぐ落ちる")]
    [Range(0f, 2f)]
    [SerializeField] private float fallingStarSway = 0.15f;

    [Tooltip("星の色。1粒ごとにこの中から1色がランダムで選ばれる。\n" +
             "★スプライトの色に掛け算されるので、【白い星の絵】ならここで自由に色を決められる。\n" +
             "  既定の4色は、あみまるさんのイメージ画像から実測した平均色。\n" +
             "★出やすさを変えたいときは、同じ色を複数回入れる。\n" +
             "  例）クリームを2つ・ピンクを1つ入れると、クリームが2倍出やすくなる。\n" +
             "★ここで決めるのは「降り始めの色」。下へ行くほど薄くなる演出は、\n" +
             "  ParticleSystem の Color over Lifetime が担当する（コードでは触らない）")]
    [SerializeField] private Color[] fallingStarTints = new Color[]
    {
        new Color(0.94f, 0.88f, 0.75f, 1f),   // クリーム（画像の38%）
        new Color(0.93f, 0.82f, 0.82f, 1f),   // 淡いピンク（31%）
        new Color(0.93f, 0.86f, 0.60f, 1f),   // 淡い黄（24%）
        new Color(0.92f, 0.87f, 0.84f, 1f),   // ほぼ白（7%）
    };

    // ── 全体 ──────────────────────────────────────────────────────────────────

    [Header("全体")]
    [Tooltip("完了演出のひと区切りとみなす秒数。\n" +
             "★2026/8/29：リザルトはこれを待たずに先に出るようになったので、\n" +
             "  この値を変えても画面の見え方は変わらない（ログのタイミングだけが変わる）。\n" +
             "  虹のほうが長い場合は、そちらに合わせて自動で延ばす")]
    [Range(0.2f, 10f)]
    [SerializeField] private float effectDuration = 2f;

    [Header("シャンプー別の設定")]
    [SerializeField] private ShampooFinishSet[] shampooSets;

    /// <summary>設定が見つからないシャンプーで使う既定。</summary>
    private const string FallbackShampooId = "normal";

    /// <summary>いま演出中か。二重に走らせて onFinished が2回呼ばれるのを防ぐ目印。</summary>
    private bool _playing;

    /// <summary>
    /// キャラが画面上で占める範囲。Play() のときに1回だけ測って覚える。
    /// ★毎粒ごとに測り直すと、粒を出すたびに全 Renderer を走査することになり無駄。
    /// </summary>
    private Rect _characterScreenRect;

    // ── ライフサイクル ────────────────────────────────────────────────────────

    /// <summary>
    /// 起動時に、演出用のパーティクルを必ず止めておく。
    ///
    /// 【なぜ必要か】
    ///   ParticleSystem の Play On Awake は既定で ON。
    ///   そのままだとシャンプー選択画面のあいだキラキラが出っぱなしになる。
    ///   Inspector で OFF にすれば起きないが、設定漏れで演出が壊れるより、
    ///   コードで確実に止めるほうが安全。
    /// </summary>
    private void Awake()
    {
        ClearAll();
    }

    // ── 外部API ───────────────────────────────────────────────────────────────

    /// <summary>
    /// 完了演出を再生する。終わったら onFinished を1回だけ呼ぶ（不要なら null でよい）。
    ///
    /// ★2026/8/29：リザルトと完了ボタンは BathWashManager 側が【先に】出すようになった。
    ///   （演出の終わりを待っていた頃は、画面に何も無い時間が約2.5秒あった）
    ///   そのため onFinished は「演出が一段落した合図」でしかない。
    ///   渡された場合は必ず1回呼ぶ。
    ///
    /// ★キラキラと降る星は、onFinished のあとも出し続ける
    ///   （完了ボタンを押して Care 画面へ移るまで）。
    /// </summary>
    public void Play(string shampooId, System.Action onFinished)
    {
        if (_playing)
        {
            Debug.LogWarning("[BathFinish] すでに完了演出が動いています。二重再生はしません");
            return;
        }
        _playing = true;

        var set = FindSet(shampooId);

        // キャラが画面のどこに、どのくらいの大きさで映っているかを1回だけ測る
        _characterScreenRect = MeasureCharacterScreenRect();

        // ★アルファ0の色が混ざっていないか先に知らせる（見えない原因を探させないため）
        if (set != null) WarnIfTransparent(set.sparkleTints, "Sparkle Tints");
        WarnIfTransparent(fallingStarTints, "Falling Star Tints");

        // 実際に待つ秒数。虹や流れ星が長い場合は、そちらに合わせて延ばす。
        // ★演出の途中でリザルトが出てしまうのを防ぐため（時間差の演出は所要時間を先に計算する）
        float wait = effectDuration;
        if (set != null && set.useRainbow)
            wait = Mathf.Max(wait, rainbowDrawDuration);
        // ★降る星は完了ボタンを押すまで降らせ続けるので、待ち時間には数えない。
        //   （数えると「降り終わるまでリザルトが出ない」＝永久に出ないことになる）

        Debug.Log($"<color=#00E5FF>[決定]</color> [BathFinish] 完了演出を始めます shampooId={shampooId} " +
                  $"虹={(set != null && set.useRainbow)} 星={(set != null && set.useFallingStars)} " +
                  $"キラキラ={CountSparkles(set)}種 所要={wait:F2}秒 " +
                  $"キャラの画面範囲={_characterScreenRect}");

        StartCoroutine(EmitSparklesForever(set));

        if (set != null && set.useRainbow) StartCoroutine(DrawRainbow());
        else                               HideRainbow();

        if (set != null && set.useFallingStars) StartCoroutine(EmitFallingStarsForever());

        StartCoroutine(FinishAfter(wait, onFinished));
    }

    /// <summary>
    /// 演出の残りを片付ける。お風呂を始めるとき（Initialize）とスキップ時に呼ばれる。
    /// ★キラキラを止めるのはここだけ。演出が終わっても止めない（漂わせ続けるため）。
    /// </summary>
    public void ClearAll()
    {
        StopAllCoroutines();
        _playing = false;

        HideRainbow();

        if (sparkleParticles != null)
        {
            foreach (var ps in sparkleParticles) StopAndClear(ps);
        }
        StopAndClear(fallingStarParticle);
        _starDieTimes.Clear();   // ★降っている星の記録も消す。残すと次のお風呂で個数を誤判定する
    }

    // ── 虹 ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 虹を左下から右上へ、等速でかけていく。
    ///
    /// 【なぜ fillAmount で描けるのか】
    ///   虹.png の中身を実測したところ、x が進むほど y が上がる一方向の弧だった。
    ///   つまり左から右へ切り出していけば、そのまま「虹がかかっていく」ように見える。
    ///
    /// 【Image の設定をコード側で行う理由】
    ///   Inspector で Filled / Horizontal / Left を設定し忘れると、
    ///   虹が最初から全部出てしまい「徐々に」にならない。設定漏れで演出が壊れるより、
    ///   コードで明示するほうが安全。設定済みでも同じ値なので害はない。
    /// </summary>
    private IEnumerator DrawRainbow()
    {
        if (rainbowImage == null)
        {
            Debug.LogWarning("[BathFinish] Rainbow Image が未結線のため、虹は出しません。\n" +
                             "      FinishEffect を選び、BathFinishEffect の \"Rainbow Image\" 欄に " +
                             "RainbowCanvas/Rainbow をドラッグしてください");
            yield break;
        }

        rainbowImage.gameObject.SetActive(true);
        rainbowImage.type       = Image.Type.Filled;
        rainbowImage.fillMethod = Image.FillMethod.Horizontal;
        rainbowImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        rainbowImage.fillAmount = 0f;

        float elapsed = 0f;
        while (elapsed < rainbowDrawDuration)
        {
            elapsed += Time.deltaTime;
            // ★等速。加減速を入れない
            rainbowImage.fillAmount = Mathf.Clamp01(elapsed / rainbowDrawDuration);
            yield return null;
        }
        rainbowImage.fillAmount = 1f;

        Debug.Log("<color=#00E5FF>[決定]</color> [BathFinish] 虹がかかりきりました");
    }

    private void HideRainbow()
    {
        if (rainbowImage == null) return;
        rainbowImage.fillAmount = 0f;
        rainbowImage.gameObject.SetActive(false);
    }

    // ── キラキラ ──────────────────────────────────────────────────────────────

    /// <summary>
    /// キャラの周りにキラキラを出し続ける。
    ///
    /// 【なぜ出し続けるのか】
    ///   2026/8/29：最初は Emit を1回だけ呼んでいたので、Start Lifetime のぶん（約1.2秒）で
    ///   全部消えてしまい「漂っている」感じにならなかった。
    ///   完了ボタンを押して Care 画面へ移るまで漂わせたい、という要望に合わせて
    ///   出しっぱなしにする。Care へ画面が変わればシーンごと消えるので、止める処理は要らない。
    ///
    /// 【なぜ Shape ではなく画面座標で置くのか】
    ///   Shape（球の半径）だと、キャラのどこにどれだけ出るのかが Scene の設定次第になり、
    ///   実際「キャラの一部にしか出ない」状態になった。
    ///   キャラが画面上で占める矩形を測り、その中へ直接置けば、体を確実に囲める。
    /// </summary>
    private IEnumerator EmitSparklesForever(ShampooFinishSet set)
    {
        // 絵をセットして、使う ParticleSystem だけ再生状態にする
        bool[] active = PrepareSparkles(set);

        // 最初のひと吹き
        EmitSparkleBatch(set, active, sparkleBurstCount);

        if (sparkleRatePerSecond <= 0f) yield break;

        float interval = 1f / sparkleRatePerSecond;
        while (true)
        {
            yield return new WaitForSeconds(interval);
            EmitSparkleBatch(set, active, 1);
        }
    }

    /// <summary>
    /// シャンプーに対応する絵をセットする。出す番号だけ true を返す。
    /// 出さない番号の ParticleSystem は止めて空にする。
    /// </summary>
    private bool[] PrepareSparkles(ShampooFinishSet set)
    {
        int n = sparkleParticles != null ? sparkleParticles.Length : 0;
        var active = new bool[n];

        for (int i = 0; i < n; i++)
        {
            var ps = sparkleParticles[i];
            if (ps == null) continue;

            Sprite sprite = (set != null && set.sparkleSprites != null && i < set.sparkleSprites.Length)
                ? set.sparkleSprites[i] : null;

            // ★色は粒ごとに EmitParams で決めるので、ここでは白（＝そのまま）にしておく
            if (!ApplySpriteTo(ps, sprite, Color.white))
            {
                StopAndClear(ps);
                continue;
            }

            PrepareForManualEmit(ps);
            active[i] = true;

            // ★Max Particles を超えるぶんは、Unity が警告もエラーも出さずに捨てる。
            //   出し続ける作りなので、同時に存在する数の目安も見ておく
            int max = ps.main.maxParticles;
            int steady = Mathf.CeilToInt(sparkleRatePerSecond * sparkleLifetime) + sparkleBurstCount;
            if (steady > max)
            {
                Debug.LogWarning($"[BathFinish] {ps.name} の Max Particles が {max} ですが、" +
                                 $"同時に約 {steady} 粒が出ます。超えたぶんは無言で捨てられるので、" +
                                 $"Max Particles を大きくしてください");
            }
        }
        return active;
    }

    /// <summary>
    /// キラキラを、キャラが映っている矩形の中へランダムに置く。
    /// ★色は1粒ごとに set.sparkleTints からランダムに選ぶ（2026/8/29 変更）。
    /// </summary>
    private void EmitSparkleBatch(ShampooFinishSet set, bool[] active, int countPerSystem)
    {
        if (active == null || countPerSystem <= 0) return;

        var cam = Camera.main;
        if (cam == null) return;

        for (int i = 0; i < active.Length; i++)
        {
            if (!active[i]) continue;
            var ps = sparkleParticles[i];
            if (ps == null) continue;

            for (int k = 0; k < countPerSystem; k++)
            {
                // キャラの矩形を sparkleAreaScale ぶん広げた範囲のどこか
                Vector2 c = _characterScreenRect.center;
                Vector2 h = _characterScreenRect.size * 0.5f * sparkleAreaScale;
                float sx = c.x + Random.Range(-h.x, h.x);
                float sy = c.y + Random.Range(-h.y, h.y);

                Vector3 world = cam.ScreenToWorldPoint(new Vector3(sx, sy, sparkleDistance));

                var ep = new ParticleSystem.EmitParams
                {
                    position      = world,
                    // ふわっと上へ。真上だけだと揃いすぎるので左右に少し散らす
                    velocity      = Vector3.up * sparkleRiseSpeed
                                    + new Vector3(Random.Range(-0.15f, 0.15f), 0f, Random.Range(-0.15f, 0.15f)),
                    startSize     = sparkleSize * Random.Range(0.75f, 1.25f),
                    startLifetime = sparkleLifetime,
                    startColor    = PickTint(set != null ? set.sparkleTints : null),
                    applyShapeToPosition = false,
                };
                ps.Emit(ep, 1);
            }
        }
    }

    private static int CountSparkles(ShampooFinishSet set)
    {
        if (set == null || set.sparkleSprites == null) return 0;
        int n = 0;
        foreach (var s in set.sparkleSprites) if (s != null) n++;
        return n;
    }

    // ── 流れ星 ────────────────────────────────────────────────────────────────

    /// <summary>いま降っている星が消える時刻。同時に出せる数を守るために数える。</summary>
    private readonly System.Collections.Generic.List<float> _starDieTimes =
        new System.Collections.Generic.List<float>();

    /// <summary>
    /// 星を、完了ボタンが押される（＝Care 画面へ移る）まで降らせ続ける。
    ///
    /// 【なぜ「流れ星」をやめたのか】★2026/8/29
    ///   斜めに横切る流れ星は、浴室（空が無い場所）では
    ///   「星が飛んでいる」ではなく「何かが飛び散っている」に見えた。
    ///   さらに背景の棚にも黄色い星の飾りがあり、動く星と区別がつかなかった。
    ///   縦に降るものは室内でも「上から降ってきた」で成立するので、上から下へ変えた。
    ///
    /// 【降らせ方】あみまるさんのイメージ画像から数値を起こしている
    ///   ・画面全体（左端の外〜右端の外）のランダムな位置から降る
    ///   ・大きさは fallingStarSizeRange の範囲でばらつかせる（実測は 0.4〜3.4倍）
    ///   ・色は fallingStarTints からランダム（実測はクリーム/淡ピンク/淡黄/白の4色）
    ///   ・回転はランダムな角度から始める（くるくる回すのは Rotation over Lifetime の担当）
    ///
    /// 【ばらまき（Prefill）について】
    ///   開始時に画面全体へ星を置く仕組みも用意してあるが、既定は 0（＝使わない）。
    ///   2026/8/29：「同時に出すのではなく、上から降ってくる感じにしたい」という
    ///   方針に決まったため。出だしが寂しいと感じたときのために欄だけ残してある。
    ///
    /// 【なぜ止めないのか】
    ///   リザルトが出ているあいだも降らせ続けたい、という要望のため。
    ///   Care へ画面が変わればシーンごと消えるので、止める処理は要らない。
    /// </summary>
    private IEnumerator EmitFallingStarsForever()
    {
        if (fallingStarParticle == null)
        {
            Debug.LogWarning("[BathFinish] Falling Star Particle が未結線のため、星は降りません。\n" +
                             "      FinishEffect を選び、BathFinishEffect の \"Falling Star Particle\" 欄に " +
                             "星用の ParticleSystem をドラッグしてください");
            yield break;
        }

        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[BathFinish] Camera.main が見つからないため、星は降りません");
            yield break;
        }

        PrepareForManualEmit(fallingStarParticle);
        _starDieTimes.Clear();

        // ★Max Particles を超えたぶんは、Unity が警告もエラーも出さずに捨てる
        int max = fallingStarParticle.main.maxParticles;
        if (maxStarsOnScreen > max)
        {
            Debug.LogWarning($"[BathFinish] {fallingStarParticle.name} の Max Particles が {max} ですが、" +
                             $"同時に {maxStarsOnScreen} 個まで出します。超えたぶんは無言で捨てられるので、" +
                             $"Max Particles を {maxStarsOnScreen + 20} 以上にしてください");
        }

        Debug.Log($"<color=#00E5FF>[決定]</color> [BathFinish] 星を降らせ始めます " +
                  $"高さ {fallingStarStartY:F2} → {fallingStarEndY:F2} 横 {fallingStarXRange} " +
                  $"距離={fallingStarDistance} 落下={fallingStarFallDuration}秒 " +
                  $"{fallingStarInterval}秒ごと 同時{maxStarsOnScreen}個まで " +
                  $"最初に{fallingStarPrefill}個ばらまき（完了ボタンを押すまで降らせ続けます）");

        // ① 最初に画面全体へばらまく（進み具合をランダムにして「途中まで降りてきた星」を作る）
        int prefill = Mathf.Min(fallingStarPrefill, maxStarsOnScreen);
        for (int i = 0; i < prefill; i++)
            EmitOneFallingStar(cam, Random.value);

        // ② 以降は画面の上から、一定間隔で降らせ続ける
        float interval = Mathf.Max(0.02f, fallingStarInterval);
        while (true)
        {
            yield return new WaitForSeconds(interval);

            PruneFinishedStars();
            if (_starDieTimes.Count < maxStarsOnScreen)
                EmitOneFallingStar(cam, 0f);
        }
    }

    /// <summary>消えた星を記録から外す。</summary>
    private void PruneFinishedStars()
    {
        for (int i = _starDieTimes.Count - 1; i >= 0; i--)
            if (Time.time >= _starDieTimes[i]) _starDieTimes.RemoveAt(i);
    }

    /// <summary>
    /// 星を1つ降らせる。
    /// progress は「もう何割降りたことにするか」。0 なら画面の上から、0.5 なら真ん中から降り始める。
    /// ★最初のばらまきでだけ 0 以外を使う。
    /// </summary>
    private void EmitOneFallingStar(Camera cam, float progress)
    {
        float x = Random.Range(fallingStarXRange.x, fallingStarXRange.y);
        float y = Mathf.Lerp(fallingStarStartY, fallingStarEndY, Mathf.Clamp01(progress));

        // 残りの寿命 ＝ 残りの距離ぶんの時間。これで、どこから始めても落ちる速さが同じになる
        float life = fallingStarFallDuration * (1f - Mathf.Clamp01(progress));
        if (life < 0.1f) return;   // ほぼ下まで来ている星は出さない（一瞬で消えて点滅に見えるため）

        Vector3 from = cam.ScreenToWorldPoint(new Vector3(
            x * Screen.width, y * Screen.height, fallingStarDistance));
        Vector3 to = cam.ScreenToWorldPoint(new Vector3(
            x * Screen.width, fallingStarEndY * Screen.height, fallingStarDistance));

        Vector3 velocity = (to - from) / life;

        // 横へのふらつき。カメラの右方向へ少しだけずらす
        // ★ワールドの X ではなくカメラの右を使う。カメラが Y に143.82°回っているため、
        //   ワールド X を足すと画面上では斜めに見えない
        if (fallingStarSway > 0f)
            velocity += cam.transform.right * Random.Range(-fallingStarSway, fallingStarSway);

        var ep = new ParticleSystem.EmitParams
        {
            position      = from,
            velocity      = velocity,
            startSize     = fallingStarSize * Random.Range(fallingStarSizeRange.x, fallingStarSizeRange.y),
            startLifetime = life,
            startColor    = PickTint(fallingStarTints),
            rotation      = Random.Range(0f, 360f),   // 向きをそろえない
            applyShapeToPosition = false,
        };
        fallingStarParticle.Emit(ep, 1);

        _starDieTimes.Add(Time.time + life);
    }

    /// <summary>
    /// 1粒の色を候補からランダムに選ぶ。候補が空なら白（スプライトの色そのまま）。
    /// ★キラキラと降る星の両方がこれを使う。
    /// </summary>
    private static Color PickTint(Color[] palette)
    {
        if (palette == null || palette.Length == 0) return Color.white;
        return palette[Random.Range(0, palette.Length)];
    }

    /// <summary>
    /// 色の候補にアルファ0のものが混ざっていたら警告する。
    ///
    /// 【なぜ必要か】2026/8/29
    ///   Unity は Color の配列の Size を増やすと (0,0,0,0)＝完全に透明 を入れる。
    ///   これに気づかず「パーティクルが1粒も出ない」と何十分も悩む事故が実際に起きた。
    ///   見えないものは原因が分からないので、こちらから知らせる。
    /// </summary>
    private static void WarnIfTransparent(Color[] palette, string label)
    {
        if (palette == null) return;
        for (int i = 0; i < palette.Length; i++)
        {
            if (palette[i].a >= 0.05f) continue;
            Debug.LogWarning($"[BathFinish] {label} の {i} 番目の色が、ほぼ透明です（アルファ={palette[i].a:F2}）。\n" +
                             "      この色が選ばれた粒は画面に出ません。\n" +
                             "      ★Unity は配列の Size を増やすと (0,0,0,0)＝透明 を入れます。" +
                             "Inspector で色をクリックして、アルファ(A)を 255 にしてください");
        }
    }

    // ── キャラの画面上の範囲 ──────────────────────────────────────────────────

    /// <summary>
    /// キャラが画面のどこに、どのくらいの大きさで映っているかを測る。
    ///
    /// 【なぜ Renderer.bounds を使ってよいのか】
    ///   Renderer.bounds はアニメーション用に余裕を持つので、輪郭ぴったりではない。
    ///   ただしここは「だいたいこのへんが体」という用途なので問題ない。
    ///   BathDropletRain の飛沫判定も同じ理由で bounds を使っている。
    ///   ★座標変換の正しさの判定には使わないこと。
    ///
    /// 【未結線・見つからないとき】
    ///   画面の中央あたり（横 20〜80% / 縦 25〜75%）を返す。
    ///   キラキラが一切出ない状態を作らないため。理由は1行ログに残す。
    /// </summary>
    private Rect MeasureCharacterScreenRect()
    {
        Rect fallback = Rect.MinMaxRect(
            Screen.width * 0.2f, Screen.height * 0.25f,
            Screen.width * 0.8f, Screen.height * 0.75f);

        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[BathFinish] Camera.main が無いため、キラキラは画面中央に出します");
            return fallback;
        }

        if (characterAnchor == null)
        {
            Debug.LogWarning("[BathFinish] Character Anchor が未結線のため、キラキラは画面中央に出します。\n" +
                             "      FinishEffect を選び、BathFinishEffect の \"Character Anchor\" 欄に " +
                             "CharacterDisplayAnchor をドラッグしてください");
            return fallback;
        }

        Bounds bounds = default;
        bool any = false;
        var renderers = characterAnchor.GetComponentsInChildren<Renderer>(false);
        foreach (var r in renderers)
        {
            if (r == null || !r.enabled) continue;
            if (r is ParticleSystemRenderer) continue;   // 泡やキラキラ自身は体の大きさに含めない
            if (!any) { bounds = r.bounds; any = true; }
            else       bounds.Encapsulate(r.bounds);
        }

        if (!any)
        {
            Debug.LogWarning("[BathFinish] キャラの Renderer が見つからないため、キラキラは画面中央に出します");
            return fallback;
        }

        // 箱の8隅を画面へ投影して、囲む矩形を求める
        Vector3 c = bounds.center, e = bounds.extents;
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < 8; i++)
        {
            var corner = c + new Vector3(
                (i & 1) == 0 ? -e.x : e.x,
                (i & 2) == 0 ? -e.y : e.y,
                (i & 4) == 0 ? -e.z : e.z);
            Vector3 sp = cam.WorldToScreenPoint(corner);
            minX = Mathf.Min(minX, sp.x); maxX = Mathf.Max(maxX, sp.x);
            minY = Mathf.Min(minY, sp.y); maxY = Mathf.Max(maxY, sp.y);
        }

        return Rect.MinMaxRect(minX, minY, maxX, maxY);
    }

    // ── 進行 ──────────────────────────────────────────────────────────────────

    private IEnumerator FinishAfter(float seconds, System.Action onFinished)
    {
        if (seconds > 0f) yield return new WaitForSeconds(seconds);

        _playing = false;
        Debug.Log("<color=#00E5FF>[決定]</color> [BathFinish] 完了演出が一段落しました" +
                  "（キラキラと星は Care 画面へ移るまで出し続けます）");
        onFinished?.Invoke();
    }

    // ── ヘルパー ──────────────────────────────────────────────────────────────

    private ShampooFinishSet FindSet(string shampooId)
    {
        if (shampooSets == null || shampooSets.Length == 0)
        {
            Debug.LogWarning("[BathFinish] Shampoo Sets が空です。完了演出は何も出ません");
            return null;
        }

        foreach (var s in shampooSets)
            if (s != null && s.shampooId == shampooId) return s;

        foreach (var s in shampooSets)
            if (s != null && s.shampooId == FallbackShampooId)
            {
                Debug.LogWarning($"[BathFinish] '{shampooId}' の設定が無いので、" +
                                 $"'{FallbackShampooId}' の設定で演出します");
                return s;
            }

        Debug.LogWarning($"[BathFinish] '{shampooId}' の設定も '{FallbackShampooId}' の設定もありません");
        return null;
    }

    /// <summary>
    /// ParticleSystem に絵を1枚だけセットする。出さない場合は false を返す。
    ///
    /// ★1つの ParticleSystem の Texture Sheet Animation（Sprites モード）に登録する絵は、
    ///   すべて同じテクスチャから切り出したものである必要がある。別々の PNG は混ぜられない。
    ///   だから「1枚だけ入れ替える」形にしている。
    ///
    /// ※BathTouchEffect にも同じ処理がある。共通化は機能追加とは別の作業なので、
    ///   いまは重複したままにしてある（AGENTS.md「機能実装とリファクタリングを混ぜない」）。
    /// </summary>
    private static bool ApplySpriteTo(ParticleSystem ps, Sprite sprite, Color tint)
    {
        if (ps == null || sprite == null) return false;

        var tsa = ps.textureSheetAnimation;
        tsa.enabled = true;
        tsa.mode    = ParticleSystemAnimationMode.Sprites;

        for (int i = tsa.spriteCount - 1; i >= 0; i--) tsa.RemoveSprite(i);
        tsa.AddSprite(sprite);

        tsa.frameOverTime = new ParticleSystem.MinMaxCurve(0f);   // 絵を切り替えない
        tsa.startFrame    = new ParticleSystem.MinMaxCurve(0f);
        tsa.cycleCount    = 1;

        var main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(tint);

        return true;
    }

    /// <summary>
    /// 自動放出を切って、コードの Emit だけで粒が出る状態にする。
    /// あわせて「動きに手を出すモジュール」を全部切る。
    ///
    /// 【なぜモジュールを切るのか】★2026/8/29 に追加
    ///   位置も速度も EmitParams で渡しているのに、流れ星が指定した速さの
    ///   1/4 ほどでしか進まず、「ゆっくり漂う星の団子」になっていた。
    ///   原因は、複製元の ParticleSystem に残っていたモジュール
    ///   （Limit Velocity over Lifetime の減衰など）が、渡した速度を後から書き換えていたこと。
    ///
    ///   Inspector で1つずつ消してもらう方法は、見落としが出るうえに
    ///   「どれが効いているか」を画面から判断できない。
    ///   こちらで確実に切り、【何を切ったかを1行ログに出す】ほうが原因が追える。
    ///
    ///   ※Size over Lifetime と Color over Lifetime は見た目の演出なので切らない。
    ///     ONだった場合だけ、参考としてログに出す。
    /// </summary>
    private static void PrepareForManualEmit(ParticleSystem ps)
    {
        if (ps == null) return;

        var em = ps.emission;
        em.enabled = false;                     // 放出はコードから行う

        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;   // 置いた場所に留まる
        main.gravityModifier = 0f;                                    // 落とさない
        main.simulationSpeed  = 1f;                                   // 再生速度を等倍に戻す

        DisableMotionModules(ps);

        if (!ps.isPlaying) ps.Play();           // Emit した粒を動かすために本体は再生状態にしておく
    }

    /// <summary>
    /// 粒の位置や速度を後から書き換えるモジュールを全部切る。
    /// 何を切ったかを1行ログに出す（黙って直さない）。
    /// </summary>
    private static void DisableMotionModules(ParticleSystem ps)
    {
        var offed = new System.Collections.Generic.List<string>();

        var shape = ps.shape;
        if (shape.enabled) { shape.enabled = false; offed.Add("Shape"); }

        var vol = ps.velocityOverLifetime;
        if (vol.enabled) { vol.enabled = false; offed.Add("Velocity over Lifetime"); }

        var limit = ps.limitVelocityOverLifetime;
        if (limit.enabled) { limit.enabled = false; offed.Add("Limit Velocity over Lifetime"); }

        var force = ps.forceOverLifetime;
        if (force.enabled) { force.enabled = false; offed.Add("Force over Lifetime"); }

        var inherit = ps.inheritVelocity;
        if (inherit.enabled) { inherit.enabled = false; offed.Add("Inherit Velocity"); }

        var noise = ps.noise;
        if (noise.enabled) { noise.enabled = false; offed.Add("Noise"); }

        if (offed.Count > 0)
            Debug.Log($"<color=#00E5FF>[決定]</color> [BathFinish] {ps.name}：" +
                      $"{string.Join(" / ", offed)} を切りました（位置と速度はコードが決めるため）");

        // 切らないが、ONだと見た目が変わるものは知らせておく
        var note = new System.Collections.Generic.List<string>();
        if (ps.sizeOverLifetime.enabled)  note.Add("Size over Lifetime");
        if (ps.colorOverLifetime.enabled) note.Add("Color over Lifetime");
        if (note.Count > 0)
            Debug.Log($"[BathFinish] {ps.name}：{string.Join(" / ", note)} が ON です" +
                      $"（見た目の演出なので切っていません。意図と違ったら Inspector で OFF にしてください）");
    }

    private static void StopAndClear(ParticleSystem ps)
    {
        if (ps == null) return;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    /// <summary>コンポーネントを付けたとき・Reset したときに、4種類ぶんの枠を用意する。</summary>
    private void Reset()
    {
        shampooSets = new[]
        {
            new ShampooFinishSet { shampooId = "normal",    useRainbow = false, useFallingStars = false },
            new ShampooFinishSet { shampooId = "ohisama",   useRainbow = true,  useFallingStars = false },
            new ShampooFinishSet { shampooId = "hoshizora", useRainbow = false, useFallingStars = true  },
            new ShampooFinishSet { shampooId = "rainbow",   useRainbow = false, useFallingStars = false },
        };
    }
}
