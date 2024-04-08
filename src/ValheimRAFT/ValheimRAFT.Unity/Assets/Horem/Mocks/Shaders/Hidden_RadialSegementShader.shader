Shader "Hidden/RadialSegementShader" {
	Properties {
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Vector) = (1,1,1,1)
		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
		_Bluriness ("Bluriness", Range(1E-07, 1)) = 0.01
		_Offset ("Offset", Range(0, 1)) = 0
		_Thickness ("Thickness", Range(0, 1)) = 0.4
		[IntRange] _Segments ("Segments", Range(4, 16)) = 8
		[IntRange] _Selected ("Selected", Range(0, 1)) = 0
		_SelectedColor ("Selected Color", Vector) = (1,1,1,1)
		[IntRange] _Activated ("Activated", Range(0, 1)) = 0
		_Queued ("Queued", Range(0, 1)) = 0
		_QueuedColor ("Queued Color", Vector) = (1,1,1,1)
		_UnselectedColor ("Unselected Color", Vector) = (0,0,0,0.8)
		_ActivatedColor ("Activated Color", Vector) = (1,1,1,1)
		_ColorMask ("Color Mask", Float) = 15
		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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