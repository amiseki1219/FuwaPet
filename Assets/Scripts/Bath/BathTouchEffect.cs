using UnityEngine;

/// <summary>
/// こすっている指に追従するパーティクル（お風呂画面）。
///
/// パーティクルシステムを2つ使う:
///   touchParticle  … 泡。シャンプーごとに「色だけ」を粒ごとランダムで変える
///   accentParticle … おひさま / 星などの飾り。色を付けずスプライトのまま出す
///
/// なぜ2つ必要か:
///   ParticleSystem の色設定は、そのシステムの全ての粒に一律で掛かる。
///   1つにまとめると「泡だけ色をランダムにして、おひさまは元の色のまま」ができず、
///   おひさままで緑や青に染まってしまう。役割ごとに分けるのが確実。
/// </summary>
public class BathTouchEffect : MonoBehaviour
{
    /// <summary>シャンプー1種類ぶんの設定。Inspector から差し替えられる。</summary>
    [System.Serializable]
    public class ShampooParticleSet
    {
        [Tooltip("BathSceneManager の AllShampoo と同じ ID。normal / ohisama / hoshizora / rainbow")]
        public string shampooId;

        [Tooltip("泡の色。粒ごとにこの中から1色がランダムで選ばれる（最大8色）")]
        public Color[] bubbleColors;

        [Tooltip("おひさま・星などの飾り。空にするとそのシャンプーでは飾りを出さない")]
        public Sprite accentSprite;

        [Tooltip("飾りの色。白にするとスプライトの色そのまま出る")]
        public Color accentTint = Color.white;

        // ── 2種目以降の飾り（2026/8/28 追加）────────────────────────────
        // ★なぜ配列で持つのか
        //   Unity の ParticleSystem は、Texture Sheet Animation に登録するスプライトが
        //   すべて同じテクスチャから切り出されている必要がある。
        //   別々の PNG（おひさま と 星 など）は1つの ParticleSystem に混ぜられない。
        //   → 「PNG 1枚につき ParticleSystem 1つ」になるため、
        //      BathTouchEffect 側の extraAccentParticles と同じ順番で対応させる。
        //   3種目・4種目が要るときも、Scene に ParticleSystem を足すだけで済む（コード変更不要）。

        [Tooltip("2種目以降の飾り。BathTouchEffect の Extra Accent Particles と同じ順番で対応する。\n" +
                 "空の要素、または対応する ParticleSystem が無い番号では、その飾りは出ない")]
        public Sprite[] extraAccentSprites;

        [Tooltip("2種目以降の飾りの色。要素が足りないぶんは白（スプライトの色そのまま）になる")]
        public Color[] extraAccentTints;
    }

    [Header("パーティクル")]
    [Tooltip("泡を出すパーティクルシステム")]
    [SerializeField] private ParticleSystem touchParticle;

    [Tooltip("おひさま・星を出すパーティクルシステム。未結線でも動作する（飾りが出ないだけ）")]
    [SerializeField] private ParticleSystem accentParticle;

    // ★2026/8/28 追加。2種目以降の飾り用。
    //   1つの ParticleSystem には1枚のスプライトしか入れられない（同一テクスチャ制約）ため、
    //   種類を増やすぶんだけ ParticleSystem を並べる。
    //   Scene では AccentParticleEffect を複製して AccentParticleEffect2 … と足していく。
    //   ShampooParticleSet.extraAccentSprites と【同じ順番】で対応する。
    [Tooltip("2種目以降の飾りを出すパーティクルシステム。\n" +
             "Scene の AccentParticleEffect を複製して並べる。\n" +
             "各シャンプーの Extra Accent Sprites と同じ順番で対応する")]
    [SerializeField] private ParticleSystem[] extraAccentParticles;

    // カメラからパーティクルを出すまでの距離（ワールド単位）。
    //
    // なぜ SerializeField にしたか:
    //   もともと const 3f がハードコードされていた。これは Orthographic カメラ
    //   （位置 Z=-10 / キャラ前面 Z≒-5.74）を前提にした値で、カメラを動かすと破綻する。
    //   2026/8/23 に Bath のカメラを Perspective (-8.53, 1.82, 9.89) へ作り替えたところ、
    //   カメラからキャラまでの距離が約 12.6 になり、3 のままでは泡がカメラの目の前に出ていた。
    //
    // 目安: 「カメラからキャラまでの距離」より少しだけ小さい値（＝キャラの手前）にする。
    [SerializeField] private float particleDistance = 10.5f;

