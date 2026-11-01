Shader "TowTrouble/County Sky"
{
 Properties { _Horizon("Horizon",Color)=(.62,.79,.88,1) _Zenith("Zenith",Color)=(.22,.50,.75,1) _SunDirection("Sun direction",Vector)=(.2,.7,.3,0) }
 SubShader { Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"} Cull Off ZWrite Off
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float4 _Horizon,_Zenith,_SunDirection;
 CBUFFER_END
 struct A{float4 positionOS:POSITION;};struct V{float4 positionCS:SV_POSITION;float3 direction:TEXCOORD0;};
 V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.direction=mul((float3x3)unity_ObjectToWorld,i.positionOS.xyz);return o;}
 half4 frag(V i):SV_Target{float3 d=normalize(i.direction);float h=pow(saturate(d.y),.55);half3 col=lerp(_Horizon.rgb,_Zenith.rgb,h);float sun=pow(saturate(dot(d,normalize(_SunDirection.xyz))),600);return half4(col+half3(1,.81,.49)*sun*.7,1);}
 ENDHLSL }
 }
}
