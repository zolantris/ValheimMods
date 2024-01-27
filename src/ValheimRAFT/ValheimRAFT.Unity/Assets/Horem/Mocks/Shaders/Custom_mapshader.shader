Shader "JVLmock_Custom/mapshader" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_MaskTex ("Mask texture", 2D) = "white" {}
		_HeightTex ("Height", 2D) = "white" {}
		_FogTex ("Fog", 2D) = "white" {}
		_BackgroundTex ("Background", 2D) = "white" {}
		_FogLayerTex ("Fog layer", 2D) = "white" {}
		_MountainTex ("Mountain texture", 2D) = "white" {}
		_ForestTex ("Forest texture", 2D) = "white" {}
		_ForestColor ("Forest color", Vector) = (1,1,1,1)
		_WaterTex ("Water texture", 2D) = "white" {}
		_SpaceTex ("Space texture", 2D) = "white" {}
		_CloudTex ("Clouds texture", 2D) = "white" {}
		[HDR] _WaterColor ("Water color", Vector) = (1,1,1,1)
		[HDR] _WaterColorDeep ("Water color deep", Vector) = (1,1,1,1)
		[HDR] _FogColor ("Fog color", Vector) = (1,1,1,1)
		_normalWidth ("Normal width", Float) = 0.01
		_normalIntensity ("Normal intensity", Float) = 1
		_lightDir ("Light dir", Vector) = (1,0,0,1)
		[HDR] _lightColor ("Light color", Vector) = (1,1,1,1)
		[HDR] _ambientLightColor ("Ambient color", Vector) = (1,1,1,1)
		_SharedFade ("Shared fade", Float) = 0
		[HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
		[HideInInspector] _Stencil ("Stencil ID", Float) = 0
		[HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
		[HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
		[HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
		[HideInInspector] _ColorMask ("Color Mask", Float) = 15
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
}