Shader "Custom/VehicleSailShader"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _MainColor ("Color", Color) = (1,1,1,1)
        _MainNormal ("Main Normal Map", 2D) = "bump" {}

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
    #pragma surface surf Standard alpha:fade vertex:vert addshadow
    #pragma target 3.0

    #include "UnityCG.cginc"
    #include "UnityPBSLighting.cginc"

    // Textures
    sampler2D _MainTex, _MainNormal;
    sampler2D _PatternTex, _PatternNormal;
    sampler2D _LogoTex, _LogoNormal;
    sampler2D _EmissiveTex;

    // Material params
    fixed4 _MainColor, _PatternColor, _LogoColor, _EmissionColor;
    half _Wet, _Glossiness, _Metallic, _MistAlpha, _Cutoff, _BumpScale,
         _TwoSidedNormals;
    half _PatternRotation, _LogoRotation;
    half _SphereNormals, _SphereOffset;

    #define PI 3.14159265359

    struct Input
    {
        float2 uv_MainTex;
        float2 uv_PatternTex;
        float2 uv_LogoTex;
        float facing : VFACE; // front=+1, back=-1
    };

    void vert(inout appdata_full v, out Input o)
    {
        UNITY_INITIALIZE_OUTPUT(Input, o);
        // (vertex motion hooks exist but are not applied here)
    }

    inline float2 RotateUV(float2 uv, float angle, float2 pivot)
    {
        float s = sin(angle), c = cos(angle);
        float2x2 R = float2x2(c, -s, s, c);
        return mul(uv - pivot, R) + pivot;
    }

    inline float3 UnpackNormalSafe(fixed4 nrm, float scale)
    {
        return UnpackNormal(nrm) * max(scale, 0.0001);
    }


    void surf(Input IN, inout SurfaceOutputStandard o)
    {
        // --- UV rotations ---
        float2 uvPat = RotateUV(IN.uv_PatternTex, _PatternRotation * 2 * PI,
                            float2(0, 0));
        float2 uvLogo = RotateUV(IN.uv_LogoTex, _LogoRotation * 2 * PI,
                                                float2(0.5, 0.5));

        // --- Samples ---
        fixed4 mainCol = tex2D(_MainTex, IN.uv_MainTex) * _MainColor;
        // base color & alpha owner
        fixed4 patternCol = tex2D(_PatternTex, uvPat);
        fixed4 logoCol = tex2D(_LogoTex, uvLogo);

        // --- Color tints ---
        fixed3 patRGB = patternCol.rgb * _PatternColor.rgb;
        fixed3 logoRGB = logoCol.rgb * _LogoColor.rgb;

        // --- Masks for COLOR mixing (pattern doesn’t affect final alpha) ---
        // Pattern: use brightness so black => 0 (shows base), white => 1 (shows pattern)
        half mPattern = saturate(dot(patternCol.rgb,
                                         float3(0.2126, 0.7152, 0.0722))) *
            _PatternColor.a;
        // Logo: typically from PNG with alpha
        half mLogo = saturate(logoCol.a * _LogoColor.a);

        // Optional: gate overlays by base opacity (avoid tinting fully transparent pixels)
        mPattern *= saturate(mainCol.a);
        mLogo *= saturate(mainCol.a);

        // --- Compose RGB ---
        fixed3 albedo = lerp(mainCol.rgb, patRGB, mPattern);
        albedo = lerp(albedo, logoRGB, mLogo);
        albedo = saturate(albedo);

        // --- Normals (additive blend, renormalized), two-sided flip ---
        float3 nMain = UnpackNormalSafe(tex2D(_MainNormal, IN.uv_MainTex),
                           _BumpScale);
        float3 nPat =
            UnpackNormalSafe(tex2D(_PatternNormal, uvPat), _BumpScale);
        float3 nLogo = UnpackNormalSafe(tex2D(_LogoNormal, uvLogo), _BumpScale);
        float3 nTan = normalize(nMain + nPat + nLogo);
        if (_TwoSidedNormals > 0.5 && IN.facing < 0.5) nTan = -nTan;

        // --- Outputs ---
        o.Albedo = albedo;
        o.Normal = nTan;
        o.Metallic = saturate(_Metallic);
        o.Smoothness = saturate(_Glossiness);
        o.Emission = tex2D(_EmissiveTex, IN.uv_MainTex).rgb * _EmissionColor.
            rgb;

        // Final alpha comes from MAIN only (pattern doesn’t punch holes)
        half alphaMain = saturate(mainCol.a * _MistAlpha);
        o.Alpha = alphaMain;
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
        #pragma surface surf Standard alpha:cutoff vertex:vert
        #pragma vertex vert
        #pragma target 5.0
        ENDCG
    }
    Fallback "Diffuse"
}