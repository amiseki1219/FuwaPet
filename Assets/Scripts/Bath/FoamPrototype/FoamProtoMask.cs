#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Yurufu.FoamPrototype
{
    /// <summary>
    /// Renderer 1つぶんの泡マスク。Ping-Pong の RenderTexture を2枚持つ。
    ///
    /// 【なぜ Ping-Pong なのか】
    ///   同じ RenderTexture を読みながら書くのは未定義動作。
    ///   前の状態を読んで、新しい状態を別の RT に書き、そのあと入れ替える。
    ///
    /// 【縦2倍にしている理由】
    ///   Head / Body は UV が左右対称で、X>0 側と X<0 側が同じ UV を使っている。
    ///   マスクを縦に2つ積んで、上半分＝X>=0 / 下半分＝X<0 と割り当てることで、
    ///   モデルを触らずに左右を分離する。
    ///   ★この割り当ては BathFoamShellProto.shader の FoamMaskUV() と一致させること。
    /// </summary>
    public class FoamProtoMask
    {
        /// <summary>上半分（v 0.5〜1.0）が object-space X >= 0 側。</summary>
        public const float UpperMin = 0.5f;
        public const float UpperMax = 1.0f;
        public const float LowerMin = 0.0f;
        public const float LowerMax = 0.5f;

        public RenderTexture Current => _read;

        private RenderTexture _read;
        private RenderTexture _write;
        private readonly Material _brush;
        private readonly string _label;

        private static readonly int IdBrushP0     = Shader.PropertyToID("_BrushP0");
        private static readonly int IdBrushP1     = Shader.PropertyToID("_BrushP1");
        private static readonly int IdBrushRadius = Shader.PropertyToID("_BrushRadius");
        private static readonly int IdBrushSoft   = Shader.PropertyToID("_BrushSoft");
        private static readonly int IdBrushStr    = Shader.PropertyToID("_BrushStr");
        private static readonly int IdBrushSide   = Shader.PropertyToID("_BrushSide");

        public FoamProtoMask(string label, int w, int h, Material brushMaterial)
        {
            _label = label;
            _brush = brushMaterial;
            _read  = Create(w, h, label + "_A");
            _write = Create(w, h, label + "_B");
            Clear();
        }

        private RenderTexture Create(int w, int h, string name)
        {
            RenderTexture rt = null;

            // R8_UNorm が使えれば 1ch で足りる（512x1024 で 0.5MB）
            if (SystemInfo.IsFormatSupported(GraphicsFormat.R8_UNorm, GraphicsFormatUsage.Render))
            {
                var desc = new RenderTextureDescriptor(w, h, GraphicsFormat.R8_UNorm, 0)
                {
                    sRGB = false,
                    msaaSamples = 1,
                    useMipMap = false,
                    autoGenerateMips = false
                };
                rt = new RenderTexture(desc);
            }

            if (rt == null || !rt.Create())
            {
                // 端末が R8 を描画先として持てない場合の保険
                if (rt != null) Object.Destroy(rt);
                rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                rt.Create();
                Debug.LogWarning($"[FoamProto] {name}: R8_UNorm が使えないため ARGB32 で作成しました");
            }

            rt.name       = "~FoamMask_" + name;
            rt.filterMode = FilterMode.Bilinear;
            rt.wrapMode   = TextureWrapMode.Clamp;
            rt.hideFlags  = HideFlags.HideAndDontSave;
            return rt;
        }

        /// <summary>マスクを真っ黒（泡なし）に戻す。</summary>
        public void Clear()
        {
            ClearOne(_read, Color.black);
            ClearOne(_write, Color.black);
        }

        /// <summary>
        /// 切り分け用: マスクを真っ白（全面が泡）にする。
        /// これで泡が出れば「シェルとシェーダーは正しく、当たり判定が原因」と確定できる。
        /// </summary>
        public void Fill()
        {
            ClearOne(_read, Color.white);
            ClearOne(_write, Color.white);
        }

        private static void ClearOne(RenderTexture rt, Color c)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(false, true, c);
            RenderTexture.active = prev;
        }

        /// <summary>
        /// 前回UVから今回UVまでを線分として塗る。
        ///
        /// ★UV は「元メッシュの UV（0〜1）」をそのまま渡す。
        ///   上下2段への詰め込みはシェーダー側が論理UVで処理するので、
        ///   ここで v を 0.5 倍したりオフセットしたりしない。
        ///   こうしないとブラシが縦方向に潰れて楕円になる。
        /// </summary>
        public void PaintSegment(Vector2 uvFrom, Vector2 uvTo, bool upperHalf, FoamProtoConfig cfg)
        {
            if (_brush == null) return;

            _brush.SetVector(IdBrushP0, new Vector4(uvFrom.x, uvFrom.y, 0, 0));
            _brush.SetVector(IdBrushP1, new Vector4(uvTo.x,   uvTo.y,   0, 0));
            _brush.SetFloat(IdBrushRadius, cfg.brushRadius);
            _brush.SetFloat(IdBrushSoft,   cfg.brushSoftness);
            _brush.SetFloat(IdBrushStr,    cfg.paintStrength);
            _brush.SetFloat(IdBrushSide,   upperHalf ? 1f : 0f);

            // 読み(_read) → 書き(_write) の一方通行。終わったら入れ替える
            Graphics.Blit(_read, _write, _brush);
            (_read, _write) = (_write, _read);
        }

        /// <summary>元メッシュUV を、512x1024 に詰め込んだあとの UV に直す（ログ確認用）。</summary>
        public static Vector2 ToPackedUv(Vector2 uv, bool upperHalf)
            => new Vector2(uv.x, uv.y * 0.5f + (upperHalf ? 0.5f : 0f));

        public void Dispose()
        {
            if (_read  != null) { _read.Release();  Object.DestroyImmediate(_read);  _read  = null; }
            if (_write != null) { _write.Release(); Object.DestroyImmediate(_write); _write = null; }
        }
    }
}
#endif
