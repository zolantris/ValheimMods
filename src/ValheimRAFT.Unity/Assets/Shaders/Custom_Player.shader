Shader "Custom/Player" {
	Properties {
		_Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
		_MainTex ("Skin (RGB)", 2D) = "white" {}
		_SkinBumpMap ("Skin bumpMap", 2D) = "bump" {}
		_SkinColor ("Skin color", Vector) = (1,1,1,1)
		_ChestTex ("Chest (RGB)", 2D) = "none" {}
		_ChestBumpMap ("Chest bumpMap", 2D) = "bump" {}
		_ChestMetal ("Chest metal", 2D) = "black" {}
		_LegsTex ("Legs (RGB)", 2D) = "none" {}
		_LegsBumpMap ("Legs bumpMap", 2D) = "bump" {}
		_LegsMetal ("Legs metal", 2D) = "black" {}
		_BumpScale ("Normal scale", Float) = 1
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
		_MetalGlossiness ("Metal gloss", Range(0, 1)) = 0.5
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