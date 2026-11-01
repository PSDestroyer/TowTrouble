Shader "TowTrouble/Distress Smoke"
{
 Properties { _BaseColor("Tint",Color)=(1,1,1,1) _UseParticleColor("Use particle color",Float)=1 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Tags { "LightMode"="UniversalForward" }
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor; float _UseParticleColor;
   CBUFFER_END
   struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
   struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; float fog:TEXCOORD1; };
   V Vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.uv=i.uv;o.color=lerp(half4(1,1,1,1),i.color,saturate(_UseParticleColor))*_BaseColor;o.fog=ComputeFogFactor(o.positionCS.z);return o;}
   half4 Frag(V i):SV_Target {float2 q=(i.uv-.5)*2;float alpha=pow(saturate(1-dot(q,q)),2);return half4(MixFog(i.color.rgb,i.fog),i.color.a*alpha);}
   ENDHLSL
  }
 }
}
