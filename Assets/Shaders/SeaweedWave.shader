Shader "Custom/SeaweedWave"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _WaveSpeed ("Wave Speed", Float) = 1.5
        _WaveAmplitude ("Wave Amplitude (units)", Float) = 0.125
        _WaveXFrequency ("Wave X Variation", Float) = 1.2
        _PhaseRandomness ("Phase Randomness", Float) = 1.0
        _PixelsPerUnit ("Pixels Per Unit", Float) = 16.0
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

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                float _WaveSpeed;
                float _WaveAmplitude;
                float _WaveXFrequency;
                float _PhaseRandomness;
                float _PixelsPerUnit;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color     : COLOR;
                float2 uv        : TEXCOORD0;
                uint   vertexID  : SV_VertexID;
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

                uint localIdx = IN.vertexID % 4;
                float heightFactor = (localIdx >= 2) ? 1.0 : 0.0;

                float2 tilePos = round(IN.positionOS.xy);
                float randomPhase = frac(sin(dot(tilePos, float2(127.1, 311.7))) * 43758.5453) * 6.2832 * _PhaseRandomness;

                float wave = sin(_Time.y * _WaveSpeed + IN.positionOS.x * _WaveXFrequency + randomPhase) * _WaveAmplitude;

                float pixelSize = 1.0 / _PixelsPerUnit;
                wave = round(wave / pixelSize) * pixelSize;

                IN.positionOS.x += wave * heightFactor;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.lightingUV  = half2(ComputeScreenPos(OUT.positionHCS / OUT.positionHCS.w).xy);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 main = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                main *= IN.color;

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
