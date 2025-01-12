Shader "Custom/TransparentShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 0)
    }
    SubShader
    {
        ColorMask 0
        Tags { "RenderType"="Transparent" }
            Stencil
            {
                Ref 2
                Comp Always // Only render where stencil is 1
                Pass IncrSat
            }
        Blend SrcAlpha OneMinusSrcAlpha
        ZClip Off

        Pass
        {
            Stencil
            {
                Ref 1
                Comp Equal // Only render where stencil is 1
                Pass Replace
            }
            ZTest Always
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            sampler2D _MainTex;
            fixed4 _Color;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color * tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
