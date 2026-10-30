Shader "Living Mud/StampToTexture"
{
    Properties
    {
        _Tex("Input Texture", 2D) = "white" {}
        _Tex2("Output Texture", 2D) = "black" {}
        
        [MaterialToggle] _Vignette("Vignette", Float) = 1.0
        _PosX("Position X", Float) = .5
        _PosY("Position Y", Float) = .5
        _Scale("Scale", Float) = 1.0
        [MaterialToggle] _FlipX("Flip X", Float) = 0.0
        [MaterialToggle] _FlipY("Flip Y", Float) = 0.0
        _Degrees("Degrees", Float) = 0.0
        _Bend("Bend", Float) = 0
        _Direction("Bend Direction", Float) = 0
     }

     SubShader
     {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Lighting Off
        Blend One Zero

        Pass
        {
            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0
            
            sampler2D   _Tex;
            sampler2D   _Tex2;
            bool        _Vignette;
            float       _PosX;
            float       _PosY;
            float       _Scale;
            bool        _FlipX;
            bool        _FlipY;
            float       _Degrees;
            float       _Bend;
            float       _Direction;

            float4 frag(v2f_customrendertexture IN) : COLOR
            {
                float2 uv = IN.localTexcoord.xy;

                float flipXCorrection = 1.0;
                float flipYCorrection = 1.0;

                //Flip
                if (_FlipX){
                    uv.x = 1.0 - uv.x;
                    flipXCorrection = -1.0;
                }

                if (_FlipY){
                    uv.y = 1.0 - uv.y;
                    flipYCorrection = -1.0;
                    }

                float2 position = float2(_PosX, _PosY);
                float2 correction = float2(0.5, 0.5);

                //Translation
                uv = ((correction - position) * float2(flipXCorrection, flipYCorrection))+uv;

                //Scale
                float scale = (1.0 / _Scale);
                uv = (uv - correction) * scale;
                uv = (uv + correction);
                float2 scaledUV = uv;

                //Rotation
                float rotation = _Degrees * -1.0;
                rotation = rotation * (3.1415/180.0);
                uv -= correction;
                float s = sin(rotation);
                float c = cos(rotation);
                float2x2 rotationMatrix = float2x2(c, -s, s, c);
                rotationMatrix *= 0.5;
                rotationMatrix += 0.5;
                rotationMatrix = rotationMatrix * 2 - 1;
                uv = mul(uv, rotationMatrix);
                uv += correction;

                //Bend the stamp
                float2 center = float2(.5, 1);
                float2 offset = float2(0,0);
                float2 centerOffset = uv - center;

                float distanceFromCenter = length(centerOffset);
                float angle = atan2(centerOffset.y, centerOffset.x) + radians(_Direction);
                float distortionAmount = _Bend * pow(distanceFromCenter, 2);
                uv += + distortionAmount * float2(cos(angle), sin(angle));

                //Vignette Mask the stamp              
                float2 maskUV = scaledUV + float2(-0.5, -0.5);
                float maskValue = length(maskUV);
                maskValue = 1.0 - maskValue;
                maskValue = smoothstep(0.5, 0.7, maskValue);

                uv = clamp(uv, 0.0, 1.0);

                //Get the background color from the output Texture
                fixed4 bkgdCol = tex2D(_Tex2, IN.localTexcoord.xy);

                fixed4 stampCol = tex2D(_Tex, clamp(uv, 0.0, 1.0));
                
                //Apply the vignette mask or not
                if (_Vignette){
                    stampCol.w *= maskValue;
                }

                fixed4 col = lerp(bkgdCol, stampCol, stampCol.w);
                
                float alpha = clamp(bkgdCol.w + stampCol.w, 0.0, 1.0);
                col = fixed4(col.x, col.y, col.z, alpha);

                return col;
            }
            ENDCG
            }
    }
}