Shader "JVLmock_Custom/Yggdrasil_root" {
	Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
		_Metallic ("Metallic", Range(0, 1)) = 0
		_Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
		_BumpMap ("Normal Map", 2D) = "bump" {}
		_BumpScale ("Normal scale", Float) = 1
		_MossTex ("Moss texture (RGB)", 2D) = "white" {}
		_MossAlpha ("Moss mask alpha", Range(0, 1)) = 1
		_MossBlend ("Moss texture blend", Range(0, 10)) = 0
		_MossColor ("Moss Cclor", Vector) = (1,1,1,1)
		_MossNormal ("MossNormal", Range(0, 1)) = 0.5
		_MossTransition ("MossTransition", Float) = 0.1
		_EmissiveTex ("Emissive (RGB)", 2D) = "white" {}
		_EmissiveMaskTex ("Emissive mask (RA)", 2D) = "white" {}
		_EmissBlend ("Emissive texture blend", Range(0, 10)) = 0
		_EmissiveColor ("Emissive color", Vector) = (1,1,1,1)
		_MossEmissive ("Moss emission", Float) = 1
		_MossEmissivePower ("Moss emission power", Float) = 1
		_EmissiveScroll ("Speed", Float) = 0
		_RefractionNormal ("Refraction normal", 2D) = "bump" {}
		_RefractionScale ("Refraction scale", Float) = 1
		_RefractionIntensity ("Refraction intensity", Float) = 1
		_RefractionSpeed ("Refraction speed", Float) = 1
		[MaterialToggle] _AddSnow ("Add Snow", Float) = 1
		[MaterialToggle] _AddRain ("Add Rain", Float) = 1
		[MaterialToggle] _TwoSidedNormals ("Twosided normals", Float) = 0
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