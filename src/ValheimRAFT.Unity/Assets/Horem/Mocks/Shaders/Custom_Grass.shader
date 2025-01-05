Shader "JVLmock_Custom/Grass" {
	Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_TerrainColorTex ("Terrain color (RGB)", 2D) = "white" {}
		_TerrainColorScale ("Terrain color scale", Float) = 0.01
		_BumpMap ("Normal Map", 2D) = "bump" {}
		_BumpScale ("Normal scale", Float) = 1
		_NormalHack ("Normal hack", Float) = 0
		[MaterialToggle] _TwoSidedNormals ("Twosided normals", Float) = 0
		_EmissiveTex ("Emissive (RGB)", 2D) = "white" {}
		[HDR] _EmissionColor ("Emissive", Vector) = (0,0,0,0)
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
		_Metallic ("Metallic", Range(0, 1)) = 0
		_Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
		_FadeDistanceMin ("FadeDistanceMin", Float) = 15
		_FadeDistanceMax ("FadeDistanceMax", Float) = 20
		_Height ("Height", Float) = 15
		_SwaySpeed ("SwaySpeed", Float) = 15
		_SwayDistance ("SwayDistance", Float) = 0.5
		_PushDistance ("Push distance", Float) = 0.5
		[MaterialToggle] _DistanceScale ("Distance scale", Float) = 1
		[MaterialToggle] _CamCull ("Near camera cull", Float) = 1
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