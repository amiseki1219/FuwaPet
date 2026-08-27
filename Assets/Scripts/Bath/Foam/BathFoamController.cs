using System.Collections.Generic;
using UnityEngine;

namespace Yurufu.Bath.Foam
{
    /// <summary>
    /// お風呂の泡（本番）。Bath.unity の BathFoamSystem に1つだけ付ける。
    ///
    /// 【呼ばれ方】
    ///   BathWashManager.Initialize()      → TryBeginWash(shampooId)   ← 「おふろスタート」押下後
    ///   BathWashManager.Update()          → Paint(screenPos)
    ///   BathWashManager.OnPointerUp() ほか → EndStroke()
    ///   BathWashManager.OnWashComplete()  → OnReady()                  ← A2 で使う
    ///
    /// 【Awake / Start / OnEnable では何も作らない】
    ///   BathFoamSystem は Canvas の外にあるため、Bath シーンをロードした瞬間から有効になる。
    ///   お風呂を始める前に泡を出さないよう、生成は TryBeginWash() の中だけで行う。
    ///
    /// 【失敗したら false を返す】
    ///   結線漏れ・未対応キャラ・対象メッシュ未発見・例外のいずれでも、
    ///   何も残さずに false を返す。呼び出し側は旧方式（BathBubblePainter）へ自動で戻る。
    /// </summary>
    public class BathFoamController : MonoBehaviour
    {
        /// <summary>いまの段階。A1 で使うのは Idle / Washing の2つだけ。</summary>
        public enum Phase { Idle, Washing, Ready, Rinsing, Complete }

        /// <summary>
        /// 本番で対応しているキャラ。
        /// ★ぴよこの Head / Body は UV が左右ミラーのみ（Σ|UV面積| = 2.000）なので、
        ///   左右分割マスクで「こすった場所だけ塗る」が成立する。
        ///   ここちゃんは Head の UV が 6.8 枚ぶん重なっており（顔と耳が同じ UV）、
        ///   この方式では分離できない。無理に適用せず旧方式へ戻す。
        /// </summary>
        private const string SupportedCharacterId = "piyoko";

        /// <summary>Phase A1 の対象はこの2つだけ。Eye / Mouth / Cheek / Hair / Arm / Leg は作らない。</summary>
        private static readonly string[] TargetNames = { "Head", "Body" };

        [Header("対象")]
        [Tooltip("キャラが実行時に生成される親。Bath.unity の CharacterDisplayAnchor を入れる")]
        [SerializeField] private Transform characterAnchor;

        [Header("マテリアル（3つとも必須）")]
        [Tooltip("Shader: Yurufu/BathFoam/Shell")]
        [SerializeField] private Material shellMaterial;
        [Tooltip("Shader: Yurufu/BathFoam/Grain。絵（泡3.png）もこの Material に設定する")]
        [SerializeField] private Material grainMaterial;
        [Tooltip("Shader: Yurufu/BathFoam/MaskBrush")]
        [SerializeField] private Material brushMaterial;

        [Header("旧方式の泡")]
        [Tooltip("新方式が動く間だけ隠す。CharacterDisplayAnchor/BubbleGroup を入れる")]
        [SerializeField] private GameObject bubbleGroup;

        [Header("見た目の調整")]
        [SerializeField] private BathFoamConfig config = new BathFoamConfig();

        public BathFoamConfig Config => config;
        public Phase CurrentPhase { get; private set; } = Phase.Idle;

        private readonly List<BathFoamShellPart> _shells = new List<BathFoamShellPart>();
        private readonly List<BathFoamMask>      _masks  = new List<BathFoamMask>();
        private BathFoamSurfacePicker _picker;
        private BathFoamGrains        _grains;

        /// <summary>ブラシ Material の実行時コピー。アセット本体は書き換えない。</summary>
        private Material _runtimeBrushMat;

        /// <summary>生成済みか。TryBeginWash を何度呼んでもオブジェクトが増えないようにする目印。</summary>
        private bool _created;

