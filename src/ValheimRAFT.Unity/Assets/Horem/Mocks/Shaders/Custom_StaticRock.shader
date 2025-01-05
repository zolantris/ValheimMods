Shader "JVLmock_Custom/StaticRock" {
	Properties {
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Color ("Color", Vector) = (1,1,1,1)
		_GlossMap ("Glossmap (RGB)", 2D) = "white" {}
		[MaterialToggle] _UseGlossMap ("Use glossmap", Float) = 0
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
		_MetalTex ("Metal", 2D) = "white" {}
		_Metallic ("Metallic", Range(0, 1)) = 0
		_MetalGloss ("Metal smoothness", Range(0, 1)) = 0
		_MossTex ("Moss (RGB)", 2D) = "white" {}
		_MossAlpha ("Moss alpha", Range(0, 1)) = 1
		_MossBlend ("Moss texture blend", Range(0, 10)) = 0
		_MossColor ("Moss Cclor", Vector) = (1,1,1,1)
		_MossGloss ("Moss gloss", Range(0, 1)) = 0
		_MossNormal ("MossNormal", Range(0, 1)) = 0.5
		_MossTransition ("MossTransition", Float) = 0.1
		_EmissiveTex ("Emissive (RGB)", 2D) = "white" {}
		[HDR] _EmissionColor ("Emissive color", Vector) = (0,0,0,1)
		_BumpScale ("Normal strength", Float) = 1
		_BumpMap ("Normal Map", 2D) = "bump" {}
		_WaterlineOffset ("Waterline offset", Float) = 0
		[MaterialToggle] _TriplanarMap ("Use triplanar mapping", Float) = 0
		_TriplanarScale ("Triplanar scale", Float) = 1
		[MaterialToggle] _AddSnow ("Add Snow", Float) = 1
		[MaterialToggle] _AddRain ("Add Rain", Float) = 1
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
}