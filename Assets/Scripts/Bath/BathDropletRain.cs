using UnityEngine;
using Yurufu.Bath.Foam;

/// <summary>
/// お風呂の「流す」演出で、雲の下から降ってくる雫と、体に当たったときの飛沫（A4）。
///
/// 【ParticleSystem を2つ使う】
///   dropParticle   … 落ちてくる雫（しずく.png）
///   splashParticle … 弾けたときの飛沫。しずく.png を小さくして数個飛ばす
///
/// 【★座標について】
///   ParticleSystem を Screen Space - Overlay の Canvas の子にすると、
///   ワールド座標が画面ピクセル系（x≒540 など）になり、カメラに映らなくなる。
///   BathTouchEffect と同じく、Camera.main と world 座標で直接置く方式にそろえている。
///   ★この Component が付く GameObject は Canvas の外（シーン直下）に置くこと。
///
/// 【弾ける判定：境界線方式】
///   泡は BathFoamController が「境界のワールド Y」を上から下へ下げながら消していく。
///   雫がその境界まで落ちてきたら「泡と素肌の境目に当たった」とみなして飛沫を出す。
///   ★体の輪郭は見ていない（Renderer.bounds の横幅だけで大まかに絞る）。
///     1粒ずつメッシュへレイを飛ばすと iOS で重くなるため、あえて簡易にしてある。
/// </summary>
public class BathDropletRain : MonoBehaviour
{
    [Header("結線")]
    [Tooltip("落ちてくる雫の ParticleSystem。しずく.png を設定する")]
    [SerializeField] private ParticleSystem dropParticle;

    [Tooltip("弾けたときの飛沫の ParticleSystem。しずく.png を小さくしたものを設定する。\n" +
             "未結線でも雫は降る（飛沫が出ないだけ）")]
    [SerializeField] private ParticleSystem splashParticle;

    [Tooltip("泡の本体。境界の高さと、キャラのだいたいの範囲をここから受け取る")]
    [SerializeField] private BathFoamController foam;

    [Header("降らせる範囲（画面比率 0〜1）")]
    [Tooltip("雫を降らせる横の範囲。0=画面左端 1=画面右端")]
    [SerializeField] private Vector2 spawnXRange = new Vector2(0.2f, 0.8f);

    [Tooltip("雫が出てくる高さ（0=画面下 1=画面上）。\n" +
             "★雲の少し下になる値にすること。1.05 のように画面外にすると、\n" +
             "  雲と関係ない所から降ってきて『雲から降っていない』ように見える")]
    [Range(0.3f, 1.2f)]
    [SerializeField] private float spawnY = 0.8f;

    [Tooltip("カメラからの距離。BathTouchEffect の Particle Distance と同じ値にそろえる")]
    [SerializeField] private float particleDistance = 10.5f;

    [Header("降り方")]
    [Tooltip("1秒あたり何粒降らせるか")]
    [Range(1f, 60f)]
    [SerializeField] private float dropsPerSecond = 14f;

    [Tooltip("落ちる速さ（ワールド単位／秒）。小さいほどゆっくり落ちる")]
    [Range(0.5f, 20f)]
    [SerializeField] private float fallSpeed = 4f;

    [Tooltip("雫の大きさ")]
    [Range(0.05f, 2f)]
    [SerializeField] private float dropSize = 0.35f;

    [Header("飛沫")]
    [Tooltip("1回の弾けで飛ばす粒の数")]
    [Range(1, 10)]
    [SerializeField] private int splashCount = 4;

    [Tooltip("飛沫の大きさ（雫に対する割合）")]
    [Range(0.05f, 1f)]
    [SerializeField] private float splashSizeRatio = 0.35f;

    [Tooltip("境界の判定に持たせるばらつき（ワールド単位）。\n" +
             "0 だと弾ける高さが一直線に揃って不自然になる")]
    [Range(0f, 1f)]
    [SerializeField] private float splashYJitter = 0.25f;

    // ── 内部状態 ──────────────────────────────────────────────────────────────

    private bool  _raining;
    private float _spawnAccumulator;

    /// <summary>雫の粒を読み書きするための使い回しバッファ。毎フレーム確保しないため。</summary>
    private ParticleSystem.Particle[] _dropBuf = new ParticleSystem.Particle[0];

    private bool _warned;

    private void Awake()
    {
        ConfigureSystems();
        StopRain();
    }

    // ── 公開 API ──────────────────────────────────────────────────────────────

    /// <summary>雫を降らせ始める。雲が定位置に着いたときに呼ぶ。</summary>
    public void StartRain()
    {
        if (dropParticle == null)
        {
            if (!_warned)
            {
                _warned = true;
                Debug.LogWarning("[BathDrop] Drop Particle が未結線のため、雫は降りません。\n" +
                                 "      雫用の ParticleSystem を作り、BathDropletRain の \"Drop Particle\" 欄に結線してください");
            }
            return;
        }

        _raining          = true;
        _spawnAccumulator = 0f;

        dropParticle.gameObject.SetActive(true);
        if (splashParticle != null) splashParticle.gameObject.SetActive(true);

        Debug.Log($"<color=#00E5FF>[決定]</color> [BathDrop] 雫を降らせ始めました {dropsPerSecond}粒/秒");
    }

    /// <summary>
    /// 雫を止める。すでに落ちている粒はそのまま消えるまで残す。
    /// 泡が消え切ったときに呼ぶ。
    /// </summary>
    public void StopRain()
    {
        _raining = false;
        _spawnAccumulator = 0f;
    }

    /// <summary>雫も飛沫も即座に消す。お風呂を始めるときに呼ぶ。</summary>
    public void ClearAll()
    {
        StopRain();
        if (dropParticle   != null) dropParticle.Clear(true);
        if (splashParticle != null) splashParticle.Clear(true);
    }

