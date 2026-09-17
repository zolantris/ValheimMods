Shader "Custom/VehicleSailShader"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _MainColor ("Color", Color) = (1,1,1,1)

        _BumpMap ("Main Normal Map", 2D) = "bump" {}
        [HideInInspector]
        _MainNormal ("Legacy Main Normal Map", 2D) = "bump" {}

        _Wet ("Wet", Range(0,1)) = 0.0

        _PatternTex ("Pattern", 2D) = "white" {}
        _PatternRotation ("Pattern Rotation", Range(0,1)) = 0.0
        _PatternColor ("Pattern Color", Color) = (1,1,1,1)
        _PatternNormal ("Pattern Normal Map", 2D) = "bump" {}

        _LogoTex ("Logo", 2D) = "white" {}
        _LogoRotation ("Logo Rotation", Range(0,1)) = 0.0
        _LogoNormal ("Logo Normal Map", 2D) = "bump" {}
        _LogoColor ("Logo Color", Color) = (1,1,1,1)

        _MistAlpha ("Mist Alpha", Range(0,1)) = 1.0

        _Glossiness ("Smoothness", Range(0,1)) = 0
        _Metallic ("Metallic", Range(0,1)) = 0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        _BumpScale ("Normal Scale", Float) = 1

        // Retained for old serialized materials.
        [HideInInspector]
        _TwoSidedNormals ("Legacy Twosided Normals", Float) = 0

        _SphereNormals ("Spherical Normal", Range(0,1)) = 0
        _SphereOffset ("Spherical Offset", Float) = 0

        _EmissiveTex ("Emissive (RGB)", 2D) = "white" {}
        [HDR]
        _EmissionColor ("Emissive", Color) = (0,0,0,0)

        _MossTex ("Moss (RGB)", 2D) = "white" {}
        _MossAlpha ("Moss Alpha", Range(0,1)) = 0
        _MossBlend ("Moss Texture Blend", Range(0,10)) = 0
        _MossNormal ("Moss Normal", Range(0,1)) = 0.5
        _MossTransition ("Moss Transition", Float) = 0.1

        [Toggle]
        _AddSnow ("Add Snow", Float) = 1

        [Toggle]
        _AddRain ("Add Rain", Float) = 1

        _Height ("Height", Float) = 15
        _SwaySpeed ("Sway Speed", Float) = 15
        _SwayDistance ("Sway Distance", Float) = 0.5

        _RippleSpeed ("Ripple Speed", Float) = 100
        _RippleDistance ("Ripple Distance", Float) = 0.5
        _RippleDeadzoneMin ("Ripple Deadzone Min", Range(0,10)) = 0.3
        _RippleDeadzoneMax ("Ripple Deadzone Max", Range(0,10)) = 2

        _PushDistance ("Push Distance", Float) = 0

        [Toggle]
        _PushClothMode ("Push Cloth Mode", Float) = 0

        [Toggle]
        _CamCull ("Near Camera Cull", Float) = 1

        /*
         * Use the simple ShaderLab enum rather than
         * UnityEngine.Rendering.CullMode reflection.
         *
         * It is maximally compatible with older importer/editor tooling.
         */
        [Enum(Off,0,Front,1,Back,2)]
        _Cull ("Cull", Float) = 2
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    sampler2D _MainTex;
    sampler2D _BumpMap;

    sampler2D _PatternTex;
    sampler2D _PatternNormal;

    sampler2D _LogoTex;
    sampler2D _LogoNormal;

    sampler2D _EmissiveTex;

    fixed4 _MainColor;
    fixed4 _PatternColor;
    fixed4 _LogoColor;
    fixed4 _EmissionColor;

    half _Glossiness;
    half _Metallic;
    half _MistAlpha;
    half _Cutoff;
    half _BumpScale;

    half _PatternRotation;
    half _LogoRotation;

    #define PI 3.14159265359

    struct Input
    {
        float2 uv_MainTex;
        float2 uv_PatternTex;
        float2 uv_LogoTex;

        /*
         * Only used for dithered mist fading.
         */
        float4 screenPos;
    };

    void vert(
        inout appdata_full v,
        out Input o)
    {
        UNITY_INITIALIZE_OUTPUT(Input, o);

        /*
         * MagicaCloth / SkinnedMeshRenderer supplies deformation.
         * Do not deform the sail again in the shader.
         */
    }

    inline float2 RotateUV(
        float2 uv,
        float angle,
        float2 pivot)
    {
        float s = sin(angle);
        float c = cos(angle);

        float2x2 rotation =
            float2x2(
                c, -s,
                s,  c);

        return
            mul(
                uv - pivot,
                rotation) +
            pivot;
    }

    inline float3 SampleNormal(
        sampler2D normalTexture,
        float2 uv,
        float scale)
    {
        float3 normal =
            UnpackNormal(
                tex2D(
                    normalTexture,
                    uv));

        normal.xy *=
            max(
                scale,
                0.0001);

        return normalize(normal);
    }

    inline float DitherNoise(
        float2 pixel)
    {
        /*
         * Cheap stable screen-space noise.
         *
         * Used instead of transparent alpha blending so front/back sail
         * surfaces retain proper depth behavior.
         */
        return frac(
            52.9829189 *
            frac(
                dot(
                    floor(pixel),
                    float2(
                        0.06711056,
                        0.00583715))));
    }

    void surf(
        Input IN,
        inout SurfaceOutputStandard o)
    {
        /*
         * --------------------------------------------------------
         * UVs
         * --------------------------------------------------------
         */

        float2 patternUV =
            RotateUV(
                IN.uv_PatternTex,
                _PatternRotation *
                2.0 *
                PI,
                float2(
                    0.0,
                    0.0));

        float2 logoUV =
            RotateUV(
                IN.uv_LogoTex,
                _LogoRotation *
                2.0 *
                PI,
                float2(
                    0.5,
                    0.5));

        /*
         * --------------------------------------------------------
         * Base textures
         * --------------------------------------------------------
         */

        fixed4 mainColor =
            tex2D(
                _MainTex,
                IN.uv_MainTex) *
            _MainColor;

        fixed4 patternSample =
            tex2D(
                _PatternTex,
                patternUV);

        fixed4 logoSample =
            tex2D(
                _LogoTex,
                logoUV);

        /*
         * --------------------------------------------------------
         * Physical cloth cutout
         * --------------------------------------------------------
         */

        clip(
            mainColor.a -
            _Cutoff);

        /*
         * --------------------------------------------------------
         * Mist fade
         * --------------------------------------------------------
         *
         * Do not turn this material into a transparent blended material.
         *
         * Transparent blending is what allowed the coincident front/back
         * surfaces to bleed through one another at shallow viewing angles.
         */

        if (_MistAlpha < 0.9999)
        {
            float inverseW =
                1.0 /
                max(
                    IN.screenPos.w,
                    0.00001);

            float2 screenUV =
                IN.screenPos.xy *
                inverseW;

            float2 pixel =
                screenUV *
                _ScreenParams.xy;

            float threshold =
                DitherNoise(
                    pixel);

            clip(
                _MistAlpha -
                threshold);
        }

        /*
         * --------------------------------------------------------
         * Color masks
         * --------------------------------------------------------
         */

        half patternMask =
            saturate(
                dot(
                    patternSample.rgb,
                    float3(
                        0.2126,
                        0.7152,
                        0.0722))) *
            _PatternColor.a;

        half logoMask =
            saturate(
                logoSample.a *
                _LogoColor.a);

        /*
         * --------------------------------------------------------
         * Color
         * --------------------------------------------------------
         */

        fixed3 patternRGB =
            patternSample.rgb *
            _PatternColor.rgb;

        fixed3 logoRGB =
            logoSample.rgb *
            _LogoColor.rgb;

        fixed3 albedo =
            lerp(
                mainColor.rgb,
                patternRGB,
                patternMask);

        albedo =
            lerp(
                albedo,
                logoRGB,
                logoMask);

        /*
         * --------------------------------------------------------
         * Normal maps
         * --------------------------------------------------------
         */

        float3 mainNormal =
            SampleNormal(
                _BumpMap,
                IN.uv_MainTex,
                _BumpScale);

        float3 patternNormal =
            SampleNormal(
                _PatternNormal,
                patternUV,
                _BumpScale);

        float3 logoNormal =
            SampleNormal(
                _LogoNormal,
                logoUV,
                _BumpScale);

        /*
         * Blend overlays toward their normal maps rather than simply adding
         * all three maps together everywhere.
         */
        float3 finalNormal =
            mainNormal;

        finalNormal =
            normalize(
                lerp(
                    finalNormal,
                    patternNormal,
                    patternMask));

        finalNormal =
            normalize(
                lerp(
                    finalNormal,
                    logoNormal,
                    logoMask));

        /*
         * --------------------------------------------------------
         * Surface output
         * --------------------------------------------------------
         */

        o.Albedo =
            saturate(
                albedo);

        o.Normal =
            finalNormal;

        o.Metallic =
            saturate(
                _Metallic);

        o.Smoothness =
            saturate(
                _Glossiness);

        o.Emission =
            tex2D(
                _EmissiveTex,
                IN.uv_MainTex).rgb *
            _EmissionColor.rgb;

        /*
         * We are not alpha blended.
         */
        o.Alpha = 1.0;
    }

    ENDCG

    SubShader
    {
        Tags
        {
            "Queue" = "AlphaTest"
            "RenderType" = "TransparentCutout"
            "IgnoreProjector" = "True"
        }

        LOD 200

        /*
         * V7+ C# builds two submeshes:
         *
         * front:
         *   normal triangle winding
         *
         * back:
         *   reversed triangle winding
         *
         * Both use Cull Back.
         *
         * This means only one of the two coincident surfaces is rendered
         * from either viewing direction.
         */
        Cull [_Cull]

        /*
         * Essential for fixing the apparent see-through behavior.
         */
        ZWrite On

        /*
         * No SrcAlpha/OneMinusSrcAlpha.
         */
        Blend Off

        CGPROGRAM

        #pragma target 3.0

        #pragma surface surf Standard \
            fullforwardshadows \
            vertex:vert \
            addshadow

        ENDCG
    }

    Fallback "Diffuse"
}