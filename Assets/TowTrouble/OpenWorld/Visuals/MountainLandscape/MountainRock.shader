Shader "TowTrouble/Mountain Rock"
{
    Properties
    {
        _Rock_Texture("Rock colour", 2D) = "white" {}
        [Normal] _Rock_Normal_Texture("Rock normal", 2D) = "bump" {}
        _Rock_Tint("Rock tint", Color) = (0.8, 0.82, 0.8, 1)
        _Snow_Texture("Snow colour", 2D) = "white" {}
        _Snow_Tint("Snow tint", Color) = (0.92, 0.95, 1, 1)
        _TopSnow("Snow on shelves", Range(0, 1)) = 0
        _Rock_Scale("Texture repeat in metres", Float) = 8
        _NormalStrength("Rock normal strength", Range(0, 2)) = 0.75
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        Cull Back
        ZWrite On
        ZTest LEqual

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        // The same material buffer is used by every pass for SRP batching.
        CBUFFER_START(UnityPerMaterial)
            half4 _Rock_Tint;
            half4 _Snow_Tint;
            float _Rock_Scale;
            half _TopSnow;
            half _NormalStrength;
        CBUFFER_END

        TEXTURE2D(_Rock_Texture); SAMPLER(sampler_Rock_Texture);
        TEXTURE2D(_Rock_Normal_Texture); SAMPLER(sampler_Rock_Normal_Texture);
        TEXTURE2D(_Snow_Texture); SAMPLER(sampler_Snow_Texture);

        struct RockAttributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 lightmapUV : TEXCOORD1;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct RockVaryings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float3 normalWS : TEXCOORD1;
            half4 fogAndVertexLight : TEXCOORD2;
            DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 3);
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        struct RockProjection
        {
            float2 x;
            float2 y;
            float2 z;
            half3 weights;
            half3 facing;
        };

        RockProjection ProjectRock(float3 positionWS, half3 normalWS)
        {
            RockProjection p;
            float3 worldUV = positionWS / max(_Rock_Scale, 0.1);
            p.facing = half3(normalWS.x < 0 ? -1 : 1,
                             normalWS.y < 0 ? -1 : 1,
                             normalWS.z < 0 ? -1 : 1);
            // Signed coordinates give each projection an outward-facing basis.
            p.x = float2(-worldUV.z * p.facing.x, worldUV.y);
            p.y = float2(worldUV.x, -worldUV.z * p.facing.y);
            p.z = float2(worldUV.x * p.facing.z, worldUV.y);
            half3 w = normalWS * normalWS;
            w *= w;
            p.weights = w / max(w.x + w.y + w.z, 0.001h);
            return p;
        }

        half3 SampleRockNormal(RockProjection p, half3 geometricNormal)
        {
            half3 nx = UnpackNormalScale(SAMPLE_TEXTURE2D(_Rock_Normal_Texture,
                sampler_Rock_Normal_Texture, p.x), _NormalStrength);
            half3 ny = UnpackNormalScale(SAMPLE_TEXTURE2D(_Rock_Normal_Texture,
                sampler_Rock_Normal_Texture, p.y), _NormalStrength);
            half3 nz = UnpackNormalScale(SAMPLE_TEXTURE2D(_Rock_Normal_Texture,
                sampler_Rock_Normal_Texture, p.z), _NormalStrength);

            // Blend surface slopes, then project them onto the actual mesh surface.
            // A flat normal map therefore preserves the authored cliff normal,
            // including curved joins, instead of turning it into a cube normal.
            half2 sx = nx.xy / max(nx.z, 0.25h);
            half2 sy = ny.xy / max(ny.z, 0.25h);
            half2 sz = nz.xy / max(nz.z, 0.25h);
            half3 slope = half3(0, sx.y, -sx.x * p.facing.x) * p.weights.x
                        + half3(sy.x, 0, -sy.y * p.facing.y) * p.weights.y
                        + half3(sz.x * p.facing.z, sz.y, 0) * p.weights.z;
            slope -= geometricNormal * dot(slope, geometricNormal);
            return NormalizeNormalPerPixel(geometricNormal + slope);
        }

        half SnowCoverage(half3 geometricNormal)
        {
            return smoothstep(0.52h, 0.84h, geometricNormal.y) * saturate(_TopSnow);
        }

        half3 RockSurfaceNormal(RockProjection projection, half3 geometricNormal, half snow)
        {
            half3 rockNormal = SampleRockNormal(projection, geometricNormal);
            return NormalizeNormalPerPixel(lerp(rockNormal, geometricNormal, snow * 0.85h));
        }

        RockVaryings RockVertex(RockAttributes input)
        {
            RockVaryings output = (RockVaryings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
            output.positionCS = position.positionCS;
            output.positionWS = position.positionWS;
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.fogAndVertexLight.x = ComputeFogFactor(position.positionCS.z);
            #ifdef _ADDITIONAL_LIGHTS_VERTEX
                output.fogAndVertexLight.yzw = VertexLighting(position.positionWS, output.normalWS);
            #endif
            OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
            OUTPUT_SH(output.normalWS, output.vertexSH);
            return output;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex RockVertex
            #pragma fragment RockFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK

            half4 RockFragment(RockVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 geometricNormal = NormalizeNormalPerPixel(input.normalWS);
                RockProjection p = ProjectRock(input.positionWS, geometricNormal);
                half3 rock = SAMPLE_TEXTURE2D(_Rock_Texture, sampler_Rock_Texture, p.x).rgb * p.weights.x
                           + SAMPLE_TEXTURE2D(_Rock_Texture, sampler_Rock_Texture, p.y).rgb * p.weights.y
                           + SAMPLE_TEXTURE2D(_Rock_Texture, sampler_Rock_Texture, p.z).rgb * p.weights.z;

                // Low-amplitude bedding follows one world height across neighbouring meshes.
                float strata = sin(input.positionWS.y * 0.73 + dot(rock, half3(0.31h, 0.47h, 0.22h)) * 0.9);
                rock *= _Rock_Tint.rgb * (1.0h + 0.035h * strata);
                half snow = SnowCoverage(geometricNormal);
                half3 snowColour = SAMPLE_TEXTURE2D(_Snow_Texture, sampler_Snow_Texture,
                    input.positionWS.xz / max(_Rock_Scale, 0.1)).rgb * _Snow_Tint.rgb;
                half3 normalWS = RockSurfaceNormal(p, geometricNormal, snow);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = lerp(rock, snowColour, snow);
                surface.metallic = 0;
                surface.smoothness = lerp(0.14h, 0.23h, snow);
                surface.normalTS = half3(0, 0, 1);
                surface.occlusion = 1;
                surface.alpha = 1;

                InputData lighting = (InputData)0;
                lighting.positionWS = input.positionWS;
                lighting.positionCS = input.positionCS;
                lighting.normalWS = normalWS;
                lighting.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    lighting.shadowCoord = ComputeScreenPos(TransformWorldToHClip(input.positionWS));
                #else
                    lighting.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif
                lighting.fogCoord = input.fogAndVertexLight.x;
                lighting.vertexLighting = input.fogAndVertexLight.yzw;
                lighting.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS);
                lighting.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                half4 colour = UniversalFragmentPBR(lighting, surface);
                colour.rgb = MixFog(colour.rgb, lighting.fogCoord);
                return half4(colour.rgb, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex RockShadowVertex
            #pragma fragment RockShadowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            float3 _LightDirection;
            float3 _LightPosition;

            float4 RockShadowVertex(RockAttributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 directionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 directionWS = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, directionWS));
                return ApplyShadowClamping(positionCS);
            }

            half4 RockShadowFragment() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex RockVertex
            #pragma fragment RockDepthFragment
            #pragma multi_compile_instancing

            half RockDepthFragment(RockVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormalsOnly" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex RockVertex
            #pragma fragment RockNormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            half4 RockNormalsFragment(RockVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 geometricNormal = NormalizeNormalPerPixel(input.normalWS);
                RockProjection p = ProjectRock(input.positionWS, geometricNormal);
                half3 normalWS = RockSurfaceNormal(p, geometricNormal, SnowCoverage(geometricNormal));
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octNormal = PackNormalOctQuadEncode(normalWS);
                    half3 packed = PackFloat2To888(saturate(octNormal * 0.5 + 0.5));
                    return half4(packed, 0);
                #else
                    return half4(normalWS, 0);
                #endif
            }
            ENDHLSL
        }
    }
    FallBack Off
}
