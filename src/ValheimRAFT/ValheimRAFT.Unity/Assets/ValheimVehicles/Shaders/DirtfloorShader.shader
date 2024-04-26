Shader "Custom/DirtfloorShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _MainNormal ("Normal Map", 2D) = "" {}
        _MainRotation("Rotation", Float) = 0
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        [MaterialToggle] _AddRain ("Add Rain", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        FOG { mode Global }

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows
        #pragma vertex vert
        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 4.0

        sampler2D _MainTex;
        sampler2D _MainNormal;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        float _MainRotation;
        float _AddRain;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert(inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o);

            float s = sin ( -_MainRotation * 3.14159265359 * 2 );
            float c = cos ( -_MainRotation * 3.14159265359 * 2 );
            float2x2 rotationMatrix = float2x2( c, -s, s, c);
            v.texcoord.xy -= 0.5;
            v.texcoord.xy = mul (v.texcoord.xy, rotationMatrix);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 col = tex2D (_MainTex, IN.uv_MainTex) * _Color;

            // Darken with rain
            o.Albedo = col.rgb * (1 - (_AddRain * 0.50));

            o.Metallic = _Metallic;
            o.Normal = UnpackNormal (tex2D(_MainNormal, IN.uv_MainTex));

            float s = sin(-_MainRotation * 3.14159265359 * 2);
            float c = cos(-_MainRotation * 3.14159265359 * 2);

            o.Normal = mul(o.Normal, float3x3(
                c, s, 0,
                -s, c, 0,
                0, 0, 1
            ));

            // Shininess with rain
            o.Smoothness = _AddRain * _Glossiness;
            o.Alpha = col.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
