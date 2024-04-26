Shader "Custom/VehicleSailShader" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}

        _Wet ("Wet", Range(0,1)) = 0.0

        _PatternTex ("Pattern", 2D) = "white" {}
        _PatternRotation("Pattern Rotation", Range(0, 1)) = 0.0
        _PatternColor ("Pattern Color", Color) = (1,1,1,1)
        
        _LogoTex ("Logo", 2D) = "white" {}
        _LogoRotation("Logo Rotation", Range(0, 1)) = 0.0
        _LogoColor ("Logo Color", Color) = (1,1,1,1)
		
//		_MainTex ("Albedo (RGB)", 2D) = "white" {}
//		_Color ("Color", Vector) = (1,1,1,1)
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
		_Metallic ("Metallic", Range(0, 1)) = 0
		_Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
		_BumpMap ("Normal Map", 2D) = "bump" {}
		_BumpScale ("Normal scale", Float) = 1
		[MaterialToggle] _TwoSidedNormals ("Twosided normals", Float) = 0
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
    sampler2D _BumpMap;

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
    // UNITY_INSTANCING_BUFFER_START(Props)
    //     // put more per-instance properties here
    // UNITY_INSTANCING_BUFFER_END(Props)

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
    	// fixed4 mainCol = tex2D (_MainTex, IN.uv_MainTex) * _Color * _MainColor;
    	fixed4 mainCol = tex2D (_MainTex, IN.uv_MainTex) * _Color;
        fixed4 patternCol = tex2D (_PatternTex, IN.uv_PatternTex);
        fixed4 logoCol = tex2D (_LogoTex, IN.uv_LogoTex);

        o.Albedo = lerp(mainCol.rgb, patternCol.rgb * _PatternColor.rgb, patternCol.r * _PatternColor.a);
        o.Albedo = lerp(o.Albedo, logoCol.rgb * _LogoColor.rgb, logoCol.a * _LogoColor.a);
        o.Albedo = o.Albedo;
        // Metallic and smoothness come from slider variables
        o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));// + tex2D(_PatternNormal, IN.uv_PatternTex) + tex2D(_LogoNormal, IN.uv_LogoTex);
        o.Metallic = 0;
        o.Smoothness = _Wet;
        o.Alpha = mainCol.a;
    }
    void surfVeg (Input IN, inout SurfaceOutputStandard o)
	{
		fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
		o.Albedo = c.rgb;
		o.Alpha = c.a;
	}
    ENDCG

	//DummyShaderTextExporter
	SubShader{
		
        Tags { "Queue" = "Transparent" "RenderType" = "Opaque" }
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

//		Tags { "RenderType"="Opaque" }
//        LOD 200
//        CGPROGRAM
//		#pragma surface surfVeg Standard
//		#pragma target 3.0
//        ENDCG
	}
	Fallback "Diffuse"
}