Shader "Custom/VisibleStandardShaderWithTransparency"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _DoubleSided ("Double Sided", Float) = 0 // 0 = False, 1 = True
        _SrcBlend ("Source Blend", Float) = 1 // Default to Alpha
        _DstBlend ("Destination Blend", Float) = 10 // Default to One Minus Src Alpha
        _ZWrite ("Z Write", Float) = 0 // Default to Off
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }

        // Double-Sided Rendering
        Cull {
            if (_DoubleSided == 1) 
                Cull Off 
            else 
                Cull Back
        }

        // Blend settings for transparency
        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]

        Pass
        {
            Stencil
            {
                Ref 1
                Comp Equal
                Pass Keep
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = _Color; // Pass color to fragment
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv);
                return texColor * i.color; // Multiply texture color by vertex color
            }
            ENDCG
        }
    }
}
