Shader "Custom/ShadowCasterOnly"
{
    SubShader
    {
        // Don't render the main pass (ColorMask 0 means no color writes)
        Tags { "RenderType"="Opaque" }
        ColorMask 0
        ZWrite On

        // Include shadow caster pass so it still casts shadows
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ColorMask 0
        }
    }
}
