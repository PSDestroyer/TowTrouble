Shader "TowTrouble/Wheel Impressions"
{
 Properties { _BaseMap("Ground",2D)="white"{} _BaseColor("Ground tint",Color)=(.86,.76,.58,.75) }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent" }
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
   struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; float2 rut:TEXCOORD1; float4 color:COLOR; };
   struct V { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; float2 rut:TEXCOORD3; float4 color:COLOR; float fog:TEXCOORD4; };
   V Vert(A i) { V o; o.positionWS=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.uv=TRANSFORM_TEX(i.uv,_BaseMap);o.rut=i.rut;o.color=i.color;o.fog=ComputeFogFactor(o.positionCS.z);return o; }
   half4 Frag(V i):SV_Target
   {
    float shoulder=smoothstep(.32,.46,abs(i.rut.x-.5));float tread=1-smoothstep(.12,.22,abs(frac(i.rut.y*6.2+abs(i.rut.x-.5)*1.6)-.5));
    float compact=lerp(.83+.05*tread,1.06,shoulder);
    Light l=GetMainLight(TransformWorldToShadowCoord(i.positionWS));half3 n=normalize(i.normalWS);half3 light=SampleSH(n)+l.color*saturate(dot(n,l.direction))*l.shadowAttenuation;
    half3 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
    float edge=1-smoothstep(.22,.5,abs(i.rut.x-.5));
    return half4(MixFog(tex*_BaseColor.rgb*compact*max(light,.25),i.fog),_BaseColor.a*i.color.a*edge);
   }
   ENDHLSL
  }
 }
}
