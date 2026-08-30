#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Yurufu.FoamPrototype
{
    /// <summary>
    /// 泡3.png の泡粒（Phase 2A）。
    ///
    /// 【役割分担】
    ///   泡シェル … キャラ表面に密着する泡の土台
    ///   泡粒     … 参考動画のようなモコモコした粒と輪郭（ここ）
    ///   浮遊泡   … Phase 2A では作らない
    ///
    /// 【GameObject は1個だけ】
    ///   泡1粒ごとに GameObject を作らない。ParticleSystem を1個だけ持ち、
    ///   毎フレーム SetParticles で全粒をまとめて更新する。
    ///   BubbleSprite.prefab は読み込みも Instantiate もしない。
    ///
    /// 【どうやってキャラに付着させるか】
    ///   粒を置いた瞬間に「どの Renderer の・どの三角形の・どの重心座標か」を覚える。
    ///   毎フレーム BakeMesh し直して、その三角形の現在位置から座標を計算する。
    ///   ＝ ボーンが動いても粒が表面から滑らない。実測 Bake は 8メッシュで平均 0.095ms。
    ///
    /// 【縦横比】
    ///   泡3.png は 263 x 261 px。startSize3D に 263:261 を入れて、引き伸ばさない。
    ///   Sprite Rect が画像全面（x0 y0 w263 h261）なので UV 補正は不要。
    /// </summary>
    public class FoamProtoGrains
    {
        /// <summary>泡3.png の GUID。パス変更に強いよう GUID で引く。</summary>
        public const string GrainTextureGuid = "5eb482f3688144f4a9b28e4e5a7a0a2b";
        public const string GrainShaderName  = "Yurufu/BathFoamGrainProto";

        /// <summary>泡3.png の実寸 263 x 261 px から出した縦横比。</summary>
        private const float TextureAspect = 263f / 261f;

        /// <summary>
        /// 粒を置くときの散らばり量（粒の大きさに対する割合）。
        /// 指の軌跡に一列に並ぶと不自然なので、接平面上に少しずらす。
        /// スライダーには出していない（調整項目を増やしすぎないため）。
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
        private Material               _mat;
        private Texture2D              _tex;

        private readonly List<Grain> _grains = new List<Grain>();
        private ParticleSystem.Particle[] _buf = new ParticleSystem.Particle[0];

        private static readonly int IdMainTex    = Shader.PropertyToID("_MainTex");
        private static readonly int IdTint       = Shader.PropertyToID("_Tint");
        private static readonly int IdAlphaScale = Shader.PropertyToID("_AlphaScale");

        public int  Count   => _grains.Count;
        public bool Visible
        {
            get => _psr != null && _psr.enabled;
            set { if (_psr != null) _psr.enabled = value; }
        }

        // ── 起動 ──────────────────────────────────────────────────────────────

        /// <summary>失敗したら null を返す。理由は Console に出す。</summary>
        public static FoamProtoGrains Create(int layer, FoamProtoConfig cfg)
        {
            string path = AssetDatabase.GUIDToAssetPath(GrainTextureGuid);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"[FoamProto] 泡3.png が見つかりません（GUID {GrainTextureGuid}）");
                return null;
            }

            // ★Sprite ではなく Texture2D を直接読む。
            //   泡3.png は Sprite Mode = Multiple だが、サブスプライトは1枚だけで
            //   Rect が画像全面なので、テクスチャをそのまま貼れば絵は一致する。
            //   BubbleSprite.prefab を経由しないので、Prefab を触る心配もない。
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
            {
                Debug.LogError($"[FoamProto] 泡3.png を Texture2D として読めません: {path}");
                return null;
            }

            var shader = Shader.Find(GrainShaderName);
            if (shader == null)
            {
                Debug.LogError($"[FoamProto] シェーダー '{GrainShaderName}' が見つかりません。" +
                               "Assets/Shaders/BathFoamPrototype/BathFoamGrainProto.shader を確認してください");
                return null;
            }

            var g = new FoamProtoGrains { _tex = tex };

            // マテリアルは実行時に作る。.mat アセットを増やさないので、
            // 既存の Sprite 用マテリアル（URP の Sprite-Unlit-Default）には一切触れない。
            g._mat = new Material(shader)
            {
                name = "~FoamGrainProtoMat",
                hideFlags = HideFlags.HideAndDontSave
            };
            g._mat.SetTexture(IdMainTex, tex);

            // ★hideFlags は付けない（理由は FoamProtoShell.cs と同じ）。
            //   ここに DontSave が付いていたため、お風呂を出た後も
            //   World 座標のまま泡粒が描かれ続け、Care / Main に泡が浮いていた。
            var go = new GameObject("~FoamGrains") { layer = layer };
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            go.transform.localScale = Vector3.one;

            g._ps  = go.AddComponent<ParticleSystem>();
            g._psr = go.GetComponent<ParticleSystemRenderer>();
            g.ConfigureParticleSystem();
            g.ApplyMaterial(cfg);

            Debug.Log($"<color=#00E5FF>[決定]</color> [FoamProto] 泡粒を用意しました  素材={path}  " +
                      $"{tex.width}x{tex.height}px  縦横比={TextureAspect:F4}  GameObject は ParticleSystem 1個だけ");
            return g;
        }

        private void ConfigureParticleSystem()
        {
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
            main.maxParticles      = 512;
            main.cullingMode       = ParticleSystemCullingMode.AlwaysSimulate;

            _psr.renderMode         = ParticleSystemRenderMode.Billboard;
            _psr.alignment          = ParticleSystemRenderSpace.View;
            _psr.sortMode           = ParticleSystemSortMode.Distance;
            _psr.shadowCastingMode  = ShadowCastingMode.Off;
            _psr.receiveShadows     = false;
            _psr.material           = _mat;

            _ps.Play();
        }

        // ── 粒を置く ──────────────────────────────────────────────────────────

        /// <summary>こすった1点に粒を1個置く。位置は覚えず、三角形と重心だけ覚える。</summary>
        public void Add(FoamProtoSurfacePicker.Hit hit, FoamProtoConfig cfg)
        {
            if (!hit.Valid) return;

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

            // ★上限に達したら「追加しない」。古い粒は消さない。
            //   以前は古い順に置き換えていたため、塗り広げると
            //   先に塗った所の泡が勝手に消えてしまっていた。
            int max = Mathf.Max(1, cfg.grainMaxCount);
            if (_grains.Count >= max) return;

            _grains.Add(g);
        }

        /// <summary>小・中・大を出現比率で抽選する。</summary>
        private static int PickTier(FoamProtoConfig cfg)
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

        private static float TierSize(FoamProtoConfig cfg, int tier)
        {
            if (tier == 0) return cfg.grainSizeS;
            if (tier == 2) return cfg.grainSizeL;
            return cfg.grainSizeM;
        }

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
        public void UpdateFollow(FoamProtoSurfacePicker picker, FoamProtoConfig cfg)
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

                float size  = TierSize(cfg, g.Tier) * (1f + g.SizeJitter * cfg.grainSizeJitter);
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
                // 縦横比を保つ。x に 263/261 を掛けるだけで、引き伸ばしにはならない
                p.startSize3D       = new Vector3(size * TextureAspect, size, size);
                // R = 左右反転フラグ / A = 1粒ごとの Alpha。色は _Tint 側で決める
                p.startColor        = new Color(g.Flip, 0f, 0f, alpha);
                _buf[n] = p;
                n++;
            }

            _ps.SetParticles(_buf, n);
        }

        private void ApplyMaterial(FoamProtoConfig cfg)
        {
            if (_mat == null) return;
            _mat.SetColor(IdTint, cfg.grainTint);
            _mat.SetFloat(IdAlphaScale, cfg.grainAlpha);
        }

        // ── 片付け ────────────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_ps != null)
            {
                Object.DestroyImmediate(_ps.gameObject);
                _ps = null; _psr = null;
            }
            if (_mat != null) { Object.DestroyImmediate(_mat); _mat = null; }
            _grains.Clear();
            _tex = null;   // 素材そのものは触っていないので、参照を外すだけ
        }
    }
}
#endif
