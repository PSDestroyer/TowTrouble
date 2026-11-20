Shader "TowTrouble/Vehicle Body Coating"
{
    Properties
    {
        _WorkflowMode("WorkflowMode", Float) = 1.0

        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}

        _SpecColor("Specular", Color) = (0.2, 0.2, 0.2)
        _SpecGlossMap("Specular", 2D) = "white" {}

        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1.0

        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}

        _Parallax("Scale", Range(0.005, 0.08)) = 0.005
        _ParallaxMap("Height Map", 2D) = "black" {}

        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}

        [HDR] _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        _DetailMask("Detail Mask", 2D) = "white" {}
        _DetailAlbedoMapScale("Scale", Range(0.0, 2.0)) = 1.0
        _DetailAlbedoMap("Detail Albedo x2", 2D) = "linearGrey" {}
        _DetailNormalMapScale("Scale", Range(0.0, 2.0)) = 1.0
        [Normal] _DetailNormalMap("Normal Map", 2D) = "bump" {}

        [HideInInspector] _ClearCoatMask("_ClearCoatMask", Float) = 0.0
        [HideInInspector] _ClearCoatSmoothness("_ClearCoatSmoothness", Float) = 0.0

        _Surface("__surface", Float) = 0.0
        _Blend("__blend", Float) = 0.0
        _Cull("__cull", Float) = 2.0
        [ToggleUI] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _SrcBlendAlpha("__srcA", Float) = 1.0
        [HideInInspector] _DstBlendAlpha("__dstA", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _BlendModePreserveSpecular("_BlendModePreserveSpecular", Float) = 1.0
        [HideInInspector] _AlphaToMask("__alphaToMask", Float) = 0.0
        [HideInInspector] _AddPrecomputedVelocity("_AddPrecomputedVelocity", Float) = 0.0
        [HideInInspector] _XRMotionVectorsPass("_XRMotionVectorsPass", Float) = 1.0

        [ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0
        _QueueOffset("Queue offset", Float) = 0.0

        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _GlossMapScale("Smoothness", Float) = 0.0
        [HideInInspector] _Glossiness("Smoothness", Float) = 0.0
        [HideInInspector] _GlossyReflections("EnvironmentReflections", Float) = 0.0

        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
        [HideInInspector] _SnowAmounts("Snow Amounts", Vector) = (0, 0, 0, 0)
        [HideInInspector] _BodyMudAmounts("Mud Amounts", Vector) = (0, 0, 0, 0)
        [HideInInspector] _BodySandAmounts("Sand Amounts", Vector) = (0, 0, 0, 0)
        [HideInInspector] _SnowWheelLayout("Snow Wheel Layout", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }
            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            ZWrite [_ZWrite]
            Cull [_Cull]
            AlphaToMask [_AlphaToMask]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex LitPassVertex
            #pragma fragment SnowLitFragment
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma multi_compile_local_fragment _ _ALPHATEST_ON
            #pragma multi_compile_local_fragment _ _EMISSION
            #pragma multi_compile_local_fragment _ _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fragment _ LIGHTMAP_BICUBIC_SAMPLING
            #pragma multi_compile_fragment _ REFLECTION_PROBE_ROTATION
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #ifndef REQUIRES_WORLD_SPACE_POS_INTERPOLATOR
                #define REQUIRES_WORLD_SPACE_POS_INTERPOLATOR
            #endif
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"

            float4x4 _SnowWorldToVehicle;
            float4 _SnowAmounts;
            float4 _BodyMudAmounts;
            float4 _BodySandAmounts;
            float4 _SnowWheelLayout;

            float SnowHash(float3 position)
            {
                position = frac(position * .1031);
                position += dot(position, position.yzx + 33.33);
                return frac((position.x + position.y) * position.z);
            }

            float SnowNoise(float3 position)
            {
                float3 cell = floor(position);
                float3 weight = frac(position);
                weight = weight * weight * (3.0 - 2.0 * weight);
                float4 nearPlane = float4(SnowHash(cell), SnowHash(cell + float3(1, 0, 0)),
                    SnowHash(cell + float3(0, 1, 0)), SnowHash(cell + float3(1, 1, 0)));
                cell.z += 1;
                float4 farPlane = float4(SnowHash(cell), SnowHash(cell + float3(1, 0, 0)),
                    SnowHash(cell + float3(0, 1, 0)), SnowHash(cell + float3(1, 1, 0)));
                float4 samples = lerp(nearPlane, farPlane, weight.z);
                return lerp(lerp(samples.x, samples.y, weight.x), lerp(samples.z, samples.w, weight.x), weight.y);
            }

            float2 BodyAmounts(float4 wheels, float right, float axleBlend, float2 arches)
            {
                float front = lerp(wheels.x, wheels.y, right);
                float rear = lerp(wheels.z, wheels.w, right);
                return float2(lerp(rear, front, axleBlend), max(front * arches.x, rear * arches.y));
            }

            void ApplyBodyCoating(float3 positionWS, inout SurfaceData surface)
            {
                if (dot(_SnowAmounts, _SnowAmounts) + dot(_BodyMudAmounts, _BodyMudAmounts) +
                    dot(_BodySandAmounts, _BodySandAmounts) < .0001) return;

                float3 position = mul(_SnowWorldToVehicle, float4(positionWS, 1)).xyz;
                float height = position.y - _SnowWheelLayout.w;
                if (height > .82) return;
                float right = smoothstep(-.22, .22, position.x);
                float2 arches = 1 - smoothstep(.25, 1.05, abs(position.z - _SnowWheelLayout.xy));
                float span = _SnowWheelLayout.x - _SnowWheelLayout.y;
                float axleBlend = saturate((position.z - _SnowWheelLayout.y) / (abs(span) > .01 ? span : 1));
                axleBlend = axleBlend * axleBlend * (3 - 2 * axleBlend);
                float sides = smoothstep(_SnowWheelLayout.z * .32, _SnowWheelLayout.z * .92, abs(position.x));
                float2 snow = BodyAmounts(_SnowAmounts, right, axleBlend, arches);
                float2 mud = BodyAmounts(_BodyMudAmounts, right, axleBlend, arches);
                float2 sand = BodyAmounts(_BodySandAmounts, right, axleBlend, arches);
                // One stable chassis-space pattern for all layers: deposits do not slide when the car turns.
                float clumps = SnowNoise(position * float3(10, 14, 9));
                float grain = SnowNoise(position * 53);
                float snowEdge = lerp(-.27, .34, snow.x) + .25 * snow.y - height + (clumps - .5) * .37 + (grain - .5) * .10;
                float mudEdge = lerp(-.26, .32, mud.x) + .24 * mud.y - height + (clumps - .5) * .38 + (grain - .5) * .07;
                float sandEdge = lerp(-.28, .40, sand.x) + .10 * sand.y - height + (clumps - .5) * .17;
                float3 mask = float3(
                    smoothstep(-.055, .07, snowEdge) * smoothstep(.025, .45, snow.x) * lerp(.68, 1, sides),
                    smoothstep(-.06, .09, mudEdge) * smoothstep(.02, .55, mud.x) * lerp(.55, .94, sides),
                    smoothstep(-.16, .12, sandEdge) * smoothstep(.02, .60, sand.x) * lerp(.53, .74, sides));
                // Keep illuminated lamps and brake-light atlas pixels readable through the surface treatment.
                float lamp = saturate(max(surface.emission.r, max(surface.emission.g, surface.emission.b)) * 2);
                mask *= 1 - lamp;
                float total = mask.x + mask.y + mask.z;
                if (total < .0001) return;
                float3 blend = mask / total;
                float cover = saturate(total);
                half3 powder = lerp(half3(.57, .68, .79), half3(.91, .95, .98), clumps * .7 + grain * .3);
                half3 earth = lerp(half3(.13, .082, .043), half3(.37, .25, .14), clumps * .78 + grain * .22);
                half3 dust = lerp(half3(.64, .51, .33), half3(.88, .77, .55), grain * .55 + clumps * .45);
                half3 coating = powder * blend.x + earth * blend.y + dust * blend.z;
                surface.albedo = lerp(surface.albedo, coating, cover);
                surface.metallic = lerp(surface.metallic, 0, cover);
                surface.specular = lerp(surface.specular, half3(.035, .035, .035), cover);
                surface.smoothness = lerp(surface.smoothness,
                    dot(blend, float3(.16 + .09 * grain, .24 + .12 * clumps, .055)), cover);
                surface.occlusion = lerp(surface.occlusion, .95, cover);
            }

            void SnowLitFragment(
                Varyings input
                , out half4 outColor : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out uint outRenderingLayers : SV_Target1
            #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            #if defined(_PARALLAXMAP)
            #if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
                half3 viewDirTS = input.viewDirTS;
            #else
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 viewDirTS = GetViewDirectionTangentSpace(input.tangentWS, input.normalWS, viewDirWS);
            #endif
                ApplyPerPixelDisplacement(viewDirTS, input.uv);
            #endif

                SurfaceData surfaceData;
                InitializeStandardLitSurfaceData(input.uv, surfaceData);
                ApplyBodyCoating(input.positionWS, surfaceData);

            #ifdef LOD_FADE_CROSSFADE
                LODFadeCrossFade(input.positionCS);
            #endif

                InputData inputData;
                InitializeInputData(input, surfaceData.normalTS, inputData);
                SETUP_DEBUG_TEXTURE_DATA(inputData, UNDO_TRANSFORM_TEX(input.uv, _BaseMap));

            #if defined(_DBUFFER)
                ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
            #endif

                InitializeBakedGIData(input, inputData);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent(_Surface));

                outColor = color;

            #ifdef _WRITE_RENDERING_LAYERS
                outRenderingLayers = EncodeMeshRenderingLayer();
            #endif
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
        UsePass "Universal Render Pipeline/Lit/Meta"
        UsePass "Universal Render Pipeline/Lit/MotionVectors"
    }
}
