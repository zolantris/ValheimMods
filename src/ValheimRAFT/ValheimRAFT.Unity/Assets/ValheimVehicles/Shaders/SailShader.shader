Shader "Custom/SailShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _MainNormal ("Normal", 2D) = "" {}
        _MainColor ("Color", Color) = (1,1,1,1)

        _Wet ("Wet", Range(0,1)) = 0.0

        _PatternTex ("Pattern", 2D) = "white" {}
        _PatternRotation("Pattern Rotation", Range(0, 1)) = 0.0
        _PatternColor ("Pattern Color", Color) = (1,1,1,1)
        
        _LogoTex ("Logo", 2D) = "white" {}
        _LogoRotation("Logo Rotation", Range(0, 1)) = 0.0
        _LogoColor ("Logo Color", Color) = (1,1,1,1)
    }

    CGINCLUDE
    #include <UnityPBSLighting.cginc>
    sampler2D _MainTex;
    sampler2D _MainNormal;
    fixed4 _MainColor;

    half _Wet;
    fixed4 _Color;

    sampler2D _PatternTex;
    sampler2D _PatternNormal;
    fixed4 _PatternColor;
    half _PatternRotation;

    sampler2D _LogoTex;
    sampler2D _LogoNormal;
    fixed4 _LogoColor;
    half _LogoRotation;

    struct Input
    {
        float2 uv_MainTex;
        float2 uv_PatternTex;
        float2 uv_LogoTex;
    };

    // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
    // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
    // #pragma instancing_options assumeuniformscaling
    UNITY_INSTANCING_BUFFER_START(Props)
        // put more per-instance properties here
    UNITY_INSTANCING_BUFFER_END(Props)

    void vert(inout appdata_full v, out Input o) {
        UNITY_INITIALIZE_OUTPUT(Input, o);
        v.normal = -v.normal;
    }

    void surf (Input IN, inout SurfaceOutputStandard o)
    {
        float s = sin ( _PatternRotation * 3.14159265359 * 2 );
        float c = cos ( _PatternRotation * 3.14159265359 * 2 );
        float2x2 rotationMatrix = float2x2( c, -s, s, c);
        IN.uv_PatternTex = mul (IN.uv_PatternTex, rotationMatrix);

        s = sin ( _LogoRotation * 3.14159265359 * 2 );
        c = cos ( _LogoRotation * 3.14159265359 * 2 );
        rotationMatrix = float2x2( c, -s, s, c);
        IN.uv_LogoTex = mul (IN.uv_LogoTex, rotationMatrix);
        IN.uv_LogoTex.xy += 0.5;

        // Albedo comes from a texture tinted by color
        fixed4 mainCol = tex2D (_MainTex, IN.uv_MainTex) * _Color * _MainColor;
        fixed4 patternCol = tex2D (_PatternTex, IN.uv_PatternTex);
        fixed4 logoCol = tex2D (_LogoTex, IN.uv_LogoTex);

        o.Albedo = lerp(mainCol.rgb, patternCol.rgb * _PatternColor.rgb, patternCol.r * _PatternColor.a);
        o.Albedo = lerp(o.Albedo, logoCol.rgb * _LogoColor.rgb, logoCol.a * _LogoColor.a);
        o.Albedo = o.Albedo;
        // Metallic and smoothness come from slider variables
        o.Normal = UnpackNormal(tex2D(_MainNormal, IN.uv_MainTex));// + tex2D(_PatternNormal, IN.uv_PatternTex) + tex2D(_LogoNormal, IN.uv_LogoTex);
        o.Metallic = 0;
        o.Smoothness = _Wet;
        o.Alpha = 1;
    }
    ENDCG

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Opaque" }
        FOG { mode Global }
        LOD 200
        Cull Back

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 4.0

        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        ENDCG

        Tags { "Queue" = "Transparent" "RenderType" = "Opaque" }
        FOG { mode Global }
        LOD 200
        Cull Front
        

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows
        #pragma vertex vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 4.0

        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        ENDCG
    }

    FallBack "Diffuse"
}
