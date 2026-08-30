using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// こすった場所にキャラの泡を置いていく担当。
///
/// 【なぜ BathWashManager と分けたか】
///   BathWashManager は「こすり判定・ゲージ・清潔値の確定・画面遷移」を持っていて、すでに大きい。
///   泡の見た目づくりは別の関心事なので、増えても互いに影響しないよう分離した。
///
/// 【置く場所の考え方】
///   指の画面座標からカメラの視線を飛ばし、許可エリアの楕円体
///   （＝Scene ビューに描いている青い球）の「カメラに近い側の表面」に泡を置く。
///   キャラのメッシュに Raycast はしない。
///   理由: キャラは SkinnedMeshRenderer で動くため MeshCollider が追従せず、
///        アニメ中に判定がズレる。5キャラぶんの Read/Write 設定も要る。
///        許可エリアの球はもともと「画面で見て体に重なるように」置いたものなので、
///        これを体の代わりとみなせば、丸みに沿った奥行きが軽い計算で手に入る。
///
///   Place On Area Surface を OFF にすると、以前の「カメラ前の平面に載せる」方式に戻る。
///   平面方式は奥行きが全部同じになるため、泡が板のように並んでしまう（既知の見た目の問題）。
///
/// 【目と口を避ける仕組み】
///   「泡を置かない場所」を Scene 上に空オブジェクトで置いてもらい、その半径内には置かない。
///   判定は必ず“泡の平面に載せてから”行う。
///   目・口のオブジェクトはキャラの表面（カメラから見て奥）にあるため、
///   そのまま3D距離で測ると奥行きのぶん遠く判定され、避けきれないことがある。
/// </summary>
public class BathBubblePainter : MonoBehaviour
{
    [Header("結線")]
    [Tooltip("複製元の泡。BubbleSprite.prefab")]
    [SerializeField] private BubbleController bubblePrefab;

    [Tooltip("置いた泡の親。BubbleGroup を入れる")]
    [SerializeField] private Transform bubbleParent;

    [Tooltip("画面座標の変換とシャンプー色をもらう。WashPanel の BathTouchEffect")]
    [SerializeField] private BathTouchEffect touchEffect;

    [Header("置きかた")]
    [Tooltip("画面に出せる泡の最大数。多すぎると重くなる")]
    [SerializeField] private int maxBubbleCount = 40;

    [Tooltip("この距離より近い場所には新しい泡を置かない（ワールド単位）。重なりすぎ防止")]
    [SerializeField] private float minBubbleDistance = 0.25f;

    [Tooltip("泡ごとの大きさのばらつき。複製元の Scale に掛かる倍率の範囲")]
    [SerializeField] private Vector2 scaleRandomRange = new Vector2(0.7f, 1.3f);

    [Tooltip("指を動かした軌跡を、この画面ピクセル間隔で埋める。小さいほど隙間なく付く")]
    [SerializeField] private float paintStepPixels = 18f;

    [Tooltip("1フレームに置ける泡の数の上限。素早くこすったときの負荷を抑えるため")]
    [SerializeField] private int maxSpawnPerFrame = 12;

    [Header("キャラ別の設定（characterId で自動的に切り替わる）")]
    [Tooltip("キャラごとの許可エリア・除外エリア。一致するものが無ければ下の共通設定を使う")]
    [SerializeField] private CharacterAreaSet[] characterAreaSets;

    [Header("キャラの動きへの追従")]
    [Tooltip("ON にすると、泡をキャラのボーンにぶら下げてアニメに追従させる")]
    [SerializeField] private bool attachToCharacterBone = true;

    [Tooltip("ON にすると、許可エリア・除外エリアの球そのものをキャラのボーンにぶら下げて、アニメに追従させる")]
    [SerializeField] private bool attachAreasToBone = true;

    [Tooltip("キャラを探す起点。空なら Bubble Parent の親（CharacterDisplayAnchor）を使う。ぽこ用に PokoRoot も足せる")]
    [SerializeField] private Transform[] characterRoots;

    [Header("泡を置いてよい場所（キャラの体）")]
    [Tooltip("この円の中だけに泡を置く。空っぽのときは制限なし（どこでも置ける）")]
    [SerializeField] private ExclusionArea[] allowedAreas;

    [Header("泡を置かない場所（目・口）")]
    [Tooltip("許可エリアの中でも、ここに入っていたら置かない")]
    [SerializeField] private ExclusionArea[] exclusionAreas;

    [Header("奥行きの決め方")]
    [Tooltip("ON: 許可エリアの球の表面に泡を置く（丸みに沿う）。OFF: 以前どおりカメラ前の平面に置く")]
    [SerializeField] private bool placeOnAreaSurface = true;

    [Tooltip("球の表面から、視線に沿ってカメラ側へ戻す量（ワールド単位）。0 だと球がメッシュより奥の場所で泡が埋まって消える")]
    [SerializeField] private float surfaceLift = 0.25f;

    [Tooltip("泡の板を体の面に沿って傾ける量。0=全部カメラ正面（平らに見える） 1=面の法線どおり（輪郭の泡は真横を向いて消える）")]
    [Range(0f, 1f)]
    [SerializeField] private float surfaceTilt = 0.7f;

    [Header("流す演出")]
    [Tooltip("上の泡から下の泡まで、消え始めが行き渡るまでの秒数")]
    [SerializeField] private float rinseSpreadDuration = 3.5f;

    [Tooltip("泡1つが消えるのにかかる秒数")]
    [SerializeField] private float bubbleFadeDuration = 0.8f;

    [Tooltip("消えるときに何ワールド単位ぶん上へ浮くか")]
    [SerializeField] private float riseDistance = 0.4f;

