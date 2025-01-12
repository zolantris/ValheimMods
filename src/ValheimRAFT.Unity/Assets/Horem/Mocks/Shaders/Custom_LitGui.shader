Shader "JVLmock_Custom/LitGui" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Tint", Vector) = (1,1,1,1)
		_Saturation ("Saturation", Range(-1, 1)) = 0
		_Brightness ("Brightness", Range(0, 2)) = 0
		_Lighting ("Lighting", Range(0, 1)) = 1
		_LightingSaturation ("Lighting saturation", Range(-1, 1)) = 0
		_ShadowOffsetX ("ShadowOffsetX", Float) = 0.1
		_ShadowOffsetY ("ShadowOffsetY", Float) = 0.1
		_ShadowIntensity ("Shadow intensity", Range(0, 1)) = 1
		_PixelSize ("PixelSize", Range(0, 1)) = 1
		_PixelIntensity ("Pixel intensity", Range(0, 1)) = 0
		_PixelAlphaIntensity ("Pixel alpha intensity", Range(0, 1)) = 0
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