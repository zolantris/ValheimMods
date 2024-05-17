Shader "JVLmock_Custom/Clouds" {
	Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Normal ("Normalmap", 2D) = "bump" {}
		_NormalPower ("NormalPower", Float) = 1
		_LightNormalFactor ("Light normal factor", Range(0, 1)) = 0
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
		_Metallic ("Metallic", Range(0, 1)) = 0
		_FogAmount ("Fog amount", Range(0, 1)) = 0.5
		_UVScale ("Scale", Float) = 0.1
		_Opacity ("Opacity", Float) = 1
		_Speed ("Speed", Float) = 1
		_Darkening ("Darkening", Range(0, 2)) = 1
		_Rain ("Rain", Range(0, 1)) = 1
		_RainFogAmount ("Rain fog amount", Range(0, 1)) = 0.5
		_RainUVScale ("Rain Scale", Float) = 0.1
		_RainOpacity ("Rain Opacity", Float) = 1
		_RainSpeed ("Rain Speed", Float) = 1
		_RainDarkening ("Rain Darkening", Range(0, 1)) = 1
		_RainColor ("Rain Color", Vector) = (1,1,1,1)
		_RainTex ("Rain tex (RGB)", 2D) = "white" {}
		_RainNormal ("Rain Normalmap", 2D) = "bump" {}
		_RainNormalPower ("NormalPower", Float) = 1
		_ZFadeDistance ("Soft particle fade distance", Float) = 0.3
		_CameraFadeDistanceMin ("Camera fade distance min", Float) = 0
		_CameraFadeDistanceMax ("Camera fade distance max", Float) = 0.1
		_RefractionNormal ("Refraction normal", 2D) = "bump" {}
		_RefractionScale ("Refraction scale", Float) = 1
		_RefractionIntensity ("Refraction intensity", Float) = 1
		_RefractionSpeed ("Refraction speed", Float) = 1
		_FaceDir ("Face Dir", Float) = 1
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