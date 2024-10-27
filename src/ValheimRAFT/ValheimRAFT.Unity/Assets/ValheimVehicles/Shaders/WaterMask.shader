Shader "Custom/WaterMask"
{
    Properties
    {
        _Color("Color", Color) = (0, 0, 1, 0.5) // Semi-transparent blue
    }
    SubShader
    {
        Tags { "RenderType"="Cutout" }
        ZWrite On
        ZTest LEqual
        ColorMask 0
        Cull Off // Cull back faces for inside-out effect

        Stencil
        {
            Ref 1 // Set stencil reference value
            Comp Always // Always write to stencil buffer
            Pass Replace // Replace the stencil buffer value with Ref
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            float4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color; // Not actually used, but needed for the pass
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
