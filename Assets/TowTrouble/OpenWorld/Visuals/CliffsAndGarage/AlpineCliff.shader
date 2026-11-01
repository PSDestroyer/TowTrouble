Shader "TowTrouble/Alpine Cliff"
{
 Properties { _Rock_Texture("Rock",2D)="white"{} _Snow_Texture("Snow",2D)="white"{} _Rock_Tint("Rock tint",Color)=(.62,.65,.66,1) _Snow_Tint("Snow tint",Color)=(.92,.96,1,1) _TopSnow("Snow cap",Float)=0 _Rock_Scale("World texture scale",Float)=8 _Rock_Normal_Texture("Normal",2D)="bump"{} _MaskTopLayer("Top mask",Float)=0 _Rock_Normal_Flatten("Normal flatten",Float)=.2 }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Cull Off
 Pass { Tags {"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float4 _Rock_Tint,_Snow_Tint;float _TopSnow,_Rock_Scale;
 CBUFFER_END
 TEXTURE2D(_Rock_Texture);SAMPLER(sampler_Rock_Texture);TEXTURE2D(_Snow_Texture);SAMPLER(sampler_Snow_Texture);
 struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;float fog:TEXCOORD2;};
 V vert(A i){V o;o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);o.n=TransformObjectToWorldNormal(i.n);o.fog=ComputeFogFactor(o.p.z);return o;}
 half4 frag(V i,FRONT_FACE_TYPE facing:FRONT_FACE_SEMANTIC):SV_Target{
 float3 n=normalize(i.n);n=IS_FRONT_VFACE(facing,n,-n);float3 w=pow(abs(n),4);w/=max(dot(w,1),.001);float3 uv=i.world/max(_Rock_Scale,1);
 half3 rock=(SAMPLE_TEXTURE2D(_Rock_Texture,sampler_Rock_Texture,uv.yz).rgb*w.x+SAMPLE_TEXTURE2D(_Rock_Texture,sampler_Rock_Texture,uv.xz).rgb*w.y+SAMPLE_TEXTURE2D(_Rock_Texture,sampler_Rock_Texture,uv.xy).rgb*w.z)*_Rock_Tint.rgb;
 rock*=.94+.06*sin(i.world.y*2.1+sin(i.world.x*.2));half3 snow=SAMPLE_TEXTURE2D(_Snow_Texture,sampler_Snow_Texture,i.world.xz/12).rgb*_Snow_Tint.rgb;
 float cap=smoothstep(.25,.65,n.y)*saturate(_TopSnow);half3 col=lerp(rock,snow,cap);Light l=GetMainLight(TransformWorldToShadowCoord(i.world));half3 lighting=SampleSH(n)+l.color*saturate(dot(n,l.direction))*(.6+.4*l.shadowAttenuation);return half4(MixFog(col*max(lighting,.5),i.fog),1);}
 ENDHLSL }
 UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 }
}
