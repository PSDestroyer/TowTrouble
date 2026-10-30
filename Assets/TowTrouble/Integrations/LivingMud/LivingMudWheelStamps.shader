Shader "Hidden/TowTrouble/LivingMudWheelStamps"
{
    Properties { _MainTex("Previous tracks",2D)="black"{} _Tread("Living Mud tire tread",2D)="black"{} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #include "UnityCG.cginc"
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            sampler2D _Tread;
            sampler2D _MainTex;
            float4 _Stamps[12];
            int _StampCount;
            float4 frag(v2f_img input):SV_Target
            {
                float2 uv=input.uv;
                float4 result=tex2D(_MainTex,uv);
                [loop] for(int i=0;i<_StampCount;i++)
                {
                    float4 stamp=_Stamps[i];float2 delta=(uv-stamp.xy)/stamp.z;
                    float s=sin(stamp.w),c=cos(stamp.w);
                    float2 local=mul(float2x2(c,-s,s,c),delta)+.5;
                    if(any(local<0)||any(local>1))continue;
                    float4 tread=tex2Dlod(_Tread,float4(local,0,0));
                    // Fade only the perimeter of each authored normal/alpha stamp.
                    float mask=saturate((.5-max(abs(local.x-.5),abs(local.y-.5)))*24);
                    float alpha=tread.a*mask;
                    result.rgb=lerp(result.rgb,tread.rgb,alpha);result.a=saturate(result.a+alpha);
                }
                return result;
            }
            ENDCG
        }
    }
}
