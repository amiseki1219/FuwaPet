using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Yurufu.Bath.Foam
{
    /// <summary>
    /// キャラに付着する泡粒。ParticleSystem を1個だけ持ち、全粒をまとめて描く。
    ///
    /// 【GameObject は1個だけ】
    ///   泡1粒ごとに GameObject を作らない。毎フレーム SetParticles で全粒を更新する。
    ///
    /// 【どうやってキャラに付着させるか】
    ///   粒を置いた瞬間に「どの Renderer の・どの三角形の・どの重心座標か」を覚える。
    ///   毎フレーム BakeMesh し直して、その三角形の現在位置から座標を計算する。
    ///   ＝ ボーンが動いても粒が表面から滑らない。
    ///
    /// 【本番化で試作から変えたところ】
    ///   ・AssetDatabase を使わない。絵は Material アセットの中に入っている
    ///   ・Material アセットを書き換えないよう、実行時コピーを作って使う
    ///   ・hideFlags を付けない。Dispose() で明示的に破棄する
    /// </summary>
    public class BathFoamGrains
    {
        /// <summary>
        /// 粒を置くときの散らばり量（粒の大きさに対する割合）。
        /// 指の軌跡に一列に並ぶと不自然なので、接平面上に少しずらす。
        /// </summary>
        private const float ScatterRatio = 0.6f;

        /// <summary>泡粒1個ぶんの覚書。座標そのものは持たず、毎フレーム計算し直す。</summary>
        private struct Grain
        {
            public int     Target;      // 0=Head, 1=Body ...
            public int     Triangle;    // その Renderer の三角形番号
            public Vector3 Bary;        // 三角形の中のどこか（重心座標）
            public float   OffsetT;     // 接平面上のズレ（-1〜1・大きさに掛けて使う）
            public float   OffsetB;
            public int     Tier;        // 0=小 1=中 2=大
            public float   SizeJitter;  // -1〜1
            public float   AlphaJitter; // -1〜1
            public float   Rotation01;  // -1〜1（Rotation Range に掛ける）
            public float   Flip;        // 0 = そのまま / 1 = 左右反転
        }

        private ParticleSystem         _ps;
        private ParticleSystemRenderer _psr;

        /// <summary>Material アセットの実行時コピー。アセット本体は書き換えない。</summary>
        private Material _runtimeMat;

        /// <summary>絵の縦横比（横 ÷ 縦）。Material に入っているテクスチャの実寸から求める。</summary>
        private float _textureAspect = 1f;

        private readonly List<Grain> _grains = new List<Grain>();
        private ParticleSystem.Particle[] _buf = new ParticleSystem.Particle[0];

        private static readonly int IdTint       = Shader.PropertyToID("_Tint");
        private static readonly int IdAlphaScale = Shader.PropertyToID("_AlphaScale");

        public int  Count => _grains.Count;
        public bool Visible
        {
            get => _psr != null && _psr.enabled;
            set { if (_psr != null) _psr.enabled = value; }
        }

        // ── 生成 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 失敗したら null を返す。理由は Console に1回だけ出す。
        /// </summary>
        /// <param name="parent">BathFoamSystem の Transform。ここの子として作る</param>
        /// <param name="grainMaterialAsset">Inspector で結線された Material アセット（絵もこの中）</param>
        public static BathFoamGrains Create(Transform parent, Material grainMaterialAsset, int layer, BathFoamConfig cfg)
        {
            if (parent == null)
            {
                Debug.LogError("[BathFoam] 泡粒: 親 Transform が null です");
                return null;
            }
            if (grainMaterialAsset == null)
            {
                Debug.LogError("[BathFoam] 泡粒: Grain Material が結線されていません");
                return null;
            }

            var g = new BathFoamGrains();

            // ★Material アセットは書き換えない。実行時コピーを作って、そちらへ値を入れる。
            //   アセットへ直接 SetColor すると、Play するたび .mat ファイルに差分が出てしまう。
            g._runtimeMat = new Material(grainMaterialAsset) { name = grainMaterialAsset.name + " (Runtime)" };

            var tex = grainMaterialAsset.mainTexture;
            if (tex != null && tex.height > 0)
            {
                g._textureAspect = (float)tex.width / tex.height;
            }
            else
            {
                Debug.LogWarning("[BathFoam] 泡粒: Grain Material に絵が設定されていません。縦横比 1:1 として扱います");
            }

            var go = new GameObject("FoamGrains") { layer = layer };
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale    = Vector3.one;

            g._ps  = go.AddComponent<ParticleSystem>();
            g._psr = go.GetComponent<ParticleSystemRenderer>();
            g.ConfigureParticleSystem();
            g.ApplyMaterial(cfg);
            return g;
        }

        private void ConfigureParticleSystem()
        {
            // ★先に必ず止める。
            //   AddComponent した ParticleSystem は playOnAwake が既定 ON のため、
            //   その場で再生が始まっている。再生中に duration を書き換えると Unity が
            //     "Setting the duration while system is still playing is not supported."
            //   というエラーを出す（2026/8/27 に実際に発生）。
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // 自動放出はしない。粒は SetParticles で自分たちが並べる
            var em = _ps.emission; em.enabled = false;
            var sh = _ps.shape;    sh.enabled = false;

            var main = _ps.main;
            main.loop              = true;
            main.playOnAwake       = false;
            main.duration          = 3600f;
            main.startLifetime     = 1e9f;   // 消えないように十分長く
            main.startSpeed        = 0f;
            main.gravityModifier   = 0f;
            main.startRotation3D   = false;  // Billboard なので Z 回転だけでよい
            main.startSize3D       = true;   // 縦横比を保つために 3D サイズを使う
            main.simulationSpace   = ParticleSystemSimulationSpace.World;
            main.maxParticles      = 1024;
            main.cullingMode       = ParticleSystemCullingMode.AlwaysSimulate;

            _psr.renderMode        = ParticleSystemRenderMode.Billboard;
            _psr.alignment         = ParticleSystemRenderSpace.View;
            _psr.sortMode          = ParticleSystemSortMode.Distance;
            _psr.shadowCastingMode = ShadowCastingMode.Off;
            _psr.receiveShadows    = false;
            _psr.sharedMaterial    = _runtimeMat;

            _ps.Play();
        }

        // ── 粒を置く ──────────────────────────────────────────────────────────

        /// <summary>こすった1点に粒を1個置く。位置は覚えず、三角形と重心だけ覚える。</summary>
        public void Add(BathFoamSurfacePicker.Hit hit, BathFoamConfig cfg)
        {
            if (!hit.Valid) return;

            // ★上限に達したら「追加しない」。古い粒は消さない。
            //   古い順に置き換えると、塗り広げたときに先に塗った所の泡が消えてしまう。
            int max = Mathf.Max(1, cfg.grainMaxCount);
            if (_grains.Count >= max) return;

            var g = new Grain
            {
                Target      = hit.TargetIndex,
                Triangle    = hit.TriangleIndex,
                Bary        = hit.Barycentric,
                Tier        = PickTier(cfg),
                SizeJitter  = Random.Range(-1f, 1f),
                AlphaJitter = Random.Range(-1f, 1f),
                Rotation01  = Random.Range(-1f, 1f),
                Flip        = (cfg.grainRandomFlip && Random.value < 0.5f) ? 1f : 0f,
            };

            // 円の中に一様に散らす（ズレは大きさに比例させるので、ここでは -1〜1 で持つ）
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float rad = Mathf.Sqrt(Random.value);
            g.OffsetT = Mathf.Cos(ang) * rad;
            g.OffsetB = Mathf.Sin(ang) * rad;

            _grains.Add(g);
        }

        /// <summary>小・中・大を出現比率で抽選する。</summary>
        private static int PickTier(BathFoamConfig cfg)
        {
            float s = Mathf.Max(0f, cfg.grainWeightS);
            float m = Mathf.Max(0f, cfg.grainWeightM);
            float l = Mathf.Max(0f, cfg.grainWeightL);
            float sum = s + m + l;
            if (sum <= 0.0001f) return 1;          // 全部 0 なら中で代用

            float r = Random.value * sum;
            if (r < s) return 0;
            if (r < s + m) return 1;
            return 2;
        }

        private static float TierSize(BathFoamConfig cfg, int tier)
        {
            if (tier == 0) return cfg.grainSizeS;
            if (tier == 2) return cfg.grainSizeL;
            return cfg.grainSizeM;
        }

        /// <summary>置いてある粒を全部消す。GameObject は作り直さない。</summary>
        public void Clear()
        {
            _grains.Clear();
            if (_ps != null) _ps.SetParticles(_buf, 0);
        }

        // ── 毎フレームの追従 ──────────────────────────────────────────────────

        /// <summary>
        /// キャラの現在の姿勢に合わせて、全粒の位置を計算し直す。
        /// LateUpdate から呼ぶこと（Animator の更新後でないと1フレーム遅れる）。
        /// </summary>
        public void UpdateFollow(BathFoamSurfacePicker picker, BathFoamConfig cfg)
        {
            if (_ps == null || picker == null) return;

            ApplyMaterial(cfg);

            if (_grains.Count == 0) { _ps.SetParticles(_buf, 0); return; }
            if (_buf.Length < _grains.Count) _buf = new ParticleSystem.Particle[_grains.Count];

            picker.BakeAllForFrame();

            int n = 0;
            for (int i = 0; i < _grains.Count; i++)
            {
                var g  = _grains[i];
                var sp = picker.GetSurfacePoint(g.Target, g.Triangle, g.Bary);
                if (!sp.Valid) continue;

                float size = TierSize(cfg, g.Tier) * (1f + g.SizeJitter * cfg.grainSizeJitter);
                if (size <= 0f) continue;
                float alpha = Mathf.Clamp01(1f + g.AlphaJitter * cfg.grainAlphaJitter);

                // 接平面上に散らしてから、法線方向へ Surface Lift ぶん持ち上げる。
                // ＝ 泡シェルの少し手前に出る
                Vector3 pos = sp.Position
                            + sp.Tangent   * (g.OffsetT * size * ScatterRatio)
                            + sp.Bitangent * (g.OffsetB * size * ScatterRatio)
                            + sp.Normal    * cfg.grainLift;

                var p = _buf[n];
                p.position          = pos;
                p.velocity          = Vector3.zero;
                p.angularVelocity   = 0f;
                p.rotation          = g.Rotation01 * cfg.grainRotationRange;
                p.startLifetime     = 1e9f;
                p.remainingLifetime = 1e9f;
                // 縦横比を保つ。引き伸ばしにはならない
                p.startSize3D       = new Vector3(size * _textureAspect, size, size);
                // R = 左右反転フラグ / A = 1粒ごとの Alpha。色は _Tint 側で決める
                p.startColor        = new Color(g.Flip, 0f, 0f, alpha);
                _buf[n] = p;
                n++;
            }

            _ps.SetParticles(_buf, n);
        }

        private void ApplyMaterial(BathFoamConfig cfg)
        {
            if (_runtimeMat == null) return;
            _runtimeMat.SetColor(IdTint, cfg.grainTint);
            _runtimeMat.SetFloat(IdAlphaScale, cfg.grainAlpha);
        }

        // ── 片付け ────────────────────────────────────────────────────────────

        /// <summary>何度呼んでも安全。</summary>
        public void Dispose()
        {
            if (_ps != null)
            {
                var go = _ps.gameObject;
                _ps = null; _psr = null;
                if (go != null) Object.Destroy(go);
            }
            if (_runtimeMat != null) { Object.Destroy(_runtimeMat); _runtimeMat = null; }
            _grains.Clear();
            _buf = new ParticleSystem.Particle[0];
        }
    }
}
