Shader "JVLmock_Custom/SkyObject" {
	Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
		_Metallic ("Metallic", Range(0, 1)) = 0
		_BumpScale ("Normal scale", Float) = 1
		_BumpMap ("Normal Map", 2D) = "bump" {}
		_RefractionNormal ("Refraction normal", 2D) = "bump" {}
		_RefractionScale ("Refraction scale", Float) = 1
		_RefractionIntensity ("Refraction intensity", Float) = 1
		_RefractionSpeed ("Refraction speed", Float) = 1
		_FogAmount ("Fog", Range(0, 1)) = 1
		_Speed ("Speed", Float) = 0
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