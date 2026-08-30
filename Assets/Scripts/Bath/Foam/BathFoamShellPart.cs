using UnityEngine;
using UnityEngine.Rendering;

namespace Yurufu.Bath.Foam
{
    /// <summary>
    /// 元の SkinnedMeshRenderer と同じメッシュ・同じボーンを使う「泡シェル」を作る。
    ///
    /// 【追従処理が要らない理由】
    ///   sharedMesh / bones / rootBone を共有すると、スキニングの結果が元と完全に一致する。
    ///   キャラがどんな動きをしても、泡シェルは同じ形で一緒に動く。コードで追従させる必要がない。
    ///
    /// 【元のマテリアルは使わない】
    ///   BathFoamShellProto.shader を新規に割り当てる。
    ///   マスクは MaterialPropertyBlock で Renderer ごとに渡すので、
    ///   マテリアル資産は1つを共有できる（Head と Body で別々に複製されない）。
    /// </summary>
    public class BathFoamShellPart
    {
        public SkinnedMeshRenderer Renderer { get; private set; }
        public SkinnedMeshRenderer Source   { get; private set; }

        private readonly MaterialPropertyBlock _mpb = new MaterialPropertyBlock();

        private static readonly int IdFoamMask      = Shader.PropertyToID("_FoamMask");
        private static readonly int IdFoamColor     = Shader.PropertyToID("_FoamColor");
        private static readonly int IdShellOffset   = Shader.PropertyToID("_ShellOffset");
        private static readonly int IdMaskDisplace  = Shader.PropertyToID("_MaskDisplace");
        private static readonly int IdBubbleScale   = Shader.PropertyToID("_BubbleScale");
        private static readonly int IdBubbleDepth   = Shader.PropertyToID("_BubbleDepth");
        private static readonly int IdClipThreshold = Shader.PropertyToID("_ClipThreshold");
        private static readonly int IdNoiseScale    = Shader.PropertyToID("_NoiseScale");
        private static readonly int IdNoiseStrength = Shader.PropertyToID("_NoiseStrength");
        private static readonly int IdRimStrength   = Shader.PropertyToID("_RimStrength");

        public static BathFoamShellPart Create(SkinnedMeshRenderer src, Material foamMaterial)
        {
            if (src == null || src.sharedMesh == null || foamMaterial == null) return null;

            // ★hideFlags は付けない。
            //   DontSave を付けると、シーン切り替えでも Play 停止でも破棄されず残骸になる。
            //   Play 中のシーンは保存できないので、フラグ無しで問題ない。
            var go = new GameObject("FoamShell_" + src.name);

            // 元と同じ親・同じローカル Transform に置く。
            // オブジェクト空間を元とそろえないと、シェーダー内の positionOS.x の符号がずれる。
            go.transform.SetParent(src.transform.parent, false);
            go.transform.localPosition = src.transform.localPosition;
            go.transform.localRotation = src.transform.localRotation;
            go.transform.localScale    = src.transform.localScale;
            go.layer = src.gameObject.layer;

            var shell = go.AddComponent<SkinnedMeshRenderer>();
            shell.sharedMesh        = src.sharedMesh;
            shell.bones             = src.bones;
            shell.rootBone          = src.rootBone;
            shell.localBounds       = src.localBounds;
            shell.quality           = src.quality;
            shell.updateWhenOffscreen = src.updateWhenOffscreen;
            shell.sharedMaterial    = foamMaterial;
            shell.shadowCastingMode = ShadowCastingMode.Off;
            shell.receiveShadows    = false;

            return new BathFoamShellPart { Renderer = shell, Source = src };
        }

        /// <summary>マスクとパラメータを Renderer ごとに渡す。マテリアル資産は書き換えない。</summary>
        public void Apply(RenderTexture mask, BathFoamConfig cfg)
        {
            if (Renderer == null) return;

            Renderer.GetPropertyBlock(_mpb);
            if (mask != null) _mpb.SetTexture(IdFoamMask, mask);
            _mpb.SetColor(IdFoamColor,     cfg.foamColor);
            _mpb.SetFloat(IdShellOffset,   cfg.shellOffset);
            _mpb.SetFloat(IdMaskDisplace,  cfg.maskDisplace);
            _mpb.SetFloat(IdBubbleScale,   cfg.bubbleScale);
            _mpb.SetFloat(IdBubbleDepth,   cfg.bubbleDepth);
            _mpb.SetFloat(IdClipThreshold, cfg.clipThreshold);
            _mpb.SetFloat(IdNoiseScale,    cfg.noiseScale);
            _mpb.SetFloat(IdNoiseStrength, cfg.noiseStrength);
            _mpb.SetFloat(IdRimStrength,   cfg.rimStrength);
            Renderer.SetPropertyBlock(_mpb);
        }

        public void Dispose()
        {
            if (Renderer != null)
            {
                Object.Destroy(Renderer.gameObject);
                Renderer = null;
            }
        }
    }
}