    // ── ParticleSystem の初期設定 ─────────────────────────────────────────────

    /// <summary>
    /// 自動放出を切り、こちらから Emit で1粒ずつ出す形にそろえる。
    /// ★Scene 側で Emission が ON のままでも、ここで確実に切る。
    /// </summary>
    private void ConfigureSystems()
    {
        SetupOne(dropParticle);
        SetupOne(splashParticle);
    }

    private static void SetupOne(ParticleSystem ps)
    {
        if (ps == null) return;

        // ★設定を書き換える前に必ず止める。
        //   再生中に duration などを変えると Unity がエラーを出すため。
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var em = ps.emission; em.enabled = false;   // 放出はコードから行う
        var sh = ps.shape;    sh.enabled = false;

        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake     = false;

        ps.Play();
    }

    // ── 毎フレーム ────────────────────────────────────────────────────────────

    private void Update()
    {
        if (_raining) SpawnDrops();
        CheckSplash();
    }

    /// <summary>
    /// 画面の上のほうから、決めた本数だけ雫を出す。
    /// ★Overlay Canvas の座標に引きずられないよう、必ずワールド座標を計算して置く。
    /// </summary>
    private void SpawnDrops()
    {
        var cam = Camera.main;
        if (cam == null || dropParticle == null) return;

        _spawnAccumulator += dropsPerSecond * Time.deltaTime;

        // ★1フレームに出しすぎないよう上限を付ける。低フレームレートで一気に降らないため
        int count = Mathf.Min(Mathf.FloorToInt(_spawnAccumulator), 6);
        if (count <= 0) return;
        _spawnAccumulator -= count;

        for (int i = 0; i < count; i++)
        {
            float rx = Random.Range(spawnXRange.x, spawnXRange.y);
            Vector3 world = cam.ScreenToWorldPoint(
                new Vector3(rx * Screen.width, spawnY * Screen.height, particleDistance));

            var ep = new ParticleSystem.EmitParams
            {
                position          = world,
                velocity          = Vector3.down * fallSpeed,
                startSize         = dropSize,
                startLifetime     = 4f,
                startColor        = Color.white,
                applyShapeToPosition = false,
            };
            dropParticle.Emit(ep, 1);
        }
    }

    /// <summary>
    /// 落ちている雫が「泡と素肌の境目」まで来たら、そこで飛沫に変える。
    ///
    /// ★1粒ずつメッシュへレイを飛ばす方式は採っていない（iOS の負荷が読めないため）。
    ///   泡の境界の高さと、キャラのだいたいの横幅だけで判定する。
    /// </summary>
    private void CheckSplash()
    {
        if (dropParticle == null || foam == null) return;

        float boundaryY = foam.RinseBoundaryWorldY;
        if (float.IsPositiveInfinity(boundaryY)) return;   // 流していないあいだは何もしない

        if (!foam.TryGetCharacterBounds(out Bounds body)) return;

        // ★境界が体より下まで下がったら、もう飛沫は出さない。
        //   これが無いと、泡が消え切ったあとに床の高さで弾け続ける（2026/8/28 の実機確認で判明）。
        if (boundaryY < body.min.y) return;

        int alive = dropParticle.particleCount;
        if (alive == 0) return;

        if (_dropBuf.Length < alive) _dropBuf = new ParticleSystem.Particle[alive];
        int n = dropParticle.GetParticles(_dropBuf, alive);

        bool changed = false;
        for (int i = 0; i < n; i++)
        {
            Vector3 pos = _dropBuf[i].position;

            // 体の横幅から外れている雫は、そのまま下へ落として消す
            if (pos.x < body.min.x || pos.x > body.max.x) continue;

            // 粒ごとに境界の高さを少しずらす。一直線に弾けないようにするため。
            // ★Random ではなく粒の乱数シードから作る。毎フレーム値が変わらないようにするため
            float jitter = splashYJitter * (Hash01(_dropBuf[i].randomSeed) - 0.5f) * 2f;

            if (pos.y > boundaryY + jitter) continue;   // まだ境界まで届いていない

            SpawnSplash(pos);

            // この雫は役目を終えたので消す
            _dropBuf[i].remainingLifetime = 0f;
            changed = true;
        }

        if (changed) dropParticle.SetParticles(_dropBuf, n);
    }

    /// <summary>当たった場所で、小さい雫を数個ランダムな向きへ飛ばす。</summary>
    private void SpawnSplash(Vector3 worldPos)
    {
        if (splashParticle == null) return;

        for (int i = 0; i < splashCount; i++)
        {
            // 上向き半円へ散らす。真下へは飛ばさない
            float ang = Random.Range(15f, 165f) * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);

            var ep = new ParticleSystem.EmitParams
            {
                position      = worldPos,
                velocity      = dir * Random.Range(1.2f, 2.6f),
                startSize     = dropSize * splashSizeRatio * Random.Range(0.7f, 1.3f),
                startLifetime = Random.Range(0.25f, 0.45f),
                startColor    = Color.white,
                applyShapeToPosition = false,
            };
            splashParticle.Emit(ep, 1);
        }
    }

    /// <summary>粒の乱数シードから 0〜1 の値を作る。同じ粒なら毎フレーム同じ値になる。</summary>
    private static float Hash01(uint seed)
    {
        uint x = seed;
        x ^= x >> 16; x *= 0x7feb352du;
        x ^= x >> 15; x *= 0x846ca68bu;
        x ^= x >> 16;
        return (x & 0xFFFFFF) / (float)0xFFFFFF;
    }
}
