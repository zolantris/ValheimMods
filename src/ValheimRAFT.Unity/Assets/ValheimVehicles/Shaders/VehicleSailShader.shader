Shader "Custom/VehicleSailShader"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _MainColor ("Color", Color) = (1,1,1,1)
        _MainColorNormal ("Main Normal Map", 2D) = "bump" {}

        _Wet ("Wet", Range(0,1)) = 0.0

        _PatternTex ("Pattern", 2D) = "white" {}
        _PatternRotation("Pattern Rotation", Range(0, 1)) = 0.0
        _PatternColor ("Pattern Color", Color) = (1,1,1,1)
        _PatternNormal ("Pattern Normal Map", 2D) = "bump" {}

        _LogoTex ("Logo", 2D) = "white" {}
        _LogoRotation("Logo Rotation", Range(0, 1)) = 0.0
        _LogoNormal ("Logo Normal Map", 2D) = "bump" {}
        _LogoColor ("Logo Color", Color) = (1,1,1,1)

        _MistAlpha("Mist Alpha", Range(0, 1)) = 0.5

        _Glossiness ("Smoothness", Range(0, 1)) = 0
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        _BumpScale ("Normal scale", Float) = 1
        [MaterialToggle] _TwoSidedNormals ("Twosided normals", Float) = 1
        _SphereNormals ("Spherical Normal", Range(0, 1)) = 0
        _SphereOffset ("Spherical offset", Float) = 0
        _EmissiveTex ("Emissive (RGB)", 2D) = "white" {}
        [HDR] _EmissionColor ("Emissive", Vector) = (0,0,0,0)
        _MossTex ("Moss (RGB)", 2D) = "white" {}
        _MossAlpha ("Moss alpha", Range(0, 1)) = 0
        _MossBlend ("Moss texture blend", Range(0, 10)) = 0
        _MossNormal ("MossNormal", Range(0, 1)) = 0.5
        _MossTransition ("MossTransition", Float) = 0.1
        [MaterialToggle] _AddSnow ("Add Snow", Float) = 1
        [MaterialToggle] _AddRain ("Add Rain", Float) = 1
        _Height ("Height", Float) = 15
        _SwaySpeed ("SwaySpeed", Float) = 15
        _SwayDistance ("SwayDistance", Float) = 0.5
        _RippleSpeed ("Ripple speed", Float) = 100
        _RippleDistance ("Ripple distance", Float) = 0.5
        _RippleDeadzoneMin ("Ripple deadzone min", Range(0, 10)) = 0.3
        _RippleDeadzoneMax ("Ripple deadzone max", Range(0, 10)) = 2
        _PushDistance ("Push distance", Float) = 0
        [MaterialToggle] _PushClothMode ("Push cloth mode", Float) = 0
        [MaterialToggle] _CamCull ("Near camera cull", Float) = 1
        [Enum(Off,0,Back,2)] _Cull ("Cull", Float) = 0
    }

    CGINCLUDE
    #include <UnityPBSLighting.cginc>
    sampler2D _MainTex;
    sampler2D _MainNormal;

    float _Cutoff;

    #define PI 3.14159265359

    half _Wet;
    half _Glossiness;
    half _MistAlpha;
    fixed4 _MainColor;

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
        half _MistAlpha;
        float2 uv_PatternTex;
        float2 uv_LogoTex;
        float facing : VFACE;
    };

    void vert(inout appdata_full v, out Input o)
    {
        UNITY_INITIALIZE_OUTPUT(Input, o);
    }


    void surf(Input IN, inout SurfaceOutputStandard o)
    {
        float s = sin(_PatternRotation * PI * 2);
        float c = cos(_PatternRotation * PI * 2);
        float2x2 rotationMatrix = float2x2(c, -s, s, c);
        IN.uv_PatternTex = mul(IN.uv_PatternTex, rotationMatrix);

        s = sin(_LogoRotation * PI * 2);
        c = cos(_LogoRotation * PI * 2);
        rotationMatrix = float2x2(c, -s, s, c);
        IN.uv_LogoTex = mul(IN.uv_LogoTex, rotationMatrix);
        IN.uv_LogoTex.xy += 0.5;

        // Albedo comes from a texture tinted by color
        fixed4 mainCol = tex2D(_MainTex, IN.uv_MainTex) * _MainColor;
        fixed4 patternCol = tex2D(_PatternTex, IN.uv_PatternTex);
        fixed4 logoCol = tex2D(_LogoTex, IN.uv_LogoTex);

        o.Albedo = lerp(mainCol.rgb, patternCol.rgb * _PatternColor.rgb,
                    patternCol.r * _PatternColor.a);
        o.Albedo = lerp(o.Albedo, logoCol.rgb * _LogoColor.rgb,
                            logoCol.a * _LogoColor.a);
        // Metallic and smoothness come from slider variables
          float3 normal = UnpackNormal(tex2D(_MainNormal, IN.uv_MainTex)) + 
                        UnpackNormal(tex2D(_PatternNormal, IN.uv_PatternTex)) + 
                        UnpackNormal(tex2D(_LogoNormal, IN.uv_LogoTex));


        o.Normal = normal * (IN.facing > 0 ? 1 : -1);

        
        o.Metallic = _Glossiness;
        o.Smoothness = _Wet;

        o.Alpha = _MainColor.a * _MistAlpha;
        clip(o.Alpha - _Cutoff);
    }
    ENDCG
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent" "RenderType" = "Transparent"
        }
        LOD 200
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        CGPROGRAM
        #pragma surface surf Standard alpha:fade vertex:vert
        #pragma vertex vert
        #pragma target 5.0
        ENDCG
    }
    Fallback "Diffuse"
}