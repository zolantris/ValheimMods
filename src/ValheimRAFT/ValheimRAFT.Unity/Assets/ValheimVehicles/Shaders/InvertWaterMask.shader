Shader "Custom/MaskedMaterial"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 0.5) // Adjustable color and transparency
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        ColorMask RGBA
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha // Alpha blending for transparency

        // Stencil settings to read and ignore the masked area
        Stencil
        {
            Ref 1 // Stencil reference value to compare against
            Comp NotEqual // Render only if stencil buffer does not match Ref
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
                return _Color; // Render the specified color outside the mask volume
            }
            ENDCG
        }
    }
}
