#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Yurufu.FoamPrototype
{
    /// <summary>
    /// Phase 1 試作の本体。Play 中にだけ動き、Scene も Prefab も書き換えない。
    ///
    /// 確認したいのは次の4点だけ。
    ///   1. 指でこすった場所だけに泡が付く
    ///   2. Head/Body の左右対称位置へ勝手に泡が出ない
    ///   3. アニメ中も泡が表面から滑らない
    ///   4. 球・除外エリア・大量の泡 GameObject を使わない
    /// </summary>
    public class FoamProtoController : MonoBehaviour
    {
        /// <summary>
        /// いま動いている試作。
        ///
        /// ★FindFirstObjectByType は HideFlags.HideAndDontSave が付いたオブジェクトを返さない。
        ///   本体を HideAndDontSave で作っているため、メニュー側が自分の作った本体を
        ///   見つけられず「有効になっていません」と出ていた。静的な参照で確実に辿れるようにする。
        /// </summary>
        public static FoamProtoController Instance { get; private set; }

        public const string ShellShaderName = "Yurufu/BathFoamShellProto";
        public const string BrushShaderName = "Yurufu/BathFoamMaskBrush";

        /// <summary>Phase 1 の対象はこの2つだけ。Eye/Mouth/Cheek/Hair/Arm/Leg は作らない。</summary>
        private static readonly string[] TargetNames = { "Head", "Body" };

        public FoamProtoConfig Config = new FoamProtoConfig();
        /// <summary>
        /// デバッグパネルの表示。★初期は OFF。
        /// OnGUI / GUILayout は毎フレーム大量に確保するため、
        /// 出しっぱなしだと塗り処理の GC 計測に混ざってしまう。
        /// メニューの「デバッグ表示 ON / OFF」で切り替えられる。
        /// </summary>
        public bool ShowDebugGui = true;   // Phase 2 は見た目調整なので初期 ON

        private readonly List<FoamProtoShell> _shells = new List<FoamProtoShell>();
        private readonly List<FoamProtoMask>  _masks  = new List<FoamProtoMask>();
        private FoamProtoSurfacePicker _picker;

        /// <summary>泡粒（泡3.png）。ParticleSystem 1個で全粒を描く。</summary>
        private FoamProtoGrains _grains;
        /// <summary>前回粒を置いてからの UV 上の移動距離。Bubble Density の判定に使う。</summary>
        private float _uvSinceGrain;

        /// <summary>デバッグパネルのスクロール位置。項目が増えて画面からはみ出さないようにする。</summary>
        private Vector2 _scroll;

        /// <summary>自動終了の処理中か。二重に Destroy しないための目印。</summary>
        private bool _terminating;

        private Material _shellMat;
        private Material _brushMat;

        private FoamProtoInput _input;
        private RectTransform  _scrubArea;
        private Canvas         _canvas;
        private Camera         _uiCamera;

        private GameObject _bubbleGroup;
        private bool       _bubbleGroupWasActive;

        // ストローク状態
        private bool    _hasLast;
        private Vector2 _lastUv;
        private int     _lastTarget = -1;
        private bool    _lastUpper;

        // 計測
        private int    _diagLeft = 24;  // 最初の数回だけ詳しいログを出す（横ドラッグを見たいので多め）
        private int    _paintCount;
        private double _sumBakeMs, _sumRayMs, _maxBakeMs, _maxRayMs;
        private long   _gcPaintBytes;
        private string _status = "";

        // ── 寿命の管理 ────────────────────────────────────────────────────────

        /// <summary>
        /// ★HideFlags.DontSave が付いたオブジェクトは、シーンを切り替えても破棄されない。
        ///   そのため、お風呂 → お世話 と移動すると
        ///     ・泡粒（ParticleSystem）が Care 画面に置き去りで浮いたまま残る
        ///     ・キャラが消えた後も BakeMesh を呼び続けて MissingReferenceException が出る
        ///     ・次にお風呂へ入ったとき、洗う前から泡が付いて見える
        ///   という3つが同時に起きる。シーンが変わったら自分から終了する。
        ///
        ///   ※これはシャワー（泡を流す処理）とは無関係の不具合。
        ///     シャワーを実装しても直らないので、ここで直す。
        /// </summary>
        private void OnEnable()
        {
            SceneManager.sceneUnloaded     += OnSceneUnloaded;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void OnDisable()
        {
            SceneManager.sceneUnloaded     -= OnSceneUnloaded;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }

        private void OnSceneUnloaded(Scene s)                 => SelfTerminate($"シーン '{s.name}' が閉じられました");
        private void OnActiveSceneChanged(Scene from, Scene to) => SelfTerminate($"シーンが '{from.name}' → '{to.name}' に変わりました");

        /// <summary>試作を自分から終了する。Destroy → OnDestroy で全部片付く。</summary>
        private void SelfTerminate(string reason)
        {
            if (_terminating) return;
            _terminating = true;
            Debug.Log($"<color=#00E5FF>[決定]</color> [FoamProto] 試作を自動終了します（{reason}）\n" +
                      "      もう一度使うときは、お風呂シーンで「試作を有効にする（Play中）」を押してください");
            Destroy(gameObject);
        }

        // ── 起動 ──────────────────────────────────────────────────────────────

        public bool Setup()
        {
            Instance = this;

            var shellShader = Shader.Find(ShellShaderName);
            var brushShader = Shader.Find(BrushShaderName);
            if (shellShader == null || brushShader == null)
            {
                Debug.LogError($"[FoamProto] シェーダーが見つかりません。shell={(shellShader != null)} brush={(brushShader != null)}\n" +
                               "Assets/Shaders/BathFoamPrototype/ の2つが正しくインポートされているか確認してください");
                return false;
            }

            _shellMat = new Material(shellShader) { name = "~FoamShellProtoMat", hideFlags = HideFlags.HideAndDontSave };
            _brushMat = new Material(brushShader) { name = "~FoamBrushProtoMat", hideFlags = HideFlags.HideAndDontSave };

            // ── 対象 Renderer を探す ──
            var anchor = GameObject.Find("CharacterDisplayAnchor");
            if (anchor == null)
            {
                Debug.LogError("[FoamProto] CharacterDisplayAnchor が見つかりません。Bath シーンを Play してから実行してください");
                return false;
            }

            var found = new List<SkinnedMeshRenderer>();
            foreach (var want in TargetNames)
            {
                SkinnedMeshRenderer hit = null;
                foreach (var smr in anchor.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    // ★ボーンにも "Head" / "Body" という名前があるため、
                    //   名前一致だけでなく SkinnedMeshRenderer であることを必ず確認する
                    if (smr.name != want) continue;
                    if (smr.sharedMesh == null) continue;
                    hit = smr;
                    break;
                }

                if (hit == null)
                {
                    Debug.LogError($"[FoamProto] '{want}' の SkinnedMeshRenderer が見つかりません");
                    continue;
                }

                Debug.Log($"<color=#00E5FF>[FoamProto]</color> 対象: {Path(hit.transform)}  頂点={hit.sharedMesh.vertexCount}");
                found.Add(hit);
            }

            if (found.Count == 0) return false;

            // ── マスク・シェル・ピッカーを用意 ──
            _picker = new FoamProtoSurfacePicker();
            foreach (var smr in found)
            {
                if (!_picker.Add(smr)) continue;

                var mask  = new FoamProtoMask(smr.name, Config.maskWidth, Config.maskHeight, _brushMat);
                var shell = FoamProtoShell.Create(smr, _shellMat);
                if (shell == null) { mask.Dispose(); continue; }

                shell.Apply(mask.Current, Config);
                _masks.Add(mask);
                _shells.Add(shell);
            }

            if (_shells.Count == 0)
            {
                Debug.LogError("[FoamProto] 泡シェルを1つも作れませんでした");
                return false;
            }

            // ── 入力を WashPanel に足す ──
            var wash = GameObject.Find("WashPanel");
            if (wash == null)
            {
                Debug.LogError("[FoamProto] WashPanel が見つかりません");
                return false;
            }

            _scrubArea = FindChildRect(wash.transform, "ScrubArea");
            if (_scrubArea == null)
                Debug.LogWarning("[FoamProto] ScrubArea が見つかりません。範囲判定なしで動かします");

            _canvas = wash.GetComponentInParent<Canvas>();
            _uiCamera = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? _canvas.worldCamera : null;   // Overlay のときは必ず null を渡す

            _input = wash.AddComponent<FoamProtoInput>();
            _input.OnDown = OnDown;
            _input.OnMove = OnMove;
            _input.OnUp   = OnUp;

            // ── 既存の泡を一時的に隠す（見た目が混ざらないように）──
            _bubbleGroup = GameObject.Find("BubbleGroup");
            if (_bubbleGroup != null)
            {
                _bubbleGroupWasActive = _bubbleGroup.activeSelf;
                _bubbleGroup.SetActive(false);
                Debug.Log("[FoamProto] 既存の BubbleGroup を一時的に非表示にしました（試作終了時に元へ戻します）");
            }

            // ── 泡粒（泡3.png）を用意する ──
            // 失敗しても泡シェルだけで動かせるよう、ここでは中断しない
            _grains = FoamProtoGrains.Create(found[0].gameObject.layer, Config);

            _picker.LogUvCompatibility();

            // 土台（泡シェル）の表示は設定に従う。既定は非表示（＝泡粒だけ）
            SetDisplay(Config.shellVisible, true);

            _status = $"対象 {_shells.Count} 個 / マスク {Config.maskWidth}x{Config.maskHeight}";
            Debug.Log($"<color=#00E5FF>[決定]</color> [FoamProto] 試作を開始しました  {_status}");
            return true;
        }

        // ── 入力 ──────────────────────────────────────────────────────────────

        private void OnDown(Vector2 screenPos)
        {
            EndStroke();
            PaintMeasured(screenPos);
        }

        private void OnMove(Vector2 screenPos) => PaintMeasured(screenPos);

        private void OnUp() => EndStroke();

        /// <summary>
        /// 塗り処理だけを挟んで GC 増分を測る。
        /// デバッグ用の OnGUI(GUILayout) は毎フレーム大量に確保するため、
        /// ドラッグ全体で測ると塗り処理の実力が分からない。
        /// </summary>
        private void PaintMeasured(Vector2 screenPos)
        {
            long before = System.GC.GetTotalMemory(false);
            Paint(screenPos);
            long d = System.GC.GetTotalMemory(false) - before;
            if (d > 0) _gcPaintBytes += d;
        }

        private void EndStroke()
        {
            _hasLast = false;
            _lastTarget = -1;
        }

        private void Paint(Vector2 screenPos)
        {
            if (_picker == null) return;
            if (_scrubArea != null &&
                !RectTransformUtility.RectangleContainsScreenPoint(_scrubArea, screenPos, _uiCamera))
            {
                EndStroke();      // 範囲外へ出たら線を切る
                return;
            }

            var cam = Camera.main;
            if (cam == null) return;

            // 方向を正規化しておく。Moller-Trumbore が返す t がそのままワールド単位の距離になる
            Ray r = cam.ScreenPointToRay(screenPos);
            var ray = new Ray(r.origin, r.direction.normalized);

            _picker.BeginEvent();                       // Bake は各 Renderer 最大1回
            var hit = _picker.Raycast(ray);

            _sumBakeMs += _picker.LastBakeMs;
            _sumRayMs  += _picker.LastRaycastMs;
            if (_picker.LastBakeMs    > _maxBakeMs) _maxBakeMs = _picker.LastBakeMs;
            if (_picker.LastRaycastMs > _maxRayMs)  _maxRayMs  = _picker.LastRaycastMs;

            // 切り分け用: 最初の数回だけ、当たったか外れたかを必ずログに出す
            if (_diagLeft > 0)
            {
                _diagLeft--;
                Debug.Log($"<color=#00E5FF>[FoamProto]</color> 判定 screen={screenPos} hit={hit.Valid}" +
                          (hit.Valid
                            ? $" target={_picker.TargetName(hit.TargetIndex)}" +
                              $" tri={hit.TriangleIndex}" +
                              $" hitLocalX={hit.LocalPos.x:F6}" +
                              $" triCenterX={hit.TriangleCenterX:F6}" +
                              $" side={hit.SelectedSide}({(hit.SelectedSide == 1 ? "上/X>=0" : "下/X<0")})" +
                              $" origUV=({hit.Uv.x:F4}, {hit.Uv.y:F4})" +
                              $" packedUV={FoamProtoMask.ToPackedUv(hit.Uv, hit.SelectedSide == 1)}" +
                              $" 距離={hit.Distance:F3}"
                            : "  ← レイがキャラに当たっていません") +
                          $"  bake={_picker.LastBakeMs:F3}ms ray={_picker.LastRaycastMs:F3}ms");
            }

            if (!hit.Valid) { EndStroke(); return; }    // 当たらなければ描かない

            // ★左右の振り分けは Picker が「三角形の重心 X」で確定させた値を使う。
            //   ヒット点の補間 X は正中線付近で ±0 を行き来するため使わない。
            bool upper = hit.SelectedSide == 1;

            // Renderer か side が切り替わったら、線をつながず新しいストロークにする
            bool newStroke = !_hasLast || _lastTarget != hit.TargetIndex || _lastUpper != upper;

            Vector2 from = newStroke ? hit.Uv : _lastUv;
            _masks[hit.TargetIndex].PaintSegment(from, hit.Uv, upper, Config);
            _shells[hit.TargetIndex].Apply(_masks[hit.TargetIndex].Current, Config);

            _hasLast    = true;
            _lastUv     = hit.Uv;
            _lastTarget = hit.TargetIndex;
            _lastUpper  = upper;
            _paintCount++;

            // ── 泡粒（泡3.png）を置く ──
            // ストロークの最初は必ず1個。以降は UV 上を grainDensity ぶん進むごとに1個。
            if (newStroke) _uvSinceGrain = Config.grainDensity;
            else           _uvSinceGrain += Vector2.Distance(from, hit.Uv);

            if (_grains != null && _uvSinceGrain >= Config.grainDensity)
            {
                _uvSinceGrain = 0f;
                _grains.Add(hit, Config);
            }
        }

        // ── 毎フレームの追従 ──────────────────────────────────────────────────

        /// <summary>
        /// 泡粒をキャラの今の姿勢へ追従させる。
        /// ★Update ではなく LateUpdate。Animator がボーンを動かした後でないと、
        ///   粒の位置が1フレームぶん遅れて、体から浮いて見える。
        /// </summary>
        private void LateUpdate()
        {
            if (_terminating) return;

            // 保険: シーンのイベントを取りこぼしても、対象が消えていれば終了する
            if (_picker != null && _picker.AnyTargetLost())
            {
                SelfTerminate("キャラクターが破棄されました");
                return;
            }

            _grains?.UpdateFollow(_picker, Config);
        }

        // ── 操作 ──────────────────────────────────────────────────────────────

        public void ClearMasks()
        {
            for (int i = 0; i < _masks.Count; i++)
            {
                _masks[i].Clear();
                _shells[i].Apply(_masks[i].Current, Config);
            }
            _grains?.Clear();
            _uvSinceGrain = 0f;
            EndStroke();
            Debug.Log("[FoamProto] マスクと泡粒を全消去し、見た目の設定を通常に戻しました");
        }

        public void ToggleExistingBubbles()
        {
            if (_bubbleGroup == null) return;
            _bubbleGroup.SetActive(!_bubbleGroup.activeSelf);
            Debug.Log($"[FoamProto] 既存の BubbleGroup: {(_bubbleGroup.activeSelf ? "表示" : "非表示")}");
        }

        /// <summary>
        /// 切り分け①: マスクを全面白にする。
        ///   泡が出る   → シェル・シェーダー・MPB は正常。原因は「当たり判定」
        ///   泡が出ない → シェルかシェーダーが原因
        /// </summary>
        public void TestFillMask()
        {
            for (int i = 0; i < _masks.Count; i++)
            {
                _masks[i].Fill();
                _shells[i].Apply(_masks[i].Current, Config);
            }
            Debug.Log($"<color=#00E5FF>[FoamProto]</color> マスクを全面白にしました。" +
                      "ここで泡が見えなければ、シェル側（シェーダー/複製）が原因です");
        }

        /// <summary>
        /// 切り分け②: 画面を格子状にレイで走査して、何回当たるかを数える。
        /// 0 なら Bake の座標空間かカメラが原因。
        /// </summary>
        public void SelfTestRays()
        {
            var cam = Camera.main;
            if (cam == null) { Debug.LogError("[FoamProto] Camera.main が null です"); return; }

            Debug.Log($"<color=#00E5FF>[FoamProto]</color> --- 自己診断 ---\n" +
                      $"  Camera.main = {cam.name}  画面 = {Screen.width}x{Screen.height}\n" +
                      $"  ScrubArea = {(_scrubArea != null ? _scrubArea.name : "なし")}  uiCamera = {(_uiCamera != null ? _uiCamera.name : "null(Overlay)")}\n" +
                      _picker.DebugReport());

            int hits = 0, total = 0;
            var perTarget = new int[_picker.TargetCount];
            for (int y = 0; y < 24; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    var sp = new Vector2((x + 0.5f) / 16f * Screen.width, (y + 0.5f) / 24f * Screen.height);
                    Ray sr = cam.ScreenPointToRay(sp);
                    _picker.BeginEvent();
                    var h = _picker.Raycast(new Ray(sr.origin, sr.direction.normalized));
                    total++;
                    if (h.Valid) { hits++; perTarget[h.TargetIndex]++; }
                }
            }

            var sb = new System.Text.StringBuilder();
            sb.Append($"  格子 {total} 点のうち {hits} 点がキャラに当たりました");
            for (int i = 0; i < perTarget.Length; i++) sb.Append($"  {_picker.TargetName(i)}={perTarget[i]}");
            if (hits == 0) sb.Append("\n  ★0 です。Bake のワールド変換かカメラが原因です（上のズレの値を確認）");
            Debug.Log($"<color=#00E5FF>[FoamProto]</color> {sb}");
        }

        /// <summary>
        /// 切り分け③: 指定した側「だけ」を全部塗る。
        ///
        /// 【1点ではなく全面にした理由】
        ///   キャラごとに UV の作りが違うため、UV(0.5,0.5) が体のどこかは保証できない。
        ///   さらに1点だけだとノイズに食われて消える（マスク0.5 − ノイズ0.28 < しきい値0.25）。
        ///   「片側を丸ごと塗る」なら UV の作りに関係なく判定できる。
        ///
        ///   ・体の片側にだけ泡が出る → 左右分離は正常
        ///   ・全身に出る             → 左右分離が効いていない
        ///   ・何も出ない             → マスクかシェルの別の問題
        /// </summary>
        public void TestPaintHalf(bool upperHalf)
        {
            if (_masks.Count == 0) return;

            // テスト中はノイズを切り、しきい値も下げて「確実に見える」状態にする
            var t = Config.Clone();
            t.brushRadius   = 10f;    // UV 全体を覆う大きさ
            t.brushSoftness = 0f;
            t.paintStrength = 1f;
            t.noiseStrength = 0f;
            t.clipThreshold = 0.05f;

            var center = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < _masks.Count; i++)
            {
                _masks[i].PaintSegment(center, center, upperHalf, t);
                _shells[i].Apply(_masks[i].Current, t);   // シェル側もテスト用の見え方にする
            }

            Debug.Log($"<color=#00E5FF>[FoamProto]</color> 切り分け3: side={(upperHalf ? "1 上(X>=0)" : "0 下(X<0)")} の半分を全部塗りました" +
                      "（ノイズOFF・しきい値0.05）\n" +
                      "      体の片側にだけ泡が出れば左右分離は正常 / 全身に出れば分離できていません");
        }

        /// <summary>
        /// 比較画像用の表示切り替え。
        ///   ① 泡シェルだけ / ② 泡粒だけ / ③ 両方
        /// </summary>
        public void SetDisplay(bool shell, bool grain)
        {
            Config.shellVisible = shell;

            foreach (var s in _shells)
                if (s.Renderer != null) s.Renderer.enabled = shell;

            if (_grains != null) _grains.Visible = grain;

            Debug.Log($"<color=#00E5FF>[FoamProto]</color> 表示: 泡シェル={(shell ? "ON" : "OFF")} / 泡粒={(grain ? "ON" : "OFF")}");
        }

        public void ApplyConfigToShells()
        {
            for (int i = 0; i < _shells.Count; i++) _shells[i].Apply(_masks[i].Current, Config);
        }

        // ── 片付け ────────────────────────────────────────────────────────────

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (_input != null) Destroy(_input);

            _grains?.Dispose();
            _grains = null;

            foreach (var s in _shells) s.Dispose();
            foreach (var m in _masks)  m.Dispose();
            _shells.Clear(); _masks.Clear();

            _picker?.Dispose();

            if (_shellMat != null) DestroyImmediate(_shellMat);
            if (_brushMat != null) DestroyImmediate(_brushMat);

            if (_bubbleGroup != null) _bubbleGroup.SetActive(_bubbleGroupWasActive);

            Debug.Log("[FoamProto] 試作を終了し、元の状態へ戻しました");
        }

        // ── 補助 ──────────────────────────────────────────────────────────────

        /// <summary>ラベル付きスライダー。値が変わったら true。</summary>
        private static bool Slider(string label, ref float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label} {value:F3}", GUILayout.Width(150));
            float v = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(150));
            GUILayout.EndHorizontal();
            if (Mathf.Approximately(v, value)) return false;
            value = v;
            return true;
        }

        /// <summary>整数用のスライダー。個数のように 1 刻みで動かしたいもの向け。</summary>
        private static void SliderInt(string label, ref int value, int min, int max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label} {value}", GUILayout.Width(150));
            int v = Mathf.RoundToInt(GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(150)));
            GUILayout.EndHorizontal();
            value = v;
        }

        /// <summary>ON / OFF の切り替え。</summary>
        private static void Toggle(string label, ref bool value)
        {
            GUILayout.BeginHorizontal();
            value = GUILayout.Toggle(value, $" {label}");
            GUILayout.EndHorizontal();
        }

        private static RectTransform FindChildRect(Transform root, string name)
        {
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == name) return rt;
            return null;
        }

        private static string Path(Transform t)
        {
            var st = new Stack<string>();
            while (t != null) { st.Push(t.name); t = t.parent; }
            return string.Join("/", st.ToArray());
        }

        private void OnGUI()
        {
            if (!ShowDebugGui) return;

            // Simulator は 1206x2622 などの高解像度なので、等倍だと文字が読めない
            float scale = Mathf.Max(1f, Screen.height / 900f);
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);

            // 項目が増えたので、画面に収まらないぶんはスクロールで見られるようにする
            const float W = 344f;
            float H = Mathf.Max(200f, Screen.height / scale - 16f);
            GUILayout.BeginArea(new Rect(8, 8, W, H), GUI.skin.box);
            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.Label("<b>泡シェル試作 Phase 1</b>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label(_status);
            GUILayout.Label(_paintCount == 0
                ? "こすってください"
                : $"塗り {_paintCount} 回 / Bake 平均 {_sumBakeMs / _paintCount:F3}ms 最大 {_maxBakeMs:F3}ms");
            if (_paintCount > 0)
                GUILayout.Label($"レイ 平均 {_sumRayMs / _paintCount:F3}ms 最大 {_maxRayMs:F3}ms");
            GUILayout.Label(_paintCount == 0
                ? "塗り処理の GC: -"
                : $"塗り処理の GC 合計 {_gcPaintBytes / 1024.0:F1} KB（1回あたり {_gcPaintBytes / 1024.0 / _paintCount:F2} KB）");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("マスク全消去")) ClearMasks();
            if (GUILayout.Button("既存泡 表示切替")) ToggleExistingBubbles();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("切分1 マスク全面白")) TestFillMask();
            if (GUILayout.Button("切分2 レイ自己診断")) SelfTestRays();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("切分3 上(X>=0)を全塗り")) TestPaintHalf(true);
            if (GUILayout.Button("切分3 下(X<0)を全塗り"))  TestPaintHalf(false);
            GUILayout.EndHorizontal();

            // ── 見た目の調整（Play 中にそのまま反映される）──
            GUILayout.Space(6);
            GUILayout.Label("<b>見た目</b>", new GUIStyle(GUI.skin.label) { richText = true });

            bool changed = false;
            changed |= Slider("土台の膨らみ",   ref Config.shellOffset,   0f, 0.5f);
            changed |= Slider("塗った所の厚み", ref Config.maskDisplace,  0f, 0.5f);
            changed |= Slider("粒の細かさ",     ref Config.bubbleScale,   5f, 150f);
            changed |= Slider("粒の凹凸",       ref Config.bubbleDepth,   0f, 1f);
            changed |= Slider("表示しきい値",   ref Config.clipThreshold, 0.01f, 0.99f);
            changed |= Slider("輪郭ノイズ",     ref Config.noiseStrength, 0f, 1f);
            GUILayout.Label("<b>ブラシ</b>", new GUIStyle(GUI.skin.label) { richText = true });
            Slider("ブラシ半径", ref Config.brushRadius,   0.005f, 0.25f);
            Slider("塗る強さ",   ref Config.paintStrength, 0.02f, 1f);

            if (changed) ApplyConfigToShells();

            // ── 泡粒（泡3.png）──
            // ここのスライダーは毎フレーム読み直しているので、Apply の呼び直しは要らない
            GUILayout.Space(6);
            GUILayout.Label($"<b>泡粒（泡3.png）</b>  いま {(_grains != null ? _grains.Count : 0)} 個",
                            new GUIStyle(GUI.skin.label) { richText = true });

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("①シェルのみ")) SetDisplay(true, false);
            if (GUILayout.Button("②粒のみ"))     SetDisplay(false, true);
            if (GUILayout.Button("③両方"))       SetDisplay(true, true);
            GUILayout.EndHorizontal();

            Slider("Bubble Density", ref Config.grainDensity, 0.002f, 0.15f);
            SliderInt("最大個数",     ref Config.grainMaxCount, 1, 800);
            Slider("小サイズ",       ref Config.grainSizeS, 0.01f, 1.5f);
            Slider("中サイズ",       ref Config.grainSizeM, 0.01f, 1.5f);
            Slider("大サイズ",       ref Config.grainSizeL, 0.01f, 1.5f);
            Slider("比率 小",        ref Config.grainWeightS, 0f, 10f);
            Slider("比率 中",        ref Config.grainWeightM, 0f, 10f);
            Slider("比率 大",        ref Config.grainWeightL, 0f, 10f);
            Slider("サイズのゆらぎ",  ref Config.grainSizeJitter, 0f, 0.6f);
            Slider("Surface Lift",   ref Config.grainLift, 0f, 0.6f);
            Slider("Alpha",          ref Config.grainAlpha, 0f, 2f);
            Slider("Alphaのゆらぎ",   ref Config.grainAlphaJitter, 0f, 0.8f);
            Slider("Rotation Range", ref Config.grainRotationRange, 0f, 180f);
            Toggle("Random Flip（左右反転）", ref Config.grainRandomFlip);

            GUILayout.Space(4);
            bool shellOn = Config.shellVisible;
            Toggle("土台（泡シェル）を表示", ref shellOn);
            if (shellOn != Config.shellVisible) SetDisplay(shellOn, _grains == null || _grains.Visible);

            GUILayout.Space(6);
            if (GUILayout.Button("試作を終了")) Destroy(gameObject);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.matrix = oldMatrix;
        }
    }
}
#endif
