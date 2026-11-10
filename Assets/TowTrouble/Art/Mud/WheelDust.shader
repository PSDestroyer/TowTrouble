Shader "TowTrouble/Wheel Dust"
{
 Properties { _BaseColor("Tint",Color)=(1,1,1,1) }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor;
   CBUFFER_END
   struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
   struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; float fog:TEXCOORD1; };
   V Vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;o.color=i.color*_BaseColor;o.fog=ComputeFogFactor(o.positionCS.z);return o;}
   half4 Frag(V i):SV_Target
   {
    // Three soft, overlapping lobes break the circular billboard silhouette.
    // No texture, noise sampling, lighting or scene-depth read on Switch.
    half2 p=(i.uv-.5h)*2;
    half2 a=(p-half2(.10h,.08h))*half2(1.35h,1.45h);
    half2 b=(p-half2(-.36h,-.10h))*half2(1.85h,1.65h);
    half2 c=(p-half2(.34h,-.28h))*half2(1.75h,1.95h);
    half cloud=max(saturate(1-dot(a,a)),max(saturate(1-dot(b,b)),saturate(1-dot(c,c)))*.88h);
    half alpha=cloud*cloud;
    return half4(MixFog(i.color.rgb,i.fog),i.color.a*alpha);
   }
   ENDHLSL
  }
 }
}
