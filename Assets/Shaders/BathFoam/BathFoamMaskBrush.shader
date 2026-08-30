// 泡マスクへブラシを描き足すブリット用シェーダー（本番 / Phase A1）
//
// Assets/Shaders/BathFoamPrototype/BathFoamMaskBrush.shader の検証済みコードをそのまま複製し、
// Shader 名だけ本番用に変更したもの。logical UV とブラシ縦横比、境界1テクセルの扱いは変えていない。
//
// 【Ping-Pong】Graphics.Blit(読み用RT, 書き込み用RT, このマテリアル)。同じRTを読み書きしない。
//
// 【★論理UVで距離を測る】
//   マスクは 512x1024 で上下2段に分けているが、各半分は論理的には 512x512。
//   packed UV（RT全体の0〜1）のまま距離を測ると、縦方向だけ半分に潰れて
//   ブラシが楕円になる。そこで担当する半分を 0〜1 に戻した logical UV で測る。
//     pixelSide = packedUV.y >= 0.5 ? 1 : 0
//     logicalUV = float2(packedUV.x, frac(packedUV.y * 2))
//   ブラシの始点・終点は C# から「元メッシュのUV（0〜1）」のまま渡す。
//
// 【★担当外の半分は絶対に書き換えない】
//   pixelSide が _BrushSide と違うピクセルは前の値をそのまま返す。
//   さらに境界の1テクセルは触らず、反対側への色漏れを防ぐ。
Shader "Yurufu/BathFoam/MaskBrush"
{
    Properties
    {
        _MainTex     ("前のマスク", 2D)        = "black" {}
        _BrushP0     ("線の始点(論理UV)", Vector) = (0,0,0,0)
        _BrushP1     ("線の終点(論理UV)", Vector) = (0,0,0,0)
        _BrushRadius ("ブラシ半径",       Float)  = 0.03
        _BrushSoft   ("ブラシのぼかし",   Range(0,1)) = 0.5
        _BrushStr    ("塗る強さ",         Range(0,1)) = 0.6
        _BrushSide   ("担当する半分 0=下(X<0) 1=上(X>=0)", Float) = 0
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always
        Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert_img
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _BrushP0;
            float4 _BrushP1;
            float  _BrushRadius;
            float  _BrushSoft;
            float  _BrushStr;
            float  _BrushSide;

            float SegDist(float2 p, float2 a, float2 b)
            {
                float2 ab = b - a;
                float  L2 = dot(ab, ab);
                float  t  = (L2 > 1e-12) ? saturate(dot(p - a, ab) / L2) : 0.0;
                return distance(p, a + ab * t);
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                float prev = tex2D(_MainTex, i.uv).r;

                // 担当する半分か
                float pixelSide = i.uv.y >= 0.5 ? 1.0 : 0.0;
                if (abs(pixelSide - _BrushSide) > 0.5) return fixed4(prev, 0, 0, 1);

                // 境界の1テクセルは触らない（反対側への漏れ防止）
                float texelV = _MainTex_TexelSize.y;
                if (i.uv.y < texelV || i.uv.y > 1.0 - texelV) return fixed4(prev, 0, 0, 1);
                if (abs(i.uv.y - 0.5) < texelV)               return fixed4(prev, 0, 0, 1);

                // 担当半分を 0〜1 に戻した論理UVで距離を測る（ブラシが楕円にならない）
                float2 logicalUV = float2(i.uv.x, frac(i.uv.y * 2.0));

                float d = SegDist(logicalUV, _BrushP0.xy, _BrushP1.xy);

                float inner = _BrushRadius * (1.0 - saturate(_BrushSoft));
                float add   = 1.0 - smoothstep(inner, max(_BrushRadius, inner + 1e-5), d);

                return fixed4(saturate(prev + add * _BrushStr), 0, 0, 1);
            }
            ENDCG
        }
    }

    FallBack Off
}
