#ifndef TOW_TREE_FADE_INCLUDED
#define TOW_TREE_FADE_INCLUDED

// Pixel coordinates keep the pattern stable across the depth and color passes.
void TowTreeFade_float(float Alpha, float Fade, float4 PixelPosition, out float Out)
{
    Out = Alpha;
#if !defined(SHADERPASS) || !defined(SHADERPASS_SHADOWCASTER) || SHADERPASS != SHADERPASS_SHADOWCASTER
    if (Fade > 0.0)
    {
        float2 pixel = floor(PixelPosition.xy);
        float threshold = frac(52.9829189 * frac(dot(pixel, float2(0.06711056, 0.00583715))));
        clip(threshold - Fade);
    }
#endif
}

#endif
