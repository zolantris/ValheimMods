Shader "Custom/Vegetation" {
	Properties {
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Color ("Color", Vector) = (1,1,1,1)
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
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard
#pragma target 3.0

		sampler2D _MainTex;
		fixed4 _Color;
		struct Input
		{
			float2 uv_MainTex;
		};
		
		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
	Fallback "Diffuse"
}