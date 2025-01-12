Shader "JVLmock_Custom/Heightmap" {
	Properties {
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_ClearedMaskTex ("ClearedMask", 2D) = "white" {}
		_DiffuseTex0 ("Diffuse0", 2D) = "white" {}
		_NormalTex0 ("Normal0", 2D) = "white" {}
		_GrassNormal ("Grass normal", 2D) = "bump" {}
		_DirtNormal ("Dirt normal", 2D) = "bump" {}
		_ForestNormal ("Forest normal", 2D) = "bump" {}
		_CultivatedNormal ("Cultivated normal", 2D) = "bump" {}
		_PavedNormal ("Paved road normal", 2D) = "bump" {}
		_SnowNormal ("Snow normal", 2D) = "bump" {}
		_SnowGloss ("Snow Smoothness", Range(0, 1)) = 0.5
		_CliffNormal ("Cliff normal", 2D) = "bump" {}
		_MistlandsCliffNormal ("Mistlands Cliff normal", 2D) = "bump" {}
		_RockNormal ("Rock normal", 2D) = "bump" {}
		_RockGloss ("Rock Smoothness", Range(0, 1)) = 0.5
		_WaterLevel ("Water level", Float) = 0
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
		_Metallic ("Metallic", Range(0, 1)) = 0
		_BumpScale ("Normal scale", Float) = 1
		_UVScale ("UVScale", Float) = 0.5
		_LodHideDistance ("Lod hide distance", Float) = 0
		[Toggle] _IsDistantLod ("Distant lod", Float) = 0
		_NoiseTex ("Noise", 2D) = "white" {}
		_Tess ("Tessellation", Range(1, 32)) = 4
		_Displacement ("Displacement", Float) = 0.2
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200
		CGPROGRAM
#pragma surface surf Standard
#pragma target 3.0

		sampler2D _MainTex;
		struct Input
		{
			float2 uv_MainTex;
		};

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}
	Fallback "Diffuse"
}