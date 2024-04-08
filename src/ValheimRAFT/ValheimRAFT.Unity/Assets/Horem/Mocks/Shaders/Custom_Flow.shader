Shader "JVLmock_Custom/Flow" {
	Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_FoamTex ("Foam", 2D) = "white" {}
		_FoamColor ("Foam Color", Vector) = (1,1,1,1)
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
		_Metallic ("Metallic", Range(0, 1)) = 0
		_SpeedX ("Speed X", Float) = 1
		_SpeedY ("Speed Y", Float) = 1
		_FoamSpeed ("Foam Speed", Float) = 1
		_BumpScale ("Normal scale", Float) = 1
		_BumpMap ("Normal Map", 2D) = "bump" {}
		_DepthFade ("Depth Fade", Float) = 1
		_FoamDepth ("Foam depth", Float) = 1
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