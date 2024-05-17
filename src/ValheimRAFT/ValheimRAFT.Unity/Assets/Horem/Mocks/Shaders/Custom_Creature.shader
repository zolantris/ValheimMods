Shader "JVLmock_Custom/Creature" {
	Properties {
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Color ("Color", Vector) = (1,1,1,1)
		_Hue ("Hue", Range(-0.5, 0.5)) = 0
		_Saturation ("Saturation", Range(-1, 1)) = 0
		_Value ("Value", Range(-1, 1)) = 0
		_Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
		[MaterialToggle] _UseGlossmap ("Use glossmap (Metal alpha)", Float) = 0
		_MetallicGlossMap ("Metal", 2D) = "white" {}
		_Metallic ("Metallic", Range(0, 1)) = 0
		_MetalGloss ("Metal smoothness", Range(0, 1)) = 0
		[HDR] _MetalColor ("Metal color", Vector) = (1,1,1,1)
		_EmissionMap ("Emissive (RGB)", 2D) = "white" {}
		[HDR] _EmissionColor ("Emissive color", Vector) = (0,0,0,1)
		_BumpScale ("Normal strength", Float) = 1
		_BumpMap ("Normal Map", 2D) = "bump" {}
		[MaterialToggle] _TwoSidedNormals ("Twosided normals", Float) = 0
		[MaterialToggle] _UseStyles ("Use styles", Float) = 0
		_Style ("Style", Float) = 0
		_StyleTex ("Style texture", 2D) = "white" {}
		[MaterialToggle] _AddRain ("Add Rain", Float) = 0
		[Enum(Off,0,Back,2)] _Cull ("Cull", Float) = 2
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