    // 指に追従させるか。
    //
    // ON  … 指の位置から泡が出る（従来）
    // OFF … Scene に置いた位置から動かない。キャラのまわりに置いて「漂う泡」にしたいときはこちら。
    //        こすっている間だけ出る、という制御はそのまま効く。
    [Tooltip("OFF にすると指を追いかけず、Scene に置いた位置から出続ける")]
    [SerializeField] private bool followFinger = true;

    // ── 泡のスプライト（大きさ違い） ──────────────────────────────────────────
    //
    // 大・中・小のように大きさの違う泡を登録すると、粒ごとに1枚がランダムで選ばれる。
    // 色はシャンプーごとに別で掛かるので、ここには「白い泡」だけを入れる。
    //
    // ※Unity の制約で、ここに入れるスプライトは全て同じテクスチャ（1枚のスプライトシート）
    //   から切り出したものである必要がある。バラバラの PNG を入れるとエラーになる。
    //
    // 空のままでも動く（パーティクル側に元から設定されている絵がそのまま使われる）。
    [Header("泡のスプライト（大きさ違い・全シャンプー共通）")]
    [SerializeField] private Sprite[] bubbleSprites;

    // ── シャンプー別の設定（requirements.md §5「シャンプーごとに泡の色・アニメーションが異なる」） ──
    [Header("シャンプー別の設定")]
    [SerializeField] private ShampooParticleSet[] shampooSets;

    private const string FallbackShampooId = "normal";

    /// <summary>いま選ばれているシャンプーで飾りを出すかどうか。SetShampoo で決まる。</summary>
    private bool _accentEnabled;

    /// <summary>
    /// 2種目以降の飾りを出すかどうか。extraAccentParticles と同じ長さ・同じ順番。
    /// SetShampoo で作り直す。シャンプーによって出す種類が違うので、番号ごとに持つ。
    /// </summary>
    private bool[] _extraAccentEnabled = new bool[0];

    // ── シャンプー切り替え ────────────────────────────────────────────────────

    /// <summary>
    /// シャンプーの種類に応じて、泡の色と飾りの絵を切り替える。
    /// BathWashManager.Initialize() から1回だけ呼ばれる。
    /// </summary>
    public void SetShampoo(string shampooId)
    {
        if (touchParticle == null)
        {
            Debug.LogWarning("[Bath] SetShampoo: touchParticle が未結線です");
            return;
        }

        ShampooParticleSet set = FindSet(shampooId);
        if (set == null)
        {
            Debug.LogWarning($"[Bath] SetShampoo: shampooId={shampooId} に対応する設定がありません。パーティクルは変更しません");
            return;
        }

        ApplyBubbleSprites();
        ApplyBubbleColors(set);
        ApplyAccent(set);
        ApplyExtraAccents(set);

        int extraOn = 0;
        for (int i = 0; i < _extraAccentEnabled.Length; i++) if (_extraAccentEnabled[i]) extraOn++;

        Debug.Log($"<color=#00E5FF>[決定]</color> [Bath] パーティクルを切り替えました shampooId={shampooId} 泡の色={(set.bubbleColors != null ? set.bubbleColors.Length : 0)}色 飾り={(_accentEnabled ? set.accentSprite.name : "なし")} 2種目以降={extraOn}個");
    }

    /// <summary>ID に一致するセットを探す。見つからなければ normal にフォールバックする。</summary>
    private ShampooParticleSet FindSet(string shampooId)
    {
        if (shampooSets == null || shampooSets.Length == 0) return null;

        foreach (var s in shampooSets)
        {
            if (s != null && s.shampooId == shampooId) return s;
        }

        foreach (var s in shampooSets)
        {
            if (s != null && s.shampooId == FallbackShampooId) return s;
        }

        return null;
    }