        /// <summary>BubbleGroup を隠す前の状態。Dispose で元へ戻す。</summary>
        private bool _bubbleGroupWasActive;
        private bool _bubbleGroupHidden;

        /// <summary>Editor 側の試作と併用したいときに一時停止するためのフラグ（A1 では誰も呼ばない）。</summary>
        private bool _suspended;

        // ストローク状態
        private bool    _hasLast;
        private Vector2 _lastUv;
        private int     _lastTarget = -1;
        private bool    _lastUpper;
        private float   _uvSinceGrain;

        // ── 外部API ──────────────────────────────────────────────────────────

        /// <summary>
        /// お風呂の洗浄操作を始める。成功したら true。
        /// false のとき、このセッションでは新方式を使わず旧方式へ戻すこと。
        /// </summary>
        public bool TryBeginWash(string shampooId)
        {
            if (_suspended) return false;

            // すでに作ってあるなら作り直さない。中身だけ空に戻して再利用する
            if (_created)
            {
                ResetState();
                CurrentPhase = Phase.Washing;
                return true;
            }

            if (!Validate(out string reason, out SkinnedMeshRenderer head, out SkinnedMeshRenderer body))
            {
                // ★検証失敗時は何も作らない。BubbleGroup も隠さない
                Debug.LogWarning($"[BathFoam] 新方式を使いません（{reason}）。旧方式の泡で洗います");
                return false;
            }

            try
            {
                Build(head, body);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BathFoam] 初期化中に例外が出たため、作りかけを破棄して旧方式へ戻します: {e.GetType().Name} {e.Message}");
                Dispose();
                return false;
            }

            // 成功したときだけ旧方式の泡を隠す
            HideBubbleGroup();

            CurrentPhase = Phase.Washing;
            Debug.Log($"<color=#00E5FF>[決定]</color> [BathFoam] 新方式で開始しました shampooId={shampooId} " +
                      $"対象={_shells.Count}個 マスク={config.maskWidth}x{config.maskHeight} 粒の上限={config.grainMaxCount}");
            return true;
        }

        /// <summary>こすっている位置に泡を置く。BathWashManager の Update から毎フレーム呼ばれる。</summary>
        public void Paint(Vector2 screenPos)
        {
            if (!_created || _picker == null) return;
            if (CurrentPhase != Phase.Washing && CurrentPhase != Phase.Ready) return;

            var cam = Camera.main;
            if (cam == null) return;

            // 方向を正規化しておく。Moller-Trumbore が返す t がそのままワールド単位の距離になる
            Ray r = cam.ScreenPointToRay(screenPos);
            var ray = new Ray(r.origin, r.direction.normalized);

            _picker.BeginEvent();                       // Bake は各 Renderer 最大1回
            var hit = _picker.Raycast(ray);
            if (!hit.Valid) { EndStroke(); return; }

            // ★左右の振り分けは Picker が「三角形の重心 X」で確定させた値を使う。
            //   ヒット点の補間 X は正中線付近で ±0 を行き来するため使わない。
            bool upper = hit.SelectedSide == 1;

            // Renderer か side が切り替わったら、線をつながず新しいストロークにする
            bool newStroke = !_hasLast || _lastTarget != hit.TargetIndex || _lastUpper != upper;

            Vector2 from = newStroke ? hit.Uv : _lastUv;
            _masks[hit.TargetIndex].PaintSegment(from, hit.Uv, upper, config);
            _shells[hit.TargetIndex].Apply(_masks[hit.TargetIndex].Current, config);

            // ── 泡粒を置く ──
            // ストロークの最初は必ず1個。以降は UV 上を grainDensity ぶん進むごとに1個
            if (newStroke) _uvSinceGrain = config.grainDensity;
            else           _uvSinceGrain += Vector2.Distance(from, hit.Uv);

            if (_grains != null && _uvSinceGrain >= config.grainDensity)
            {
                _uvSinceGrain = 0f;
                _grains.Add(hit, config);
            }

            _hasLast    = true;
            _lastUv     = hit.Uv;
            _lastTarget = hit.TargetIndex;
            _lastUpper  = upper;
        }

