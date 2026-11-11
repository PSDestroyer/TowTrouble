Shader "TowTrouble/Soft Road Wear"
{
 Properties { _BaseMap("Ground",2D)="white"{} _BaseColor("Tint",Color)=(.4,.3,.2,.4) }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-20" "RenderType"="Transparent" }
  Pass
  {
   Tags { "LightMode"="UniversalForward" }
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Offset -1,-1
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_fog
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor; float4 _BaseMap_ST;
   CBUFFER_END
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; float4 color:COLOR; };
   struct V { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; float4 color:COLOR; float fog:TEXCOORD3; };
   V Vert(A i) { V o; o.positionWS=TransformObjectToWorld(i.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.positionWS); o.normalWS=TransformObjectToWorldNormal(i.normalOS); o.uv=TRANSFORM_TEX(i.uv,_BaseMap); o.color=i.color; o.fog=ComputeFogFactor(o.positionCS.z); return o; }
   half4 Frag(V i):SV_Target { Light l=GetMainLight(TransformWorldToShadowCoord(i.positionWS)); half3 light=SampleSH(i.normalWS)+l.color*saturate(dot(normalize(i.normalWS),l.direction))*lerp(1,l.shadowAttenuation,.8); half3 tex=SAMPLE_TEXTURE2D_BIAS(_BaseMap,sampler_BaseMap,i.uv,1.25).rgb; half3 rgb=tex*_BaseColor.rgb*max(light,.25); return half4(MixFog(rgb,i.fog),_BaseColor.a*i.color.a); }
   ENDHLSL
  }
 }
}