    /// <summary>
    /// 指定したシャンプーの「泡の色」を返す。
    ///
    /// なぜ外に出すのか:
    ///   体に付く泡（BubbleController）にも同じ色を使いたいため。
    ///   色の設定を2箇所に持つと必ずズレるので、Inspector の設定を正として
    ///   ここから読み取る形にした（お風呂の MaxBathPerDay が2箇所にある問題の再発防止）。
    ///
    /// 見つからない・未設定のときは null を返す。呼び出し側は「色を変えない」を選ぶこと。
    /// </summary>
    public Color[] GetBubbleColors(string shampooId)
    {
        ShampooParticleSet set = FindSet(shampooId);
        if (set == null) return null;
        if (set.bubbleColors == null || set.bubbleColors.Length == 0) return null;
        return set.bubbleColors;
    }

    /// <summary>
    /// 泡の絵（大きさ違い）を登録する。
    ///
    /// Texture Sheet Animation を Sprites モードにして複数枚を登録し、
    /// Start Frame をランダムにすることで「粒ごとに違う大きさの泡」を出している。
    /// bubbleSprites が空のときは何もしない（パーティクル側の元の設定をそのまま使う）。
    /// </summary>
    private void ApplyBubbleSprites()
    {
        if (bubbleSprites == null || bubbleSprites.Length == 0) return;

        var tsa = touchParticle.textureSheetAnimation;
        tsa.enabled = true;
        tsa.mode = ParticleSystemAnimationMode.Sprites;

        for (int i = tsa.spriteCount - 1; i >= 0; i--)
        {
            tsa.RemoveSprite(i);
        }

        int added = 0;
        foreach (var sprite in bubbleSprites)
        {
            if (sprite == null) continue;
            tsa.AddSprite(sprite);
            added++;
        }

        if (added == 0)
        {
            Debug.LogWarning("[Bath] ApplyBubbleSprites: 泡のスプライトが全て未設定です");
            return;
        }

        tsa.frameOverTime = new ParticleSystem.MinMaxCurve(0f);   // 絵を切り替えない
        tsa.startFrame    = new ParticleSystem.MinMaxCurve(0f, added); // 粒ごとに1枚選ぶ
        tsa.cycleCount    = 1;
    }

    /// <summary>
    /// 泡の色を設定する。
    ///
    /// 粒ごとに「決まった数色の中から1色」を選ばせたいので、
    /// GradientMode.Fixed（補間せず段階で切り替わる）のグラデーションを組み立て、
    /// MinMaxGradient を RandomColor モードにしている。
    /// こうすると、中間色が混ざらず指定した色そのものだけが出る。
    /// </summary>
    private void ApplyBubbleColors(ShampooParticleSet set)
    {
        var main = touchParticle.main;

        if (set.bubbleColors == null || set.bubbleColors.Length == 0)
        {
            // 色未設定なら白（スプライトそのまま）
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white);
            return;
        }

        if (set.bubbleColors.Length == 1)
        {
            main.startColor = new ParticleSystem.MinMaxGradient(set.bubbleColors[0]);
            return;
        }

