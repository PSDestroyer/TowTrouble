Shader "TowTrouble/Gorge Falling Water"
{
 Properties { _Deep("Water tint",Color)=(.43,.66,.68,1) _White("Aerated water",Color)=(.88,.97,.95,1) }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+5" "RenderType"="Transparent" }
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _Deep;half4 _White;
   CBUFFER_END
   struct A{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
   struct V{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 normal:TEXCOORD1;};
   V vert(A a){V o;o.pos=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;o.normal=TransformObjectToWorldNormal(a.normalOS);return o;}
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
   half4 frag(V i):SV_Target
   {
    float t=_Time.y;float2 uv=i.uv;
    float broad=noise(float2(uv.x*17,uv.y*3.4+t*1.1));
    float fine=noise(float2(uv.x*74+sin(uv.y*11+t)*.4,uv.y*12+t*3.2));
    float foam=smoothstep(.33,.76,broad*.65+fine*.35);
    foam=saturate(.16+foam*.68+pow(1-uv.y,4)*.16);
    float edge=smoothstep(0,.045,uv.x)*smoothstep(0,.045,1-uv.x);
    Light sun=GetMainLight();float light=.65+.30*saturate(dot(normalize(i.normal),sun.direction));
    return half4(lerp(_Deep.rgb,_White.rgb,foam)*light,edge*(.81+foam*.17));
   }
   ENDHLSL
  }
 }
}
