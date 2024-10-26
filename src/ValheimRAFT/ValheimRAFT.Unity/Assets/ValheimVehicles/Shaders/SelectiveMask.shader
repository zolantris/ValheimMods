Shader "Custom/SelectiveMask"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ColorMask 0 // Disables color output
        ZWrite On // Writes to the depth buffer

        // Stencil settings for marking the mask area
        Stencil
        {
            Ref 1 // Stencil reference value to mark mask area
            Comp Always // Always write to the stencil buffer
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

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag() : SV_Target
            {
                return fixed4(0, 0, 0, 0); // Output no color
            }
            ENDCG
        }
    }
}
