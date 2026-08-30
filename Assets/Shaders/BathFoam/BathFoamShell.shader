// 泡シェル シェーダー（本番 / Phase A1）
//
// Assets/Shaders/BathFoamPrototype/BathFoamShellProto.shader の検証済みコードをそのまま複製し、
// Shader 名だけ本番用に変更したもの。座標変換・左右分割・logical UV・境界処理は1文字も変えていない。
// 本番コードは試作フォルダを参照しない。試作側は比較用にそのまま残す。
//
// 【左右ミラーUVの回避】※Phase 1 で確定済み。変更しないこと
//   object-space X >= 0 → マスクの上半分 (v 0.5〜1.0)
//   object-space X <  0 → マスクの下半分 (v 0.0〜0.5)
//   ★side は fragment ごとに決める。0/1 に量子化した値を頂点から補間しない。
//
// 【頂点での膨らみ】
//   Phase 1 では「頂点でマスクを読むと X=0 のシーム頂点が反対側を読む」ため止めていた。
//   Phase 2 では上下【両方】を読んで max を取る。これなら反対側を読んでも
//   厚みがわずかに増えるだけで、間違った場所に泡が出ることはない。
//   fragment 側の表示判定は従来どおり正しい side だけを使う。
//
// 【泡の粒】
//   sin の積で周期的な凹凸を作り、その解析的な勾配で法線をずらす。
//   ノイズテクスチャも導関数も要らないので Metal / iOS で軽い。
//   規則正しくなりすぎないよう、値ノイズで少し崩す。
Shader "Yurufu/BathFoam/Shell"
{
    Properties
    {
        [NoScaleOffset] _FoamMask ("泡マスク (R)", 2D) = "black" {}
        _FoamColor      ("泡の色",               Color)             = (1, 0.955, 0.965, 1)
        _ShellOffset    ("土台の膨らみ",           Range(0, 0.5))     = 0.02
        _MaskDisplace   ("塗った所の追加の厚み",    Range(0, 0.5))     = 0.06
        _ClipThreshold  ("表示しきい値",           Range(0.01, 0.99)) = 0.25
        _NoiseScale     ("輪郭ノイズの細かさ",      Float)             = 24
        _NoiseStrength  ("輪郭ノイズの強さ",        Range(0, 1))       = 0.35
        _BubbleScale    ("粒の細かさ",             Float)             = 40
        _BubbleDepth    ("粒の凹凸の強さ",          Range(0, 1))       = 0.45
        _RimStrength    ("縁の明るさ",             Range(0, 2))       = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "TransparentCutout"
            "Queue"          = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "FoamShellForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_FoamMask);
            SAMPLER(sampler_FoamMask);
            float4 _FoamMask_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _FoamColor;
                float  _ShellOffset;
                float  _MaskDisplace;
                float  _ClipThreshold;
                float  _NoiseScale;
                float  _NoiseStrength;
                float  _BubbleScale;
                float  _BubbleDepth;
                float  _RimStrength;
            CBUFFER_END

            float Hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = Hash13(i + float3(0,0,0));
                float n100 = Hash13(i + float3(1,0,0));
                float n010 = Hash13(i + float3(0,1,0));
                float n110 = Hash13(i + float3(1,1,0));
                float n001 = Hash13(i + float3(0,0,1));
                float n101 = Hash13(i + float3(1,0,1));
                float n011 = Hash13(i + float3(0,1,1));
                float n111 = Hash13(i + float3(1,1,1));
                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);
                return lerp(lerp(nx00, nx10, f.y), lerp(nx01, nx11, f.y), f.z);
            }

            // 半分の内側へ寄せた maskUV（境界をまたいだサンプリングを防ぐ）
            float2 MaskUV(float2 uv, float side)
            {
                float t = _FoamMask_TexelSize.y;
                return float2(uv.x, clamp(uv.y * 0.5 + side * 0.5, side * 0.5 + t, side * 0.5 + 0.5 - t));
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;

                // 上下【両方】を読んで max。シーム頂点が反対側を読んでも厚みが増えるだけで済む
                float mUp = SAMPLE_TEXTURE2D_LOD(_FoamMask, sampler_FoamMask, MaskUV(IN.uv, 1.0), 0).r;
                float mLo = SAMPLE_TEXTURE2D_LOD(_FoamMask, sampler_FoamMask, MaskUV(IN.uv, 0.0), 0).r;
                float m   = max(mUp, mLo);

                float3 nOS = normalize(IN.normalOS);

                // 粒のぶんも厚みに混ぜて、表面をぼこぼこにする
                float3 q = IN.positionOS.xyz * _BubbleScale;
                float bump = sin(q.x) * sin(q.y) * sin(q.z);

                float thickness = _ShellOffset
                                + _MaskDisplace * m
                                + _MaskDisplace * m * bump * _BubbleDepth * 0.6;

                float3 posOS = IN.positionOS.xyz + nOS * thickness;

                VertexPositionInputs vp = GetVertexPositionInputs(posOS);
                VertexNormalInputs   vn = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = vp.positionCS;
                OUT.positionWS = vp.positionWS;
                OUT.normalWS   = vn.normalWS;
                OUT.uv         = IN.uv;
                OUT.positionOS = IN.positionOS.xyz;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // ★side は fragment ごとに決める
                float side = IN.positionOS.x >= 0.0 ? 1.0 : 0.0;
                float m = SAMPLE_TEXTURE2D(_FoamMask, sampler_FoamMask, MaskUV(IN.uv, side)).r;

                // 輪郭を崩して、板っぽさを消す
                float n = ValueNoise(IN.positionOS * _NoiseScale);
                clip(m - (1.0 - n) * _NoiseStrength - _ClipThreshold);

                // ── 粒の陰影 ──
                // sin の積の解析的な勾配で法線をずらす。導関数もテクスチャも使わないので軽い
                float3 q  = IN.positionOS * _BubbleScale;
                float3 g  = float3(cos(q.x) * sin(q.y) * sin(q.z),
                                   sin(q.x) * cos(q.y) * sin(q.z),
                                   sin(q.x) * sin(q.y) * cos(q.z));
                // 規則的になりすぎないよう、値ノイズで少し崩す
                g += (ValueNoise(IN.positionOS * _BubbleScale * 0.7) - 0.5) * 0.8;

                float3 N = normalize(IN.normalWS + g * _BubbleDepth);
                Light  L = GetMainLight();

                float  ndl = saturate(dot(N, L.direction)) * 0.5 + 0.5;
                float3 amb = SampleSH(N);
                float3 col = _FoamColor.rgb * (L.color * ndl + amb * 0.6);

                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));
                col += pow(1.0 - saturate(dot(N, V)), 3.0) * _RimStrength;

                return half4(col, 1);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