    /// <summary>泡を置かない範囲1つぶん。</summary>
    /// <summary>
    /// キャラ1体ぶんの設定。
    ///
    /// なぜキャラごとに分けるのか:
    ///   5体は体の形も大きさも違う。共通の円ひとつでは、ある子には大きすぎ、別の子には小さすぎる。
    ///   セットが見つからないキャラは共通設定（下の Allowed Areas / Exclusion Areas）に落ちるので、
    ///   1体ずつ順番に追加していける。
    /// </summary>
    [Serializable]
    public class CharacterAreaSet
    {
        [Tooltip("セーブデータの selectedCharacterId と同じ文字列。すべて小文字（poko / eru / koko / paru / piyoko）")]
        public string characterId;

        [Tooltip("この子の体。泡を置いてよい範囲")]
        public ExclusionArea[] allowedAreas;

        [Tooltip("この子の目・口。泡を置かない範囲")]
        public ExclusionArea[] exclusionAreas;

        [Tooltip("ボーンを探す起点。空なら共通の Character Roots を使う")]
        public Transform characterRoot;
    }

    [Serializable]
    public class ExclusionArea
    {
        [Tooltip("Inspector で見分けるための名前。処理には使わない（例: 目_L）")]
        public string label;

        [Tooltip("中心にする空オブジェクト。目や口の位置に置く")]
        public Transform center;

        [Tooltip("この距離までは泡を置かない（ワールド単位）")]
        public float radius = 0.3f;
    }

    /// <summary>いま置いてある泡。流すときに上から順に消すので順番は問わない。</summary>
    private readonly List<BubbleController> _spawned = new List<BubbleController>();

    /// <summary>このお風呂で使う泡の色。空なら泡の絵の色そのまま。</summary>
    private Color[] _colors;

    /// <summary>複製元の大きさ。毎回 prefab から読むのを避けてキャッシュする。</summary>
    private Vector3 _prefabScale = Vector3.one;

    /// <summary>泡の絵の半径（Scale 1 のときのワールド単位）。判定に泡の大きさを含めるために使う。</summary>
    private float _spriteRadius = 0.5f;

    /// <summary>前フレームに泡を置こうとした画面座標。指の軌跡を線でつなぐために覚えておく。</summary>
    private Vector2 _lastPaintScreenPos;

    /// <summary>_lastPaintScreenPos が有効かどうか。指を離す・範囲外へ出るとリセットする。</summary>
    private bool _hasLastPaintPos;

    /// <summary>キャラのボーン一覧。泡をぶら下げる先を探すのに使う。</summary>
    private Transform[] _bones;

    /// <summary>このお風呂で実際に使う許可エリア。キャラ別セットがあればそれ、無ければ共通設定。</summary>
    private ExclusionArea[] _activeAllowed;

    /// <summary>このお風呂で実際に使う除外エリア。</summary>
    private ExclusionArea[] _activeExclusion;

    /// <summary>このお風呂で使うキャラのボーン探索の起点。</summary>
    private Transform _activeCharacterRoot;

    /// <summary>
    /// すでにボーンへ付け替えたエリアの中心オブジェクト。
    /// Begin() は1シーンで何度も呼ばれうるので、二重に付け替えないよう覚えておく。
    /// </summary>
    private readonly HashSet<Transform> _attachedAreas = new HashSet<Transform>();

    /// <summary>
    /// 「球の表面に置けた」ことを1回だけログに出すためのフラグ。
    /// 毎フレーム出すとログが埋まるので、お風呂1回につき最初の1個だけ出す。
    /// </summary>
    private bool _loggedSurfacePlacement;

    // ── 開始・後片付け ────────────────────────────────────────────────────────

    /// <summary>
    /// お風呂を始めるときに呼ぶ。前回の泡を片付けて、シャンプーの色を取り込む。
    /// </summary>
    public void Begin(string shampooId)
    {
        ClearAll();

        if (bubblePrefab != null)
        {
            _prefabScale = bubblePrefab.transform.localScale;

            // 保険: Prefab の Scale が 0 のまま保存されていると、置く泡が全部 0 倍になって見えない
            if (_prefabScale.sqrMagnitude < 0.0001f)
            {
                Debug.LogWarning("[BathBubble] BubbleSprite の Scale が 0 です。1 として扱います（Prefab の Scale を 1 に直してください）");
                _prefabScale = Vector3.one;
            }
        }

        _spriteRadius = MeasureSpriteRadius();
        _loggedSurfacePlacement = false;   // お風呂ごとに1回だけログを出したいので戻す
        SelectAreaSet();
        CollectBones();
        AttachAreasToBones();

        _colors = touchEffect != null ? touchEffect.GetBubbleColors(shampooId) : null;

        Debug.Log($"<color=#00E5FF>[決定]</color> [BathBubble] 泡の準備ができました shampooId={shampooId} " +
                  $"色数={(_colors != null ? _colors.Length : 0)} 上限={maxBubbleCount}個 " +
                  $"許可エリア={(allowedAreas != null ? allowedAreas.Length : 0)}個 除外エリア={(exclusionAreas != null ? exclusionAreas.Length : 0)}個");
    }

    /// <summary>置いてある泡を全部消す。</summary>
    public void ClearAll()
    {
        foreach (var b in _spawned)
        {
            if (b != null) Destroy(b.gameObject);
        }
        _spawned.Clear();
        _hasLastPaintPos = false;
    }

