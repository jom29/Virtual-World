Shader "Custom/ReceiveShadowsOnly"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags {"RenderType"="Opaque"}
        CGPROGRAM
        #pragma surface surf NoLighting addshadow

        fixed4 _Color;

        struct Input {
            float3 worldPos;
        };

        void surf(Input IN, inout SurfaceOutput o)
        {
            o.Albedo = _Color.rgb;
            o.Alpha = 1;
        }

        // NoLighting: disables light contribution but allows shadow attenuation
        inline fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten)
        {
            return fixed4(s.Albedo * atten, 1); // applies shadows only
        }
        ENDCG
    }
    FallBack "Diffuse"
}
