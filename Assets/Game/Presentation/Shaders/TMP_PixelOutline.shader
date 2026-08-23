Shader "BombSwap/UI/TMP Pixel Outline"
{
    Properties
    {
        _MainTex ("Font Atlas", 2D) = "white" {}
        _FaceTex ("Font Texture", 2D) = "white" {}
        _FaceColor ("Text Color", Color) = (1, 1, 1, 1)

        _OutlineColor ("Outline Color", Color) = (0.035, 0.04, 0.06, 1)
        [IntRange] _OutlineWidth ("Outline Width (Atlas Pixels)", Range(0, 2)) = 1
        _Padding ("TMP Mesh Padding", Float) = 2

        _VertexOffsetX ("Vertex Offset X", Float) = 0
        _VertexOffsetY ("Vertex Offset Y", Float) = 0
        _MaskSoftnessX ("Mask Softness X", Float) = 0
        _MaskSoftnessY ("Mask Softness Y", Float) = 0

        _ClipRect ("Clip Rect", Vector) = (-32767, -32767, 32767, 32767)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _CullMode ("Cull Mode", Float) = 0
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Lighting Off
        Cull [_CullMode]
        ZTest [unity_GUIZTestMode]
        ZWrite Off
        Fog { Mode Off }
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float4 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float4 mask : TEXCOORD2;
            };

            sampler2D _MainTex;
            sampler2D _FaceTex;
            float4 _MainTex_TexelSize;
            float4 _FaceTex_ST;
            fixed4 _FaceColor;
            fixed4 _OutlineColor;
            float _OutlineWidth;

            float _VertexOffsetX;
            float _VertexOffsetY;
            float4 _ClipRect;
            float _MaskSoftnessX;
            float _MaskSoftnessY;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;
            int _UIVertexColorAlwaysGammaSpace;

            v2f vert(appdata_t input)
            {
                float4 vertex = input.vertex;
                vertex.x += _VertexOffsetX;
                vertex.y += _VertexOffsetY;
                vertex.xy += (vertex.w * 0.5) / _ScreenParams.xy;

                float4 clipPosition = UnityPixelSnap(UnityObjectToClipPos(vertex));

                if (_UIVertexColorAlwaysGammaSpace && !IsGammaSpace())
                {
                    input.color.rgb = UIGammaToLinear(input.color.rgb);
                }

                v2f output;
                output.vertex = clipPosition;
                output.color = input.color * _FaceColor;
                output.texcoord0 = input.texcoord0;
                output.texcoord1 = TRANSFORM_TEX(input.texcoord1, _FaceTex);

                float2 pixelSize = clipPosition.w;
                pixelSize /= abs(float2(
                    _ScreenParams.x * UNITY_MATRIX_P[0][0],
                    _ScreenParams.y * UNITY_MATRIX_P[1][1]));

                const float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                const half2 maskSoftness = half2(
                    max(_UIMaskSoftnessX, _MaskSoftnessX),
                    max(_UIMaskSoftnessY, _MaskSoftnessY));
                output.mask = float4(
                    vertex.xy * 2 - clampedRect.xy - clampedRect.zw,
                    0.25 / (0.25 * maskSoftness + pixelSize.xy));

                return output;
            }

            fixed SampleRing(float2 uv, float2 offset)
            {
                fixed coverage = tex2D(_MainTex, uv + float2(offset.x, 0)).a;
                coverage = max(coverage, tex2D(_MainTex, uv - float2(offset.x, 0)).a);
                coverage = max(coverage, tex2D(_MainTex, uv + float2(0, offset.y)).a);
                coverage = max(coverage, tex2D(_MainTex, uv - float2(0, offset.y)).a);
                coverage = max(coverage, tex2D(_MainTex, uv + offset).a);
                coverage = max(coverage, tex2D(_MainTex, uv - offset).a);
                coverage = max(coverage, tex2D(_MainTex, uv + float2(offset.x, -offset.y)).a);
                coverage = max(coverage, tex2D(_MainTex, uv + float2(-offset.x, offset.y)).a);
                return coverage;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed faceCoverage = tex2D(_MainTex, input.texcoord0).a;
                fixed3 faceRgb = tex2D(_FaceTex, input.texcoord1).rgb * input.color.rgb;

                fixed outlineCoverage = 0;
                if (_OutlineWidth > 0.5)
                {
                    float2 atlasTexel = _MainTex_TexelSize.xy;
                    outlineCoverage = SampleRing(input.texcoord0, atlasTexel);
                    if (_OutlineWidth > 1.5)
                    {
                        outlineCoverage = max(
                            outlineCoverage,
                            SampleRing(input.texcoord0, atlasTexel * 2));
                    }
                }

                fixed faceAlpha = faceCoverage * input.color.a;
                fixed outlineAlpha = outlineCoverage * _OutlineColor.a * input.color.a;
                fixed4 color = fixed4(
                    lerp(_OutlineColor.rgb, faceRgb, faceCoverage),
                    max(faceAlpha, outlineAlpha));

                #if UNITY_UI_CLIP_RECT
                    half2 mask = saturate(
                        (_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) *
                        input.mask.zw);
                    color *= mask.x * mask.y;
                #endif

                #if UNITY_UI_ALPHACLIP
                    clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }

    CustomEditor "BombSwap.Editor.UI.PixelFontShaderGui"
}