    // ── 泡を置く ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 指の位置に泡を置こうとする。置けない条件に当たったら何もしない。
    /// こすっている間、毎フレーム呼ばれる前提で書いてある。
    /// </summary>
    public void TryPaint(Vector2 screenPosition)
    {
        if (bubblePrefab == null || bubbleParent == null || touchEffect == null) return;

        // 【なぜ「線」で置くのか】
        //   1フレームに1個だけ置く作りだと、指を素早く動かしたときに
        //   フレームとフレームの間が飛んで、こすったのに泡が付かない場所ができる。
        //   前フレームの位置から今の位置までを一定間隔で埋めることで、
        //   こすった軌跡どおりに泡が乗るようにしている。
        if (!_hasLastPaintPos)
        {
            TryPaintAt(screenPosition);
            _lastPaintScreenPos = screenPosition;
            _hasLastPaintPos = true;
            return;
        }

        float distance = Vector2.Distance(_lastPaintScreenPos, screenPosition);
        float step = Mathf.Max(paintStepPixels, 1f);

        // 動いた距離を step で割った数だけ、間を埋める
        int steps = Mathf.Clamp(Mathf.FloorToInt(distance / step), 1, Mathf.Max(maxSpawnPerFrame, 1));

        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            TryPaintAt(Vector2.Lerp(_lastPaintScreenPos, screenPosition, t));
        }

