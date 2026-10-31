Shader "TowTrouble/County Road"
{
    Properties
    {
        _BaseMap("Ground texture",2D)="white"{}
        _BumpMap("Ground normal",2D)="bump"{}
        _BaseColor("Ground tint",Color)=(1,1,1,1)
        _BumpScale("Normal strength",Range(0,2))=.55
        _Smoothness("Smoothness",Range(0,1))=.08
    }
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest+5"}
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap);SAMPLER(sampler_BumpMap);
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;half4 _BaseColor;half _BumpScale;half _Smoothness;
        CBUFFER_END
        struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 tangentOS:TANGENT;float2 uv:TEXCOORD0;float2 lane:TEXCOORD1;half4 color:COLOR;};
        struct Varyings{float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;half4 tangent:TEXCOORD2;float2 uv:TEXCOORD3;float2 lane:TEXCOORD4;half alpha:TEXCOORD5;half fog:TEXCOORD6;};
        Varyings Vert(Attributes a){Varyings v=(Varyings)0;VertexPositionInputs p=GetVertexPositionInputs(a.positionOS.xyz);VertexNormalInputs n=GetVertexNormalInputs(a.normalOS,a.tangentOS);v.positionCS=p.positionCS;v.world=p.positionWS;v.normal=n.normalWS;v.tangent=half4(n.tangentWS,a.tangentOS.w*GetOddNegativeScale());v.uv=TRANSFORM_TEX(a.uv,_BaseMap);v.lane=a.lane;v.alpha=a.color.a;v.fog=ComputeFogFactor(p.positionCS.z);return v;}
        void Feather(Varyings v){float2 cell=floor(v.world.xz*30);float noise=frac(sin(dot(cell,float2(12.9898,78.233)))*43758.5453);clip(v.alpha-noise-.001);}
        ENDHLSL
        Pass
        {
            Name "ForwardLit" Tags{"LightMode"="UniversalForward"}
            ZWrite On Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            half4 Frag(Varyings v):SV_Target
            {
                Feather(v);
                half3 ground=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,v.uv).rgb*_BaseColor.rgb;
                float macro=.94+.06*sin(v.world.x*.071+sin(v.world.z*.035)*2)*sin(v.world.z*.093);
                float ruts=exp(-pow((abs(v.lane.x)-.33)*17,2));
                ground*=macro*(1-.11*ruts);
                half3 n=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,v.uv),_BumpScale);
                half3 bitangent=cross(v.normal,v.tangent.xyz)*v.tangent.w;
                InputData input=(InputData)0;input.positionWS=v.world;input.normalWS=NormalizeNormalPerPixel(mul(n,half3x3(v.tangent.xyz,bitangent,v.normal)));input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(v.world);input.shadowCoord=TransformWorldToShadowCoord(v.world);input.bakedGI=SampleSH(input.normalWS);input.shadowMask=half4(1,1,1,1);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(v.positionCS);
                SurfaceData surface=(SurfaceData)0;surface.albedo=ground;surface.alpha=1;surface.normalTS=n;surface.smoothness=_Smoothness;surface.occlusion=1;
                half4 color=UniversalFragmentPBR(input,surface);color.rgb=MixFog(color.rgb,v.fog);return color;
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags{"LightMode"="DepthOnly"}
            ZWrite On ColorMask R Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDepth
            half FragDepth(Varyings v):SV_Target{Feather(v);return v.positionCS.z;}
            ENDHLSL
        }
    }
}
