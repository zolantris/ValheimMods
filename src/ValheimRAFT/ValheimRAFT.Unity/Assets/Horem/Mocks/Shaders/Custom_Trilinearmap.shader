Shader "JVLmock_Custom/Trilinearmap" {
	Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
		_UVScale ("UV scale", Float) = 1
		_MetallicTex ("Metal map", 2D) = "white" {}
		_Metallic ("Metallic", Range(0, 1)) = 0
		_MetalGloss ("Metal gloss", Range(0, 1)) = 0
		_BumpScale ("Normal strength", Float) = 1
		_BumpMap ("Normal Map", 2D) = "bump" {}
		_RippleDistance ("Noise distance", Float) = 0
		_RippleFreq ("Noise freq", Float) = 1
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