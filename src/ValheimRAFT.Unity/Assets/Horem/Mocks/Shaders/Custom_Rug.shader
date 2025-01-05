Shader "JVLmock_Custom/Rug" {
	Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
		_Metallic ("Metallic", Range(0, 1)) = 0
		_BumpMap ("Normal Map", 2D) = "bump" {}
		_BumpScale ("Normal intensity", Float) = 1
		_EmissionColor ("Emissive", Vector) = (0,0,0,0)
		_ZFadeDistance ("Fade far", Float) = 0.3
		_ZFadeDistanceNear ("Fade near", Float) = 0
		_Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
		[Enum(Off,0,On,1)] _ZWrite ("ZWrite", Float) = 1
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
}