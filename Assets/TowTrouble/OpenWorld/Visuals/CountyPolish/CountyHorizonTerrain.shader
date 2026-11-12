Shader "TowTrouble/County Horizon Terrain"
{
    Properties
    {
        _Layer0("Grass", 2D) = "white" {}
        _Layer1("Coastal sand", 2D) = "white" {}
        _Layer2("Rock", 2D) = "white" {}
        _Layer3("Snow", 2D) = "white" {}
        _Layer4("Desert sand", 2D) = "white" {}
        [HideInInspector] _Tile0("Grass scale and offset", Vector) = (8, 8, 0, 0)
        [HideInInspector] _Tile1("Coastal scale and offset", Vector) = (8, 8, 0, 0)
        [HideInInspector] _Tile2("Rock scale and offset", Vector) = (12, 12, 0, 0)
        [HideInInspector] _Tile3("Snow scale and offset", Vector) = (10, 10, 0, 0)
        [HideInInspector] _Tile4("Desert scale and offset", Vector) = (8, 8, 0, 0)
        [HideInInspector] _Tint0("Grass tint", Vector) = (1, 1, 1, 1)
        [HideInInspector] _Tint1("Coastal tint", Vector) = (1, 1, 1, 1)
        [HideInInspector] _Tint2("Rock tint", Vector) = (1, 1, 1, 1)
        [HideInInspector] _Tint3("Snow tint", Vector) = (1, 1, 1, 1)
        [HideInInspector] _Tint4("Desert tint", Vector) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_Layer0); SAMPLER(sampler_Layer0);
            TEXTURE2D(_Layer1); SAMPLER(sampler_Layer1);
            TEXTURE2D(_Layer2); SAMPLER(sampler_Layer2);
            TEXTURE2D(_Layer3); SAMPLER(sampler_Layer3);
            TEXTURE2D(_Layer4); SAMPLER(sampler_Layer4);

            CBUFFER_START(UnityPerMaterial)
                float4 _Tile0, _Tile1, _Tile2, _Tile3, _Tile4;
                half4 _Tint0, _Tint1, _Tint2, _Tint3, _Tint4;
            CBUFFER_END

            struct Attributes
            {
                float4 position : POSITION;
                float3 normal : NORMAL;
                half4 weights : COLOR;
                float2 desert : TEXCOORD0;
            };
            struct Varyings
            {
                float4 position : SV_POSITION;
                float3 normal : TEXCOORD0;
                float2 terrainXZ : TEXCOORD1;
                half4 weights : TEXCOORD2;
                half desert : TEXCOORD3;
                half fog : TEXCOORD4;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.position.xyz);
                output.position = TransformWorldToHClip(world);
                output.normal = TransformObjectToWorldNormal(input.normal);
                output.terrainXZ = world.xz + 500;
                output.weights = input.weights;
                output.desert = input.desert.x;
                output.fog = ComputeFogFactor(output.position.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.terrainXZ;
                half3 color = SAMPLE_TEXTURE2D(_Layer0, sampler_Layer0, (p + _Tile0.zw) / _Tile0.xy).rgb * _Tint0.rgb * input.weights.x;
                color += SAMPLE_TEXTURE2D(_Layer1, sampler_Layer1, (p + _Tile1.zw) / _Tile1.xy).rgb * _Tint1.rgb * input.weights.y;
                color += SAMPLE_TEXTURE2D(_Layer2, sampler_Layer2, (p + _Tile2.zw) / _Tile2.xy).rgb * _Tint2.rgb * input.weights.z;
                color += SAMPLE_TEXTURE2D(_Layer3, sampler_Layer3, (p + _Tile3.zw) / _Tile3.xy).rgb * _Tint3.rgb * input.weights.w;
                color += SAMPLE_TEXTURE2D(_Layer4, sampler_Layer4, (p + _Tile4.zw) / _Tile4.xy).rgb * _Tint4.rgb * input.desert;
                half3 normal = normalize(input.normal);
                Light sun = GetMainLight();
                half3 light = SampleSH(normal) + sun.color * saturate(dot(normal, sun.direction));
                return half4(MixFog(color * max(light, .3), input.fog), 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
