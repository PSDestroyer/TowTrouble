Shader "TowTrouble/Winter Snow Ruts"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+5" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half fog : TEXCOORD2;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fog = ComputeFogFactor(output.positionCS.z);
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half depth = sqrt(saturate(input.color.r * 1.55));
                half bank = smoothstep(0, .55, input.color.g);
                half3 normalWS = normalize(input.normalWS);
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 light = SampleSH(normalWS) + sun.color * saturate(dot(normalWS, sun.direction)) * sun.shadowAttenuation;
                float2 surfaceXZ = input.positionWS.xz;
                float grain = frac(sin(dot(floor(surfaceXZ * 37), float2(12.9898, 78.233))) * 43758.5453);
                half scuff = sin(surfaceXZ.x * 19 + surfaceXZ.y * 11) * sin(surfaceXZ.y * 26 - surfaceXZ.x * 7) * .5 + .5;
                half3 bed = lerp(half3(.47, .55, .61), half3(.34, .39, .43), scuff);
                half3 snow = lerp(half3(.82, .89, .95), bed, depth);
                snow = lerp(snow, half3(.94, .97, 1), bank * (1 - depth));
                half surfaceDetail = lerp(.9, 1.06, grain) * lerp(1, .89, scuff * depth);
                return half4(MixFog(snow * clamp(light, .25, 1.35) * surfaceDetail, input.fog), input.color.a);
            }
            ENDHLSL
        }
    }
}
