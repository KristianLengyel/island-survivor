Shader "Custom/SpriteOutline"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineEnabled ("Outline Enabled", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/InputData2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SurfaceData2D.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _OutlineColor;
                float _OutlineEnabled;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4  color       : COLOR;
                float2 uv          : TEXCOORD0;
                half2  lightingUV  : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.lightingUV  = half2(ComputeScreenPos(OUT.positionHCS / OUT.positionHCS.w).xy);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 main = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;

                if (main.a < 0.01)
                {
                    if (_OutlineEnabled > 0.5)
                    {
                        float2 ts = _MainTex_TexelSize.xy;
                        half aU = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( 0,     ts.y)).a;
                        half aD = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( 0,    -ts.y)).a;
                        half aL = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(-ts.x,  0   )).a;
                        half aR = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2( ts.x,  0   )).a;

                        if (aU > 0.01 || aD > 0.01 || aL > 0.01 || aR > 0.01)
                            main = _OutlineColor;
                        else
                            discard;
                    }
                    else
                    {
                        discard;
                    }
                }

                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(main.rgb, main.a, half4(1, 0, 0, 0), half3(0, 0, 1), surfaceData);
                InitializeInputData(IN.uv, IN.lightingUV, inputData);
                return CombinedShapeLightShared(surfaceData, inputData);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