        var gradient = new ParticleSystem.MinMaxGradient(BuildDiscreteGradient(set.bubbleColors));
        gradient.mode = ParticleSystemGradientMode.RandomColor;
        main.startColor = gradient;
    }

    /// <summary>
    /// 指定された色を「混ざらない帯」として並べた Gradient を作る。
    /// Unity の Gradient はキーを最大8個までしか持てないため、9色目以降は切り捨てる。
    /// </summary>
    private static Gradient BuildDiscreteGradient(Color[] colors)
    {
        int count = Mathf.Min(colors.Length, 8);

        var colorKeys = new GradientColorKey[count];
        var alphaKeys = new GradientAlphaKey[count];

        for (int i = 0; i < count; i++)
        {
            // 0, 1/n, 2/n ... と等間隔に置くことで、各色が同じ確率で選ばれる
            float time = (float)i / count;
            colorKeys[i] = new GradientColorKey(colors[i], time);
            alphaKeys[i] = new GradientAlphaKey(colors[i].a, time);
        }

        var g = new Gradient();
        g.mode = GradientMode.Fixed; // 補間しない＝指定した色だけが出る
        g.SetKeys(colorKeys, alphaKeys);
        return g;
    }

    /// <summary>
    /// 飾り1種目（おひさま・星）を設定する。
    /// accentSprite が空、または accentParticle が未結線なら飾りを出さない。
    /// </summary>
    private void ApplyAccent(ShampooParticleSet set)
    {
        _accentEnabled = ApplyAccentTo(accentParticle, set.accentSprite, set.accentTint);
    }

    /// <summary>
    /// 飾り2種目以降を設定する。
    ///
    /// extraAccentParticles[i] と set.extraAccentSprites[i] を【同じ番号】で対応させる。
    /// 片方しか無い番号は「出さない」。シャンプーごとに種類数が違ってよい。
    /// （例：おひさま は2種類、せっけん は1種類だけ）
    /// </summary>
    private void ApplyExtraAccents(ShampooParticleSet set)
    {
        int slots = extraAccentParticles != null ? extraAccentParticles.Length : 0;

        // 結線されている ParticleSystem の数ぶんだけフラグを持つ
        if (_extraAccentEnabled.Length != slots) _extraAccentEnabled = new bool[slots];

        for (int i = 0; i < slots; i++)
        {
            Sprite sprite = (set.extraAccentSprites != null && i < set.extraAccentSprites.Length)
                ? set.extraAccentSprites[i]
                : null;

            // 色は要素が足りなければ白（＝スプライトの色そのまま）にする
            Color tint = (set.extraAccentTints != null && i < set.extraAccentTints.Length)
                ? set.extraAccentTints[i]
                : Color.white;

            _extraAccentEnabled[i] = ApplyAccentTo(extraAccentParticles[i], sprite, tint);
        }
    }

    /// <summary>
    /// ParticleSystem 1つに飾りスプライトを1枚設定する。ApplyAccent と ApplyExtraAccents の共通処理。
    ///
    /// ★1枚しか登録しない理由
    ///   Unity は Texture Sheet Animation に登録するスプライトが同一テクスチャであることを要求する。
    ///   別々の PNG を混ぜられないので、種類を増やすときは ParticleSystem 自体を増やす。
    ///
    /// 戻り値: この ParticleSystem で飾りを出すなら true。
    ///         未結線・スプライト未設定なら放出を止めて false を返す。
    /// </summary>
    private static bool ApplyAccentTo(ParticleSystem ps, Sprite sprite, Color tint)
    {
        if (ps == null) return false;

        if (sprite == null)
        {
            // このシャンプーでは、この番号の飾りは出さない。放出を止める
            var off = ps.emission;
            off.enabled = false;
            return false;
        }

        // Texture Sheet Animation に1枚だけ登録して、その絵を出す
        var tsa = ps.textureSheetAnimation;
        tsa.enabled = true;
        tsa.mode = ParticleSystemAnimationMode.Sprites;

        for (int i = tsa.spriteCount - 1; i >= 0; i--)
        {
            tsa.RemoveSprite(i);
        }
        tsa.AddSprite(sprite);

        tsa.frameOverTime = new ParticleSystem.MinMaxCurve(0f); // 絵を切り替えない
        tsa.startFrame    = new ParticleSystem.MinMaxCurve(0f);
        tsa.cycleCount    = 1;

        var main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(tint);

        return true;
    }

    /// <summary>コンポーネント追加時・Reset 時に、4種類ぶんの枠を用意する。</summary>
    private void Reset()
    {
        shampooSets = new[]
        {
            new ShampooParticleSet
            {
                shampooId = "normal",
                bubbleColors = new[]
                {
                    new Color(1f, 1f, 1f),
                    new Color(0.90f, 0.95f, 1f),
                },
            },
            new ShampooParticleSet
            {
                shampooId = "ohisama",
                bubbleColors = new[]
                {
                    new Color(1f, 0.78f, 0.85f),
                    new Color(1f, 0.66f, 0.76f),
                    new Color(1f, 0.88f, 0.92f),
                },
            },
            new ShampooParticleSet
            {
                shampooId = "hoshizora",
                bubbleColors = new[]
                {
                    new Color(0.80f, 0.72f, 0.96f),
                    new Color(0.68f, 0.60f, 0.92f),
                    new Color(0.90f, 0.86f, 1f),
                },
            },
            new ShampooParticleSet
            {
                shampooId = "rainbow",
                bubbleColors = new[]
                {
                    new Color(1f,    0.45f, 0.50f), // 赤
                    new Color(1f,    0.72f, 0.42f), // 橙
                    new Color(1f,    0.95f, 0.55f), // 黄
                    new Color(0.62f, 0.95f, 0.68f), // 緑
                    new Color(0.55f, 0.85f, 1f),    // 水色
                    new Color(0.66f, 0.68f, 1f),    // 青紫
                    new Color(0.90f, 0.66f, 1f),    // 紫
                },
            },
        };
    }

    // ── 位置・再生まわり ──────────────────────────────────────────────────────

    /// <summary>
    /// 画面座標を「泡を出している平面」のワールド座標に変換して返す。
    ///
    /// なぜ外に出すのか:
    ///   体に泡を置く BathBubblePainter が、指の位置を同じ平面に載せる必要があるため。
    ///   変換の距離（particleDistance）を2箇所に持つとカメラを動かしたときに必ずズレるので、
    ///   この1箇所を正として共有する。
    /// </summary>
    public Vector3 ScreenToWorldPosition(Vector2 screenPosition)
    {
        return ScreenToWorld(screenPosition);
    }

    private Vector3 ScreenToWorld(Vector2 screenPosition)
    {
        var cam = Camera.main;
        if (cam == null) return Vector3.zero;

        return cam.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, particleDistance));
    }

    // 一発再生（OnPointerDown 時など）
    public void Play(Vector2 screenPosition)
    {
        Vector3 world = ScreenToWorld(screenPosition);

        if (touchParticle != null)
        {
            touchParticle.transform.position = world;
            touchParticle.Play();
        }

        if (_accentEnabled && accentParticle != null)
        {
            accentParticle.transform.position = world;
            accentParticle.Play();
        }

        for (int i = 0; i < _extraAccentEnabled.Length; i++)
        {
            if (!_extraAccentEnabled[i]) continue;
            var ps = extraAccentParticles[i];
            if (ps == null) continue;
            ps.transform.position = world;
            ps.Play();
        }
    }

    // 毎フレーム位置を更新（ドラッグ追従用）
    public void UpdatePosition(Vector2 screenPosition)
    {
        Vector3 world = ScreenToWorld(screenPosition);

        if (touchParticle != null) touchParticle.transform.position = world;
        if (accentParticle != null) accentParticle.transform.position = world;

        if (extraAccentParticles != null)
        {
            foreach (var ps in extraAccentParticles)
                if (ps != null) ps.transform.position = world;
        }
    }

    // 連続放出：emission を有効化し、停止中なら Play() する
    public void StartContinuous(Vector2 screenPosition)
    {
        if (touchParticle == null)
        {
            Debug.LogWarning("[TouchEffect] StartContinuous: touchParticle is null");
            return;
        }

        // 先に位置を確定してから再生する（原点に出ないよう）
        // followFinger が OFF のときは Scene に置いた位置のまま動かさない
        if (followFinger) UpdatePosition(screenPosition);

        StartOne(touchParticle);

        if (_accentEnabled && accentParticle != null)
        {
            StartOne(accentParticle);
        }

        for (int i = 0; i < _extraAccentEnabled.Length; i++)
        {
            if (!_extraAccentEnabled[i]) continue;
            var ps = extraAccentParticles[i];
            if (ps != null) StartOne(ps);
        }
    }

    /// <summary>1つのパーティクルシステムの放出を開始する。</summary>
    private static void StartOne(ParticleSystem ps)
    {
        var emission = ps.emission;
        emission.enabled = true;

        if (!ps.isPlaying)
        {
            var main = ps.main;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ps.Play();
        }
    }

    // 新規放出だけ止める（Stop() を呼ばず StopAction を起動させない）
    public void StopContinuous()
    {
        if (touchParticle != null)
        {
            var e = touchParticle.emission;
            e.enabled = false;
        }

        if (accentParticle != null)
        {
            var e = accentParticle.emission;
            e.enabled = false;
        }

        // ★結線されている全部を止める。_extraAccentEnabled は見ない。
        //   シャンプーを切り替えた直後など、フラグと実際の放出状態がずれても
        //   「出しっぱなし」を作らないため。
        if (extraAccentParticles != null)
        {
            foreach (var ps in extraAccentParticles)
            {
                if (ps == null) continue;
                var ee = ps.emission;
                ee.enabled = false;
            }
        }
    }
}
