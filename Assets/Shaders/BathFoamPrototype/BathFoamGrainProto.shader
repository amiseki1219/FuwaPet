// 泡粒シェーダー（Phase 2A）
//
// 【役割】
//   泡3.png を ParticleSystem の Billboard として描く。
//   泡シェル（キャラ表面に密着する土台）の「少し手前」に、装飾として散らす。
//
// 【なぜ Sprite-Unlit-Default を使わないのか】
//   ・URP の Sprite-Unlit-Default はプロジェクト共有のパッケージ資産で、書き換えてはいけない
//   ・左右反転（Random Flip）を1粒ずつ切り替えたい
//   ・ZTest / ZWrite / Cull を泡シェルとの前後関係に合わせて自分で決めたい
//
// 【左右反転の渡し方】
//   ParticleSystem の startColor の R を「反転フラグ（0 or 1）」として使う。
//   色そのものは _Tint（マテリアル側）で決めるので、R を色に使う必要がない。
//   これなら Custom Vertex Stream を足さずに済み、Billboard の既定の頂点構成
//   （POSITION / COLOR / TEXCOORD0）だけで完結する。
//   startColor の A は 1粒ごとの Alpha ゆらぎに使う。
//
// 【前後関係】
//   ZTest LEqual + ZWrite Off。
//   泡シェルは Queue=AlphaTest / ZWrite On なので先に深度を書く。
//   そのため「キャラより奥にある泡粒」は、ここで自動的に捨てられる。
//   ＝ 奥の泡が手前へ透けない。
//   ※ プロジェクトの Depth Texture は OFF だが、ここで使うのは実際の深度バッファなので影響しない。
//
// 【Cull Off の理由】
//   Billboard は常にカメラを向くので裏表は出ないが、
//   反転や負のスケールが入っても消えないように両面描画にしてある。
Shader "Yurufu/BathFoamGrainProto"
{
    Properties
    {
        [NoScaleOffset] _MainTex    ("泡の絵 (泡3.png)", 2D)      = "white" {}
        _Tint       ("色（控えめな Tint）", Color)       = (1, 1, 1, 1)
        _AlphaScale ("Alpha",              Range(0, 2)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType"     = "Plane"
        }

        Pass
        {
            Name "FoamGrainForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest  LEqual
            Cull   Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float  _AlphaScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                // ParticleSystem は World シミュレーションなので、頂点は既にワールド座標。
                // 親 Transform は原点・無回転・等倍にしてあるため、そのまま変換してよい。
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = IN.uv;
                OUT.color      = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // color.r = 左右反転フラグ（0 or 1）。色には使わない
                float2 uv = float2(IN.color.r > 0.5 ? (1.0 - IN.uv.x) : IN.uv.x, IN.uv.y);

                // 泡3.png は Sprite Rect が画像全面なので、UV は 0〜1 をそのまま使える。
                // 引き伸ばし・切り取り・タイリングはしない
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                half a = tex.a * IN.color.a * _AlphaScale;
                return half4(tex.rgb * _Tint.rgb, saturate(a));
            }
            ENDHLSL
        }
    }

    FallBack Off
}
