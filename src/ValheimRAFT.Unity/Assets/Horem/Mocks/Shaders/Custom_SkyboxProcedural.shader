Shader "JVLmock_Custom/SkyboxProcedural" {
	Properties {
		[KeywordEnum(None, Simple, High Quality)] _SunDisk ("Sun", Float) = 2
		_SunSize ("Sun Size", Range(0, 1)) = 0.04
		_SunSizeConvergence ("Sun Size Convergence", Range(1, 10)) = 5
		_MoonTex ("Moon texture", 2D) = "white" {}
		_MoonTexScale ("Moon Tex scale", Float) = 1
		_MoonSize ("Moon Size", Range(0, 1)) = 0.04
		[HDR] _MoonColor ("Moon Tint", Vector) = (0.5,0.5,0.5,1)
		_AtmosphereThickness ("Atmosphere Thickness", Range(0, 5)) = 1
		_SkyTint ("Sky Tint", Vector) = (0.5,0.5,0.5,1)
		_GroundColor ("Ground", Vector) = (0.369,0.349,0.341,1)
		_Exposure ("Exposure", Range(0, 8)) = 1.3
		[NoScaleOffset] _StarFieldTex ("Starfield cubemap   (HDR)", Cube) = "black" {}
		_StarExposure ("Star exposure", Range(0, 2)) = 1
		_FogHeightMin ("Fog height min", Range(0, 1)) = 1
		_FogHeightMax ("Fog height max", Range(0, 1)) = 1
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
	//CustomEditor "SkyboxProceduralShaderGUI"
}