Shader "JVLmock_Custom/Water" {
	Properties {
		_ColorTop ("Color top", Vector) = (1,1,1,1)
		_ColorBottom ("Color bottom deep", Vector) = (1,1,1,1)
		_ColorBottomShallow ("Color bottom shallow", Vector) = (1,1,1,1)
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
		_Metallic ("Metallic", Range(0, 1)) = 0
		_FoamTex ("Foam", 2D) = "white" {}
		_FoamHighTex ("Foam high", 2D) = "white" {}
		_RandomFoamTex ("Random Foam", 2D) = "white" {}
		_Normal ("Normalmap", 2D) = "bump" {}
		_NormalFine ("NormalFine", 2D) = "bump" {}
		_NormalPower ("NormalPower", Range(0, 5)) = 1
		_NormalScale ("NormalScale", Float) = 1
		_WaveVel ("WaveVel", Float) = 1
		_DepthFade ("DepthFade", Float) = 1
		_ShoreFade ("ShoreFade", Float) = 1
		_FoamDepth ("FoamDepth", Float) = 0.05
		_FoamColor ("FoamColor", Vector) = (1,1,1,1)
		_WaveFoam ("wave foam", Float) = 0.8
		_RefractionScale ("Refraction Scale", Float) = 1
		_RefractionMax ("Refraction Max", Float) = 0.01
		_SurfaceColor ("SurfaceColor", Vector) = (1,1,1,1)
		_Tess ("Tessellation", Range(1, 32)) = 4
		[MaterialToggle] _IsLod ("Is lod", Float) = 0
		_VisibleMaxDistance ("Lod distance", Float) = 0
		_WaterEdge ("World water edge", Float) = 9100
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType" = "Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard
#pragma target 3.0

		struct Input
		{
			float2 uv_MainTex;
		};

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			o.Albedo = 1;
		}
		ENDCG
	}
	Fallback "Diffuse"
}