        /// <summary>指を離した／範囲外へ出たとき。泡を置く線をいったん切る。</summary>
        public void EndStroke()
        {
            _hasLast    = false;
            _lastTarget = -1;
        }

        /// <summary>進行度が満タンになったとき。A1 では段階を進めるだけで、見た目は変えない。</summary>
        public void OnReady()
        {
            if (!_created) return;
            if (CurrentPhase == Phase.Washing) CurrentPhase = Phase.Ready;
        }

        /// <summary>
        /// Editor 側の試作と同時に動かないようにするための一時停止。
        /// ★A1 では誰も呼ばない。Runtime 側から Editor 側を探すことは一切しない。
        /// </summary>
        public void SetSuspended(bool suspended)
        {
            _suspended = suspended;
            if (suspended) Dispose();
        }

        // ── 検証 ──────────────────────────────────────────────────────────────

        /// <summary>生成する前に、必要なものが揃っているかを全部調べる。</summary>
        private bool Validate(out string reason, out SkinnedMeshRenderer head, out SkinnedMeshRenderer body)
        {
            head = null; body = null;

            if (characterAnchor == null) { reason = "Character Anchor が未結線です"; return false; }
            if (shellMaterial   == null) { reason = "Shell Material が未結線です";   return false; }
            if (grainMaterial   == null) { reason = "Grain Material が未結線です";   return false; }
            if (brushMaterial   == null) { reason = "Brush Material が未結線です";   return false; }

            if (characterAnchor.childCount == 0)
            {
                reason = "Character Anchor の下に実行時キャラがいません";
                return false;
            }

            string id = ResolveCharacterId();
            if (id != SupportedCharacterId)
            {
                reason = $"キャラ '{id}' は新方式に未対応です（対応は {SupportedCharacterId} のみ。UV の重なりのため）";
                return false;
            }

            var found = new SkinnedMeshRenderer[TargetNames.Length];
            for (int i = 0; i < TargetNames.Length; i++)
            {
                int count = 0;
                foreach (var smr in characterAnchor.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    // ★ボーンにも "Head" / "Body" という名前があるため、
                    //   名前一致だけでなく SkinnedMeshRenderer であることを必ず確認する
                    if (smr == null || smr.name != TargetNames[i]) continue;
                    if (smr.sharedMesh == null) continue;
                    if (found[i] == null) found[i] = smr;
                    count++;
                }

                if (found[i] == null) { reason = $"'{TargetNames[i]}' の SkinnedMeshRenderer が見つかりません"; return false; }
                if (count != 1)       { reason = $"'{TargetNames[i]}' の SkinnedMeshRenderer が {count} 個あります（1個であること）"; return false; }

                var m = found[i];
                if (m.bones == null || m.bones.Length == 0) { reason = $"'{TargetNames[i]}' に bones がありません"; return false; }
                if (m.rootBone == null)                     { reason = $"'{TargetNames[i]}' に rootBone がありません"; return false; }

                var mesh = m.sharedMesh;
                if (mesh.vertexCount == 0) { reason = $"'{TargetNames[i]}' の頂点数が 0 です"; return false; }

                // ★ここで mesh.uv / mesh.triangles / mesh.vertices を読んではいけない。
                //   FBX の Read/Write Enabled が OFF だと Unity がエラーを出して空を返し、
                //   「UV がありません」と誤判定して新方式が起動できなくなる（2026/8/27 に実際に発生）。
                //   UV と三角形は BathFoamSurfacePicker.Add() が BakeMesh のコピーから取得し、
                //   取れなければ false を返す。失敗は Build() の例外として拾う。
            }

            head = found[0];
            body = found[1];
            reason = null;
            return true;
        }