        _lastPaintScreenPos = screenPosition;
    }

    /// <summary>
    /// 指を離した／こすり範囲の外へ出たときに呼ぶ。
    /// 次に触ったとき、離れた2点が線でつながって泡が置かれるのを防ぐ。
    /// </summary>
    public void EndStroke()
    {
        _hasLastPaintPos = false;
    }

    /// <summary>1点に泡を置こうとする。置けない条件に当たったら何もしない。</summary>
    private void TryPaintAt(Vector2 screenPosition)
    {
        if (_spawned.Count >= maxBubbleCount) return;

        // ① まず「泡の平面」に載せた点を作る。球を外したときのフォールバック先でもある。
        Vector3 planeWorld = touchEffect.ScreenToWorldPosition(screenPosition);
        Vector3 world = planeWorld;

        // ② 許可エリアの球の表面へ載せ直す。
        //    球を外したときは ① の平面の点をそのまま使う。
        //    奥へ置きすぎて泡が消えるより、手前に浮くほうが安全なため。
        //
        //    ここで奥行きを変えても、許可／除外の判定結果は変わらない。
        //    点は「同じ視線の上」を滑るだけで、画面に映る位置が1ピクセルも動かないから。
        //    （判定は IsInsideEllipse で画面座標に直してから行っている）
        // 面の法線。球を外したときは Vector3.zero のままで、その場合は泡をカメラ正面に向ける。
        Vector3 surfaceNormal = Vector3.zero;

        if (placeOnAreaSurface && TryGetSurfacePoint(screenPosition, out Vector3 surface, out Vector3 n, out ExclusionArea hitArea))
        {
            world = surface;
            surfaceNormal = n;
            LogSurfacePlacementOnce(world, planeWorld, hitArea);
        }

        // これから置く泡の大きさを先に決める。
        // 「泡の絵がどこまで広がるか」を判定に使いたいので、Spawn より前に決めておく。
        float factor = UnityEngine.Random.Range(scaleRandomRange.x, scaleRandomRange.y);
        float bubbleRadius = _spriteRadius * _prefabScale.x * factor;

        // キャラの外（背景）に泡が付かないよう、まず許可エリアの中かを見る
        if (!IsInAllowedArea(world, bubbleRadius)) return;
        if (IsInExcludedArea(world, bubbleRadius)) return;
        if (IsTooCloseToExisting(world)) return;

        Spawn(world, surfaceNormal, factor);
    }

    /// <summary>
    /// いま選ばれているキャラに合う設定を選ぶ。
    /// 見つからなければ共通設定（Inspector 下側の Allowed Areas / Exclusion Areas）を使う。
    /// </summary>
    private void SelectAreaSet()
    {
        _activeAllowed = allowedAreas;
        _activeExclusion = exclusionAreas;
        _activeCharacterRoot = null;

        string id = ResolveCharacterId();

        if (characterAreaSets != null)
        {
            foreach (var set in characterAreaSets)
            {
                if (set == null) continue;
                if (string.IsNullOrEmpty(set.characterId)) continue;
                if (!string.Equals(set.characterId.Trim(), id, System.StringComparison.OrdinalIgnoreCase)) continue;

                // 中身が空の項目は共通設定のままにしておく（部分的にだけ差し替えられるように）
                if (set.allowedAreas   != null && set.allowedAreas.Length   > 0) _activeAllowed   = set.allowedAreas;
                if (set.exclusionAreas != null && set.exclusionAreas.Length > 0) _activeExclusion = set.exclusionAreas;
                _activeCharacterRoot = set.characterRoot;

                Debug.Log($"<color=#00E5FF>[決定]</color> [BathBubble] キャラ別の設定を使います characterId={id} 許可={_activeAllowed?.Length ?? 0}個 除外={_activeExclusion?.Length ?? 0}個");
                return;
            }
        }

        Debug.Log($"<color=#00E5FF>[決定]</color> [BathBubble] characterId={id} の専用設定が無いため共通設定を使います 許可={_activeAllowed?.Length ?? 0}個 除外={_activeExclusion?.Length ?? 0}個");
    }

    /// <summary>セーブデータから、いま選ばれているキャラのIDを取る。すべて小文字。</summary>
    private string ResolveCharacterId()
    {
        var save = SaveManager.Instance != null ? SaveManager.Instance.Data : null;
        string id = save != null ? save.selectedCharacterId : null;

        if (string.IsNullOrEmpty(id)) return "poko";   // 旧セーブや単独再生時のフォールバック
        return id.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// キャラのボーンを集める。
    ///
    /// 【なぜボーン名を使わないのか】
    ///   キャラ5体でボーンの名前も構造もバラバラ。名前で探すと1体でも変わった瞬間に壊れる。
    ///   SkinnedMeshRenderer が持っている bones 配列をそのまま使えば、
    ///   どのキャラでも同じコードで拾える。
    ///
    /// お風呂を始めるたびに集め直す。キャラは実行時に生成されるため、
    /// Awake の時点ではまだ存在しないことがあるから。
    /// </summary>
    private void CollectBones()
    {
        _bones = null;

        // 泡の追従とエリアの追従、どちらか一方でも使うならボーンは要る
        if (!attachToCharacterBone && !attachAreasToBone) return;

        var roots = new List<Transform>();

        // キャラ別セットで指定があれば、それを最優先で使う
        if (_activeCharacterRoot != null) roots.Add(_activeCharacterRoot);

        if (characterRoots != null && characterRoots.Length > 0)
        {
            foreach (var r in characterRoots) if (r != null) roots.Add(r);
        }

        // 何も指定が無ければ、泡の親の親（＝CharacterDisplayAnchor）から探す
        if (roots.Count == 0 && bubbleParent != null && bubbleParent.parent != null)
            roots.Add(bubbleParent.parent);

        var found = new List<Transform>();

        foreach (var root in roots)
        {
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.bones == null) continue;
                foreach (var bone in smr.bones)
                {
                    if (bone == null) continue;
                    if (bone.IsChildOf(bubbleParent)) continue;   // 泡自身を拾わないように
                    if (!found.Contains(bone)) found.Add(bone);
                }
            }
        }

        _bones = found.ToArray();


        if (_bones.Length == 0)
            Debug.LogWarning("[BathBubble] キャラのボーンが見つかりませんでした。泡は BubbleGroup の子のままになります（アニメに追従しません）");
        else
            Debug.Log($"<color=#00E5FF>[決定]</color> [BathBubble] ボーンを {_bones.Length} 本見つけました。泡をキャラの動きに追従させます");
    }

    // ── エリアの球をボーンに追従させる ─────────────────────────────────────────

    /// <summary>
    /// 許可エリア・除外エリアの球を、いちばん近いボーンの子に付け替える。
    ///
    /// 【なぜ必要か】
    ///   キャラは CharacterStaticDisplayController の Awake で実行時に生成され、Animator で動く。
    ///   一方でエリアの球は CharacterDisplayAnchor の子に固定で置いてある。
    ///   球の表面がそのまま「泡の乗る面」になったので、球が動かないと
    ///   アニメで体が動いた瞬間に、泡の付く面と実際の体がズレる。
    ///   球をボーンにぶら下げれば、頭の球は頭と、体の球は体と一緒に動く。
    ///
    /// 【コードの他の場所を変えなくてよい理由】
    ///   判定（IsInsideEllipse）も配置（RayEllipsoid）も Gizmo 表示も、
    ///   すべて area.center の position / rotation / lossyScale を毎回読み直している。
    ///   親が変われば、そのまま追従した値が入る。
    ///
    /// 【ボーン名は使わない】
    ///   5体でボーンの名前も構造も違うため、泡と同じく「距離がいちばん近いもの」で選ぶ。
    ///   どれに付いたかはログに出すので、おかしければ Inspector で位置を直せば選び直される。
    /// </summary>
    private void AttachAreasToBones()
    {
        if (!attachAreasToBone) return;
        if (_bones == null || _bones.Length == 0) return;

        var report = new List<string>();

        AttachAreaList(_activeAllowed   ?? allowedAreas,   report);
        AttachAreaList(_activeExclusion ?? exclusionAreas, report);

        if (report.Count > 0)
            Debug.Log($"<color=#00E5FF>[決定]</color> [BathBubble] エリアをボーンに追従させます {string.Join(" / ", report)}");
        else
            Debug.Log("[BathBubble] ボーンに付け替えるエリアがありませんでした（すでに付け替え済みか、エリアが未設定）");
    }

    private void AttachAreaList(ExclusionArea[] areas, List<string> report)
    {
        if (areas == null) return;

        foreach (var area in areas)
        {
            if (area == null || area.center == null) continue;

            Transform t = area.center;
            if (_attachedAreas.Contains(t)) continue;   // 二重に付け替えない

            Transform nearest = FindNearestBone(t.position, t);
            if (nearest == null) continue;

            // 取り消せるように、いまの親と位置を控えておく
            Transform  oldParent   = t.parent;
            Vector3    oldPos      = t.localPosition;
            Quaternion oldRot      = t.localRotation;
            Vector3    oldScale    = t.localScale;
            Vector3    beforeLossy = t.lossyScale;

            t.SetParent(nearest, true);   // 見た目（ワールドでの位置・大きさ）を保ったまま親を変える

            // 安全弁: 親替えで見た目の大きさが狂ったら取り消す。
            // 球の大きさは「判定範囲」と「泡の乗る面」の両方に直結するので、
            // 狂ったまま進めると泡が全部おかしな場所に出てしまう。
            float wanted = Mathf.Max(Mathf.Abs(beforeLossy.x), 1e-4f);
            float ratio  = Mathf.Abs(t.lossyScale.x) / wanted;

            if (ratio < 0.2f || ratio > 5f)
            {
                Debug.LogWarning($"[BathBubble] '{t.name}' をボーン '{nearest.name}' に付けたら大きさが {ratio:F2} 倍に狂ったため、追従をやめました");
                t.SetParent(oldParent, false);
                t.localPosition = oldPos;
                t.localRotation = oldRot;
                t.localScale    = oldScale;
                continue;
            }

            _attachedAreas.Add(t);
            report.Add($"{(string.IsNullOrEmpty(area.label) ? t.name : area.label)}→{nearest.name}");
        }
    }

    /// <summary>
    /// いちばん近いボーンを探す。
    /// self の子孫は親にできない（親子が循環して Unity が落ちる）ので除外する。
    /// </summary>
    private Transform FindNearestBone(Vector3 world, Transform self)
    {
        Transform nearest = null;
        float best = float.MaxValue;

        foreach (var bone in _bones)
        {
            if (bone == null) continue;
            if (self != null && bone.IsChildOf(self)) continue;

            float d = (bone.position - world).sqrMagnitude;
            if (d < best) { best = d; nearest = bone; }
        }

        return nearest;
    }

    /// <summary>
    /// 泡を、いちばん近いボーンの子に付け替える。
    ///
    /// SetParent(bone, true) は「見た目の位置を保ったまま」親を変える。
    /// ただし親のスケールが 1 でないと見た目の大きさが変わってしまうので、
    /// 親のスケールで割った値を「本来の大きさ」として渡し直している。
    /// </summary>
    private void AttachToNearestBone(BubbleController bubble, Vector3 world, float factor)
    {
        // ボーン収集は「エリアだけ追従させたい」場合にも走るので、泡側のスイッチはここで見る
        if (!attachToCharacterBone) return;
        if (_bones == null || _bones.Length == 0) return;

        Transform nearest = null;
        float best = float.MaxValue;

        foreach (var bone in _bones)
        {
            if (bone == null) continue;
            float d = (bone.position - world).sqrMagnitude;
            if (d < best) { best = d; nearest = bone; }
        }

        if (nearest == null) return;

        // 【順番が大事】
        //   SetParent(bone, true) は「いまの見た目の大きさ」を保ったまま親を変える。
        //   泡は Awake で Scale 0 にされているので、0 のまま親を変えると
        //   大きさの情報が失われ、あとで巨大な泡になってしまう（実際に画面が真っ白になった）。
        //   そこで、いったん本来の大きさに戻してから親を変え、
        //   その結果の localScale を「本来の大きさ」として覚え直す。
        Vector3 want = _prefabScale * factor;

        bubble.transform.localScale = want;          // ① 本来の大きさに戻す
        bubble.transform.SetParent(nearest, true);   // ② 見た目を保ったまま親を変える

        // 安全弁: 親替えのあと、見た目の大きさが大きく狂っていたら親替えを取り消す。
        // ボーンにおかしなスケールが入っていても、巨大な泡で画面が埋まらないようにするため。
        Vector3 after = bubble.transform.lossyScale;
        float wanted = Mathf.Max(want.x, 0.0001f);
        float ratio  = Mathf.Max(after.x, 0f) / wanted;

        if (ratio < 0.2f || ratio > 5f)
        {
            Debug.LogWarning($"[BathBubble] ボーン '{nearest.name}' に付けたら大きさが {ratio:F2} 倍に狂ったため、追従をやめました");
            bubble.transform.SetParent(bubbleParent, true);
            bubble.transform.localScale = want;
        }

        bubble.SetInitialScale(bubble.transform.localScale);  // ③ 変換後の値を覚える
        bubble.transform.localScale = Vector3.zero;  // ④ 0 に戻す。Show() でここから膨らむ
    }

    // ── 奥行きの決め方（球の表面に載せる） ───────────────────────────────────

    /// <summary>
    /// 指の画面座標から視線を飛ばし、許可エリアの楕円体の「カメラに近い側の表面」を返す。
    ///
    /// 【なぜ球の表面なのか】
    ///   泡をカメラ前の平面に置くと、奥行きが全部同じになって板のように並ぶ。
    ///   かといって平面をそのまま奥へ下げると、泡がキャラのメッシュに埋まって消える。
    ///   （泡のマテリアルは URP の Sprite-Unlit-Default で、ZTest が既定の LEqual のため、
    ///     メッシュより奥にある泡は描画されない）
    ///   実測すると、体の表面の深さは画面上の場所によって 0.94 も違った。
    ///   1枚の平面では全部の場所に合わせられないので、球の表面で奥行きを場所ごとに変える。
    ///
    /// 【必ずカメラに近い側の交点を取ること】
    ///   視線と球の交点は2つある。奥側を取ると体の裏側に置くことになり、
    ///   メッシュに隠れて泡が1つも見えなくなる。
    /// </summary>
    private bool TryGetSurfacePoint(Vector2 screenPosition, out Vector3 world, out Vector3 normal, out ExclusionArea hitArea)
    {
        world = Vector3.zero;
        normal = Vector3.zero;
        hitArea = null;

        var areas = _activeAllowed ?? allowedAreas;
        if (areas == null || areas.Length == 0) return false;

        var cam = Camera.main;
        if (cam == null) return false;

        Ray ray = cam.ScreenPointToRay(new Vector3(screenPosition.x, screenPosition.y, 0f));

        // 頭と体が重なっている場所では、いちばんカメラに近い交点が正しい表面になる
        float bestDistance = float.MaxValue;

        foreach (var area in areas)
        {
            if (area == null || area.center == null) continue;
            if (!RayEllipsoid(ray, area, out Vector3 hit, out Vector3 n)) continue;

            float d = Vector3.Distance(ray.origin, hit);
            if (d < bestDistance)
            {
                bestDistance = d;
                world = hit;
                normal = n;
                hitArea = area;
            }
        }

        if (hitArea == null) return false;

        // 表面ぴったりに置くと、球がメッシュより少しでも奥にある場所で泡が埋まって消える。
        // 視線に沿ってカメラ側へ戻して、確実に手前へ出す。
        //
        // 法線方向ではなく視線方向へ戻しているのは、輪郭付近では法線が画面と平行になり、
        // どれだけ戻しても奥行きがまったく稼げないため。
        world -= ray.direction * surfaceLift;
        return true;
    }

    /// <summary>
    /// 視線と楕円体（＝Scene ビューに描いている青い球）の交点のうち、カメラに近い側を返す。
    ///
    /// 楕円体を「半径1の球」に戻す行列を作って、そこで直線と球の交点を解いている。
    /// OnDrawGizmos とまったく同じ Matrix4x4.TRS を使うので、
    /// 「Scene ビューで見えている球の表面」＝「泡が乗る面」になる。見たままが結果になる。
    /// </summary>
    private bool RayEllipsoid(Ray ray, ExclusionArea area, out Vector3 hit, out Vector3 normal)
    {
        hit = Vector3.zero;
        normal = Vector3.zero;

        Transform t = area.center;
        Vector3 size = t.lossyScale * area.radius;

        // 潰れて厚みが無い軸があると割り算が壊れるので弾く
        if (Mathf.Abs(size.x) < 1e-5f || Mathf.Abs(size.y) < 1e-5f || Mathf.Abs(size.z) < 1e-5f) return false;

        Matrix4x4 m = Matrix4x4.TRS(t.position, t.rotation, size);
        Matrix4x4 inv = m.inverse;

        // 半径1の球の空間へ持ち込む
        Vector3 o = inv.MultiplyPoint3x4(ray.origin);
        Vector3 d = inv.MultiplyVector(ray.direction);

        float a = Vector3.Dot(d, d);
        if (a < 1e-12f) return false;

        float b = 2f * Vector3.Dot(o, d);
        float c = Vector3.Dot(o, o) - 1f;

        float disc = b * b - 4f * a * c;
        if (disc < 0f) return false;              // 球を外した

        float sqrt = Mathf.Sqrt(disc);
        float tNear = (-b - sqrt) / (2f * a);     // ★ カメラに近い側。ここを間違えると泡が全部消える
        float tFar  = (-b + sqrt) / (2f * a);

        // カメラが球の中に入っている場合は近い側が負になるので、その時だけ奥側を使う
        float tHit = tNear >= 0f ? tNear : tFar;
        if (tHit < 0f) return false;              // 交点が2つともカメラの後ろ

        Vector3 local = o + d * tHit;      // 半径1の球の上の点
        hit = m.MultiplyPoint3x4(local);

        // 楕円体の法線。半径1の球では中心からの向きがそのまま法線だが、
        // 軸ごとに伸ばした楕円体では軸の長さで割り直す必要がある。
        //   面の式 (x/a)^2+(y/b)^2+(z/c)^2 = 1 の勾配 → (x/a^2, y/b^2, z/c^2)
        //   local は既に a,b,c で割った値なので、もう一度割ればよい。
        Vector3 nLocal = new Vector3(local.x / size.x, local.y / size.y, local.z / size.z);
        normal = (t.rotation * nLocal).normalized;

        return true;
    }

    /// <summary>
    /// 球の表面に置けたことを、お風呂1回につき最初の1個だけログに出す。
    /// 泡が消えたときに「平面のままなのか、球には乗っているのか」を切り分けるため。
    /// </summary>
    private void LogSurfacePlacementOnce(Vector3 world, Vector3 planeWorld, ExclusionArea hitArea)
    {
        if (_loggedSurfacePlacement) return;
        _loggedSurfacePlacement = true;

        var cam = Camera.main;
        if (cam == null) return;

        // WorldToScreenPoint の z が、そのままカメラからの奥行きになる
        float depthSurface = cam.WorldToScreenPoint(world).z;
        float depthPlane   = cam.WorldToScreenPoint(planeWorld).z;

        Debug.Log($"<color=#00E5FF>[決定]</color> [BathBubble] 球の表面に泡を置きます " +
                  $"エリア={(string.IsNullOrEmpty(hitArea.label) ? hitArea.center.name : hitArea.label)} " +
                  $"深さ={depthSurface:F2}（平面なら {depthPlane:F2}） 手前への戻し={surfaceLift:F2}");
    }

    /// <summary>
    /// 泡の絵の半径（Scale 1 のときのワールド単位）を測る。
    /// Sprite の bounds は Pixels Per Unit を反映した実寸なので、そのまま使える。
    /// </summary>
    private float MeasureSpriteRadius()
    {
        if (bubblePrefab == null) return 0.5f;

        var sr = bubblePrefab.GetComponentInChildren<SpriteRenderer>(true);
        if (sr == null || sr.sprite == null) return 0.5f;

        Vector3 extents = sr.sprite.bounds.extents;
        return Mathf.Max(extents.x, extents.y);
    }

    /// <summary>
    /// 泡1枚の向きを決める。
    ///
    /// 【なぜ傾けるのか】
    ///   泡は1枚の板なので、全部カメラ正面に向けると、どこに置いても同じ形に見える。
    ///   奥行きを正しく変えても、見た目は平らなままになる（実際にそうなった）。
    ///   体の面に沿って傾けると、輪郭に近い泡ほど横につぶれて見えるようになり、
    ///   これが「体に巻きついている」という手がかりになる。丸みが見える主因はここ。
    ///
    /// 【なぜ全部は傾けないのか】
    ///   法線どおり（Surface Tilt = 1）にすると、輪郭の泡は真横を向いて幅が0になり消える。
    ///   カメラ正面と法線の間を Slerp で混ぜて、つぶれても見える範囲に収める。
    ///
    /// 【向きの決まり】
    ///   Unity の 2D の既定に合わせ、スプライトの forward をカメラの forward と同じ向きにする。
    ///   面に沿わせる場合は、体の中へ向かう -法線 がその役になる。
    /// </summary>
    private Quaternion ResolveBubbleRotation(Vector3 surfaceNormal)
    {
        var cam = Camera.main;
        if (cam == null) return bubblePrefab.transform.rotation;

        Quaternion camRot = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);

        // 球を外した泡（平面へのフォールバック）は面が無いので、カメラ正面のままにする
        if (surfaceTilt <= 0f || surfaceNormal.sqrMagnitude < 1e-8f) return camRot;

        Vector3 dir = -surfaceNormal.normalized;   // 体の中へ向かう向き

        // dir と上方向が重なると LookRotation が破綻するので、そのときだけ別の上方向を使う
        Vector3 upRef = cam.transform.up;
        if (Mathf.Abs(Vector3.Dot(dir, upRef)) > 0.99f) upRef = cam.transform.forward;

        Quaternion surfRot = Quaternion.LookRotation(dir, upRef);

        return Quaternion.Slerp(camRot, surfRot, surfaceTilt);
    }

    private void Spawn(Vector3 world, Vector3 surfaceNormal, float factor)
    {
        BubbleController bubble = Instantiate(bubblePrefab, bubbleParent);
        bubble.transform.position = world;
        bubble.transform.rotation = ResolveBubbleRotation(surfaceNormal);

        // 泡ごとに大きさをばらつかせる（倍率は TryPaint で決めたものを使う）。
        // Instantiate 直後は Awake が済んでいて「複製元の大きさ」を覚えているので、ここで上書きする。
        bubble.SetInitialScale(_prefabScale * factor);

        if (_colors != null && _colors.Length > 0)
            bubble.SetColor(_colors[UnityEngine.Random.Range(0, _colors.Length)]);

        // キャラのボーンにぶら下げて、アニメに追従させる
        AttachToNearestBone(bubble, world, factor);

        bubble.Show();   // ぷにっと出る

        _spawned.Add(bubble);
    }

    // ── 判定 ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// 泡を置いてよい範囲に入っているか。
    /// 許可エリアが1つも設定されていないときは「制限なし」として true を返す。
    /// </summary>
    private bool IsInAllowedArea(Vector3 world, float bubbleRadius)
    {
        var areas = _activeAllowed ?? allowedAreas;
        if (areas == null || areas.Length == 0) return true;

        var cam = Camera.main;
        if (cam == null) return true;

        foreach (var area in areas)
        {
            if (area == null || area.center == null) continue;

            // 泡が丸ごと楕円の中に収まるときだけ許可する（はみ出して背景に乗るのを防ぐ）
            if (IsInsideEllipse(cam, area, world, -bubbleRadius)) return true;
        }

        return false;
    }

    /// <summary>
    /// 目・口の範囲に入っているか。
    /// 泡の外周が楕円に触れたら「入っている」とみなす（中心だけで見ると絵が目に乗るため）。
    /// </summary>
    private bool IsInExcludedArea(Vector3 world, float bubbleRadius)
    {
        var areas = _activeExclusion ?? exclusionAreas;
        if (areas == null || areas.Length == 0) return false;

        var cam = Camera.main;
        if (cam == null) return false;

        foreach (var area in areas)
        {
            if (area == null || area.center == null) continue;

            if (IsInsideEllipse(cam, area, world, bubbleRadius)) return true;
        }

        return false;
    }

    /// <summary>
    /// 「画面から見た楕円」の中に点が入っているかを調べる。
    ///
    /// 【なぜ画面で測るのか】
    ///   お風呂のカメラは斜めから見ている。ワールド座標のまま距離を測ると、
    ///   奥行き方向の差のぶんだけ遠いと判定され、見た目と合わない。
    ///   （目L と目R が画面上はほぼ重なっているのに、ワールドでは 1.19 離れている、など）
    ///   「画面に映った形」で判定すれば、Scene ビューで見たままの結果になる。
    ///
    /// 【楕円の作り方】
    ///   中心オブジェクトの右方向・上方向を、それぞれ Scale × radius ぶん伸ばした2本を軸にする。
    ///   Scale を潰せば楕円、回せば斜めの楕円になる。Scale が 1,1,1 なら今までどおりの真円。
    ///
    /// 【margin】
    ///   泡の半径ぶん、楕円を広げたり縮めたりする量。
    ///   プラス＝広げる（除外エリア：泡の外周が触れたらアウト）
    ///   マイナス＝縮める（許可エリア：泡が丸ごと入るときだけセーフ）
    /// </summary>
    private bool IsInsideEllipse(Camera cam, ExclusionArea area, Vector3 world, float margin)
    {
        Transform t = area.center;
        Vector3 scale = t.lossyScale;

        // 楕円の2軸（ワールド）。radius を倍率として掛けるので、Scale 1 のときは半径 = radius になる
        Vector3 axisXWorld = t.right * (scale.x * area.radius);
        Vector3 axisYWorld = t.up    * (scale.y * area.radius);

        Vector3 centerScreen = cam.WorldToScreenPoint(t.position);
        Vector2 axisX = (Vector2)cam.WorldToScreenPoint(t.position + axisXWorld) - (Vector2)centerScreen;
        Vector2 axisY = (Vector2)cam.WorldToScreenPoint(t.position + axisYWorld) - (Vector2)centerScreen;

        float lenX = axisX.magnitude;
        float lenY = axisY.magnitude;
        if (lenX < 0.001f || lenY < 0.001f) return false;   // 潰れすぎて判定できない

        Vector2 point = (Vector2)cam.WorldToScreenPoint(world) - (Vector2)centerScreen;

        // point = u * axisX + v * axisY を解く（2元1次方程式）
        float det = axisX.x * axisY.y - axisX.y * axisY.x;
        if (Mathf.Abs(det) < 0.001f) return false;          // 2軸が平行＝楕円にならない

        float u = ( point.x * axisY.y - point.y * axisY.x) / det;
        float v = (-point.x * axisX.y + point.y * axisX.x) / det;

        // 泡の半径を画面ピクセルに直して、軸ごとの倍率に変換する
        float marginPixels = WorldToScreenLength(cam, world, margin);
        float scaleX = 1f + marginPixels / lenX;
        float scaleY = 1f + marginPixels / lenY;
        if (scaleX <= 0f || scaleY <= 0f) return false;     // 縮めすぎて中身が無くなった

        u /= scaleX;
        v /= scaleY;

        return (u * u + v * v) <= 1f;
    }

    /// <summary>ワールド単位の長さが、その位置で画面上の何ピクセルになるかを返す。</summary>
    private float WorldToScreenLength(Camera cam, Vector3 worldPoint, float worldLength)
    {
        if (Mathf.Approximately(worldLength, 0f)) return 0f;

        float abs = Mathf.Abs(worldLength);
        Vector3 a = cam.WorldToScreenPoint(worldPoint);
        Vector3 b = cam.WorldToScreenPoint(worldPoint + cam.transform.right * abs);
        float pixels = Vector2.Distance(a, b);

        return worldLength < 0f ? -pixels : pixels;
    }

    /// <summary>すでに置いた泡と近すぎないか。</summary>
    private bool IsTooCloseToExisting(Vector3 world)
    {
        foreach (var b in _spawned)
        {
            if (b == null) continue;
            if (Vector3.Distance(b.transform.position, world) < minBubbleDistance) return true;
        }
        return false;
    }

    // ── 流す（区切りD） ───────────────────────────────────────────────────────

    /// <summary>
    /// 座標が高い泡から順に、ふわーっと消していく。
    /// 置いた場所がばらばらでも、必ず 頭 → 体 → 足元 の順になる。
    /// </summary>
    public void StartRinse(Action onFinished = null)
    {
        StopAllCoroutines();
        StartCoroutine(RinseCoroutine(onFinished));
    }

    private IEnumerator RinseCoroutine(Action onFinished)
    {
        // null を除いてから、ワールド Y の高い順に並べ替える
        var list = _spawned.FindAll(b => b != null);
        list.Sort((a, b) => b.transform.position.y.CompareTo(a.transform.position.y));

        // 泡が1つでも 0除算にならないようにする
        float interval = list.Count > 1 ? rinseSpreadDuration / (list.Count - 1) : 0f;

        foreach (var bubble in list)
        {
            if (bubble == null) continue;
            bubble.FadeAway(bubbleFadeDuration, riseDistance);
            if (interval > 0f) yield return new WaitForSeconds(interval);
        }

        // 最後の泡が消えきるまで待つ
        yield return new WaitForSeconds(bubbleFadeDuration);

        ClearAll();

        Debug.Log($"<color=#00E5FF>[決定]</color> [BathBubble] 泡を流し終わりました 泡={list.Count}個 所要={rinseSpreadDuration + bubbleFadeDuration:F1}秒");

        onFinished?.Invoke();
    }

    /// <summary>いま置いてある泡の数。デバッグ表示やリザルトで使う想定。</summary>
    public int BubbleCount => _spawned.Count;

    // ── Scene ビューでの見え方 ────────────────────────────────────────────────

    /// <summary>
    /// 泡を置いてよい範囲を青い球、置かない範囲を黄色い球で Scene ビューに描く。
    ///
    /// ※Place On Area Surface が ON のとき、この青い球の表面がそのまま泡の乗る面になる。
    ///   RayEllipsoid() が同じ Matrix4x4.TRS を使っているので、見たままが結果になる。
    /// ※許可／除外の判定そのものは画面座標の楕円で行うため、
    ///   斜めから見たときは球の見え方と判定範囲がわずかにずれる。
    /// </summary>
    private void OnDrawGizmos()
    {
        // 判定と同じ「Transform の Scale で潰した楕円」を描く。
        // Gizmos.matrix に中心オブジェクトの位置・回転・大きさを渡すと、
        // 半径1の球がそのまま楕円として描かれる。
        Color allowFill = new Color(0.3f, 0.8f, 1f, 0.25f);
        Color allowLine = new Color(0.3f, 0.8f, 1f, 1f);
        Color denyFill  = new Color(1f, 0.9f, 0.2f, 0.35f);
        Color denyLine  = new Color(1f, 0.85f, 0f, 1f);

        DrawAreaGizmos(allowedAreas,   allowFill, allowLine);
        DrawAreaGizmos(exclusionAreas, denyFill,  denyLine);

        // キャラ別セットも全部描く。5体ぶんを見比べながら置けるようにするため
        if (characterAreaSets != null)
        {
            foreach (var set in characterAreaSets)
            {
                if (set == null) continue;
                DrawAreaGizmos(set.allowedAreas,   allowFill, allowLine);
                DrawAreaGizmos(set.exclusionAreas, denyFill,  denyLine);
            }
        }

        Gizmos.matrix = Matrix4x4.identity;
    }

    private void DrawAreaGizmos(ExclusionArea[] areas, Color fill, Color line)
    {
        if (areas == null) return;

        foreach (var area in areas)
        {
            if (area == null || area.center == null) continue;

            Transform t = area.center;
            Vector3 size = t.lossyScale * area.radius;

            Gizmos.matrix = Matrix4x4.TRS(t.position, t.rotation, size);

            Gizmos.color = fill;
            Gizmos.DrawSphere(Vector3.zero, 1f);

            Gizmos.color = line;
            Gizmos.DrawWireSphere(Vector3.zero, 1f);
        }
    }
}
