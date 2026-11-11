Shader "TowTrouble/R01 Stylized Soil"
{
 Properties { _TextureDetail("Texture detail",Range(0,1))=.28 _ShadowStrength("Shadow strength",Range(0,1))=.75  _BaseMap("Ground",2D)="white"{} _BaseColor("Ground tint",Color)=(.86,.76,.58,.75) _Compaction("Compaction shade",Range(0,1))=.76 _HardSurface("Hard surface tread",Range(0,1))=0 _RutDepth("Recessed bed depth",Range(0,.2))=.08 _EdgeBreakup("Organic scuff edges",Range(0,1))=0 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+5" "RenderType"="Transparent" }
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
    float4 _BaseColor; float4 _BaseMap_ST; float _Compaction; float _HardSurface; float _RutDepth; float _EdgeBreakup; float _TextureDetail; float _ShadowStrength;
   CBUFFER_END
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; float2 rut:TEXCOORD1; float2 ends:TEXCOORD2; float4 color:COLOR; };
   struct V { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; float2 rut:TEXCOORD3; float4 color:COLOR; float fog:TEXCOORD4; float2 ends:TEXCOORD5; };
   V Vert(A i) { V o; o.positionWS=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.uv=TRANSFORM_TEX(i.uv,_BaseMap);o.rut=i.rut;o.ends=i.ends;o.color=i.color;o.fog=ComputeFogFactor(o.positionCS.z);return o; }
   float ScuffNoise(float2 p)
   {
    float2 cell=floor(p),f=frac(p);f=f*f*(3-2*f);
    float4 h=frac(sin(float4(dot(cell,float2(127.1,311.7)),dot(cell+float2(1,0),float2(127.1,311.7)),dot(cell+float2(0,1),float2(127.1,311.7)),dot(cell+1,float2(127.1,311.7))))*43758.5453);
    return lerp(lerp(h.x,h.y,f.x),lerp(h.z,h.w,f.x),f.y);
   }
   half4 Frag(V i):SV_Target
   {
    float shoulder=smoothstep(.32,.46,abs(i.rut.x-.5));float tread=1-smoothstep(.12,.22,abs(frac(i.rut.y*6.2+abs(i.rut.x-.5)*1.6)-.5));
    float compact=lerp(lerp(_Compaction+.08*tread,1.06,shoulder),1,_HardSurface);
    // Resolve the channel walls and compressed tread inside the bounded strip.
    float3 baseN=normalize(i.normalWS);
    float3 dx=ddx(i.positionWS),dy=ddy(i.positionWS),geometricNormal=cross(dx,dy);
    float3 tangent=normalize(cross(dy,geometricNormal)*ddx(i.rut.x)+cross(geometricNormal,dx)*ddy(i.rut.x)+float3(.00001,0,0));
    float u=i.rut.x*2-1;
    float wall=sign(u)*smoothstep(.20,.52,abs(u))*(1-smoothstep(.65,.95,abs(u)));
    float3 n=normalize(baseN-tangent*wall*_RutDepth*15*(1-_HardSurface));
    float3 view=GetWorldSpaceNormalizeViewDir(i.positionWS);
    float2 bedUv=i.uv-view.xz*_RutDepth*(1-shoulder)/max(.3,abs(dot(view,baseN)))*.125;
    Light l=GetMainLight(TransformWorldToShadowCoord(i.positionWS));half3 light=SampleSH(n)+l.color*saturate(dot(n,l.direction))*lerp(1,l.shadowAttenuation,_ShadowStrength);
    half3 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,bedUv).rgb;
    tex=lerp(half3(.68,.68,.68),tex,_TextureDetail);
    compact*=lerp(lerp(.78,1.09,shoulder),1,_HardSurface);
    float edge=1-smoothstep(.12,.49,abs(i.rut.x-.5));
    // World-anchored variation stays continuous across pooled segments and both axles.
    // Opt-in on grass only; loose-ground relief and hard-surface tread keep their response.
    if(_EdgeBreakup>.001)
    {
     float coarse=ScuffNoise(i.positionWS.xz*3.5),fine=ScuffNoise(i.positionWS.xz*11);
     edge=1-smoothstep(.12,.49-_EdgeBreakup*(.025+.075*coarse),abs(i.rut.x-.5));
     edge*=lerp(1,.25+.75*smoothstep(.15,.8,fine),_EdgeBreakup);
    }
    float caps=smoothstep(0,.65,i.rut.y)*smoothstep(0,1,i.ends.x);
    return half4(MixFog(tex*_BaseColor.rgb*compact*max(light,.25),i.fog),_BaseColor.a*i.color.a*edge*caps*lerp(1,.18+.82*tread,_HardSurface));
   }
   ENDHLSL
  }
 }
}
