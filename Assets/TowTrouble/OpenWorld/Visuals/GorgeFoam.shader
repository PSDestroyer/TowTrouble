Shader "TowTrouble/Gorge Foam"
{
 Properties { _Color("Color",Color)=(.85,.96,.92,.65) _Mist("Mist",Float)=0 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+12" "RenderType"="Transparent" }
  Pass
  {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _Color; float _Mist;
   CBUFFER_END
   struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
   struct V { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
   V vert(A a){ V o;o.pos=TransformObjectToHClip(a.positionOS.xyz);o.uv=a.uv;o.color=a.color;return o; }
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
   half4 frag(V i):SV_Target
   {
    float2 p=i.uv*2-1;float r=length(p);float t=_Time.y;
    float n=noise(i.uv*14+float2(t*.18,-t*.48));float n2=noise(i.uv*31+float2(-t*.28,t*.13));
    float edge=1-smoothstep(.35,.98,r+(n-.5)*.18);
    float froth=smoothstep(.32,.7,n*.64+n2*.36)*edge;
    float mist=(1-smoothstep(0,1,r))*.65;
    return half4(_Color.rgb,_Color.a*lerp(froth,mist,_Mist)*lerp(1,i.color.a,_Mist));
   }
   ENDHLSL
  }
 }
}
