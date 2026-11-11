Shader "TowTrouble/Stylized Recovery Ground Blend"
{
 Properties { _TextureDetail("Loose texture detail",Range(0,1))=.3 _TextureMean("Filtering softness",Range(0,1))=.62 _ShadowStrength("Shadow strength",Range(0,1))=.72  _CoverEnabled("Loose surface",Float)=0 _CoverMap("Loose texture",2D)="white"{} _CoverTint("Loose tint",Color)=(1,1,1,1) _CoverTiling("Loose tiling",Vector)=(1,1,0,0) _Control0("Terrain weights 0",2D)="black"{} _Control1("Terrain weights 1",2D)="black"{} _TerrainSize("Terrain size and origin",Vector)=(1000,1000,-500,-500) _Ground0("Ground 0",2D)="white"{} _Tile0("Tile 0",Vector)=(15,15,0,0) _Tint0("Tint 0",Vector)=(1,1,1,1) _Ground1("Ground 1",2D)="white"{} _Tile1("Tile 1",Vector)=(15,15,0,0) _Tint1("Tint 1",Vector)=(1,1,1,1) _Ground2("Ground 2",2D)="white"{} _Tile2("Tile 2",Vector)=(15,15,0,0) _Tint2("Tint 2",Vector)=(1,1,1,1) _Ground3("Ground 3",2D)="white"{} _Tile3("Tile 3",Vector)=(15,15,0,0) _Tint3("Tint 3",Vector)=(1,1,1,1) _Ground4("Ground 4",2D)="white"{} _Tile4("Tile 4",Vector)=(15,15,0,0) _Tint4("Tint 4",Vector)=(1,1,1,1) _Ground5("Ground 5",2D)="white"{} _Tile5("Tile 5",Vector)=(15,15,0,0) _Tint5("Tint 5",Vector)=(1,1,1,1) _Ground6("Ground 6",2D)="white"{} _Tile6("Tile 6",Vector)=(15,15,0,0) _Tint6("Tint 6",Vector)=(1,1,1,1) _Ground7("Ground 7",2D)="white"{} _Tile7("Tile 7",Vector)=(15,15,0,0) _Tint7("Tint 7",Vector)=(1,1,1,1) }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-5" "RenderType"="Transparent" }
  Pass
  {
   Tags { "LightMode"="UniversalForward" }
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_fog
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _TerrainSize;float4 _CoverTint;float4 _CoverTiling;float _CoverEnabled;float _TextureDetail;float _TextureMean;float _ShadowStrength;
float4 _Tile0;float4 _Tint0;
float4 _Tile1;float4 _Tint1;
float4 _Tile2;float4 _Tint2;
float4 _Tile3;float4 _Tint3;
float4 _Tile4;float4 _Tint4;
float4 _Tile5;float4 _Tint5;
float4 _Tile6;float4 _Tint6;
float4 _Tile7;float4 _Tint7;

   CBUFFER_END
   TEXTURE2D(_CoverMap);SAMPLER(sampler_CoverMap);TEXTURE2D(_Control0); SAMPLER(sampler_Control0);TEXTURE2D(_Control1);
TEXTURE2D(_Ground0); SAMPLER(sampler_Ground0);
TEXTURE2D(_Ground1); SAMPLER(sampler_Ground1);
TEXTURE2D(_Ground2); SAMPLER(sampler_Ground2);
TEXTURE2D(_Ground3); SAMPLER(sampler_Ground3);
TEXTURE2D(_Ground4); SAMPLER(sampler_Ground4);
TEXTURE2D(_Ground5); SAMPLER(sampler_Ground5);
TEXTURE2D(_Ground6); SAMPLER(sampler_Ground6);
TEXTURE2D(_Ground7); SAMPLER(sampler_Ground7);

   struct A { float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 color:COLOR;float2 uv:TEXCOORD0; };
   struct V { float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float4 color:COLOR;float fog:TEXCOORD2;float2 uv:TEXCOORD3; };
   V Vert(A i){V o;o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.normalOS);o.color=i.color;o.uv=i.uv;o.fog=ComputeFogFactor(o.positionCS.z);return o;}
   half4 Frag(V i):SV_Target {Light l=GetMainLight(TransformWorldToShadowCoord(i.world));half3 n=normalize(i.normal);half3 lighting=SampleSH(n)+l.color*saturate(dot(n,l.direction))*lerp(1,l.shadowAttenuation,_ShadowStrength);float2 terrainUV=(i.world.xz-_TerrainSize.zw)/_TerrainSize.xy;half4 w0=SAMPLE_TEXTURE2D(_Control0,sampler_Control0,terrainUV);half4 w1=SAMPLE_TEXTURE2D(_Control1,sampler_Control0,terrainUV);half3 tex=0;tex+=SAMPLE_TEXTURE2D(_Ground0,sampler_Ground0,(i.world.xz-_TerrainSize.zw+_Tile0.zw)/_Tile0.xy).rgb*_Tint0.rgb*w0[0];tex+=SAMPLE_TEXTURE2D(_Ground1,sampler_Ground1,(i.world.xz-_TerrainSize.zw+_Tile1.zw)/_Tile1.xy).rgb*_Tint1.rgb*w0[1];tex+=SAMPLE_TEXTURE2D(_Ground2,sampler_Ground2,(i.world.xz-_TerrainSize.zw+_Tile2.zw)/_Tile2.xy).rgb*_Tint2.rgb*w0[2];tex+=SAMPLE_TEXTURE2D(_Ground3,sampler_Ground3,(i.world.xz-_TerrainSize.zw+_Tile3.zw)/_Tile3.xy).rgb*_Tint3.rgb*w0[3];tex+=SAMPLE_TEXTURE2D(_Ground4,sampler_Ground4,(i.world.xz-_TerrainSize.zw+_Tile4.zw)/_Tile4.xy).rgb*_Tint4.rgb*w1[0];tex+=SAMPLE_TEXTURE2D(_Ground5,sampler_Ground5,(i.world.xz-_TerrainSize.zw+_Tile5.zw)/_Tile5.xy).rgb*_Tint5.rgb*w1[1];tex+=SAMPLE_TEXTURE2D(_Ground6,sampler_Ground6,(i.world.xz-_TerrainSize.zw+_Tile6.zw)/_Tile6.xy).rgb*_Tint6.rgb*w1[2];tex+=SAMPLE_TEXTURE2D(_Ground7,sampler_Ground7,(i.world.xz-_TerrainSize.zw+_Tile7.zw)/_Tile7.xy).rgb*_Tint7.rgb*w1[3];tex=lerp(tex,SAMPLE_TEXTURE2D_BIAS(_CoverMap,sampler_CoverMap,i.uv*_CoverTiling.xy,(1-_TextureDetail)*3*_TextureMean).rgb*_CoverTint.rgb,saturate(i.color.r*_CoverEnabled));return half4(MixFog(tex*max(lighting,.25),i.fog),i.color.a);}
   ENDHLSL
  }
 }
}
