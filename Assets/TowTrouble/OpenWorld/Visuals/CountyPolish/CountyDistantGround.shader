Shader "TowTrouble/County Distant Ground"
{
 Properties { _Grass("Grass",2D)="white"{} _Snow("Snow",2D)="white"{} _Sand("Sand",2D)="white"{} }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 Pass { Tags {"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_Grass);SAMPLER(sampler_Grass);TEXTURE2D(_Snow);SAMPLER(sampler_Snow);TEXTURE2D(_Sand);SAMPLER(sampler_Sand);
 struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;float fog:TEXCOORD2;};
 V vert(A i){V o;o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);o.n=TransformObjectToWorldNormal(i.n);o.fog=ComputeFogFactor(o.p.z);return o;}
 half4 frag(V i):SV_Target{float2 uv=i.world.xz/18;float snow=smoothstep(112,152,i.world.y);float desert=smoothstep(140,250,i.world.x)*(1-smoothstep(80,180,i.world.z));half3 grass=SAMPLE_TEXTURE2D(_Grass,sampler_Grass,uv).rgb*half3(.52,.60,.37);half3 sand=SAMPLE_TEXTURE2D(_Sand,sampler_Sand,uv).rgb*half3(.75,.61,.43);half3 ice=SAMPLE_TEXTURE2D(_Snow,sampler_Snow,uv).rgb*half3(.87,.92,.95);half3 col=lerp(lerp(grass,sand,desert),ice,snow);Light l=GetMainLight();half3 lighting=SampleSH(normalize(i.n))+l.color*saturate(dot(normalize(i.n),l.direction));return half4(MixFog(col*max(lighting,.3),i.fog),1);}
 ENDHLSL }
 }
}
