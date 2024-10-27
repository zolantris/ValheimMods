Shader "Custom/SelectiveMask"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 0) // Adjustable color and transparency
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        ColorMask 0  // No color output, just stencil
        ZWrite On
        Cull Off

        // Stencil settings to read and ignore the masked area
        Stencil
        {
            Ref 9833 // Stencil reference value to compare against
            Comp Always // Write to the stencil buffer
            Pass Replace // Replace stencil value
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