        /// <summary>
        /// いま表示されているキャラの正式な ID を返す。
        /// ★CharacterStaticDisplayController.ResolveCharacterId() と同じ手順にそろえてある
        ///   （selectedCharacterId を優先し、空なら旧 characterId、それも空なら poko）。
        ///   Prefab 名の部分一致では判定しない。
        /// </summary>
        private static string ResolveCharacterId()
        {
            SaveData data = SaveManager.Instance != null ? SaveManager.Instance.Data : null;
            if (data == null) return "poko";

            string rawId = !string.IsNullOrWhiteSpace(data.selectedCharacterId)
                ? data.selectedCharacterId
                : data.characterId;

            if (string.IsNullOrWhiteSpace(rawId)) return "poko";
            return rawId.Trim().ToLowerInvariant();
        }

        // ── 生成・後片付け ────────────────────────────────────────────────────

        private void Build(SkinnedMeshRenderer head, SkinnedMeshRenderer body)
        {
            // ★Material アセットは書き換えない。ブラシは値を毎回入れ替えるので実行時コピーを使う
            _runtimeBrushMat = new Material(brushMaterial) { name = brushMaterial.name + " (Runtime)" };

            _picker = new BathFoamSurfacePicker();

            var targets = new[] { head, body };
            foreach (var smr in targets)
            {
                if (!_picker.Add(smr)) throw new System.InvalidOperationException($"'{smr.name}' を対象に追加できませんでした");

                var mask  = new BathFoamMask(smr.name, config.maskWidth, config.maskHeight, _runtimeBrushMat);
                var shell = BathFoamShellPart.Create(smr, shellMaterial);
                if (shell == null) { mask.Dispose(); throw new System.InvalidOperationException($"'{smr.name}' の泡シェルを作れませんでした"); }

                shell.Apply(mask.Current, config);
                _masks.Add(mask);
                _shells.Add(shell);
            }

            _grains = BathFoamGrains.Create(transform, grainMaterial, head.gameObject.layer, config);
            if (_grains == null) throw new System.InvalidOperationException("泡粒を作れませんでした");

            // 土台（泡シェル）の表示は設定に従う。既定は非表示＝泡粒だけで見せる
            SetShellVisible(config.shellVisible);

            _created = true;
        }

        /// <summary>作り直さずに中身だけ空へ戻す。</summary>
        private void ResetState()
        {
            for (int i = 0; i < _masks.Count; i++)
            {
                _masks[i].Clear();
                _shells[i].Apply(_masks[i].Current, config);
            }
            _grains?.Clear();
            _uvSinceGrain = 0f;
            EndStroke();
            HideBubbleGroup();
        }

        public void SetShellVisible(bool visible)
        {
            config.shellVisible = visible;
            foreach (var s in _shells)
                if (s.Renderer != null) s.Renderer.enabled = visible;
        }

        private void HideBubbleGroup()
        {
            if (bubbleGroup == null || _bubbleGroupHidden) return;
            _bubbleGroupWasActive = bubbleGroup.activeSelf;
            bubbleGroup.SetActive(false);
            _bubbleGroupHidden = true;
        }

        private void RestoreBubbleGroup()
        {
            if (bubbleGroup == null || !_bubbleGroupHidden) return;
            bubbleGroup.SetActive(_bubbleGroupWasActive);
            _bubbleGroupHidden = false;
        }

        /// <summary>作ったものを全部片付ける。★何度呼んでも安全。</summary>
        public void Dispose()
        {
            foreach (var s in _shells) s?.Dispose();
            _shells.Clear();

            foreach (var m in _masks) m?.Dispose();
            _masks.Clear();

            _grains?.Dispose();
            _grains = null;

            _picker?.Dispose();
            _picker = null;

            if (_runtimeBrushMat != null) { Destroy(_runtimeBrushMat); _runtimeBrushMat = null; }

            RestoreBubbleGroup();

            _created      = false;
            _uvSinceGrain = 0f;
            EndStroke();
            CurrentPhase = Phase.Idle;
        }

        // ── Unity ライフサイクル ──────────────────────────────────────────────

        private void LateUpdate()
        {
            if (!_created) return;
            _grains?.UpdateFollow(_picker, config);
        }

        private void OnDisable() => Dispose();
        private void OnDestroy() => Dispose();
    }
}
