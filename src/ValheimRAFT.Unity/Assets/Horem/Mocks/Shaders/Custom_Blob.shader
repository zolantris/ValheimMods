Shader "JVLmock_Custom/Blob" {
	Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0, 1)) = 0.5
		_Metallic ("Metallic", Range(0, 1)) = 0
		_BumpScale ("Normal scale", Float) = 1
		_BumpMap ("Normal Map", 2D) = "bump" {}
		_FlowSpeedAll ("Flow speed all", Float) = 1
		_FlowMaskTex ("FlowMask", 2D) = "white" {}
		_FlowSpeedY ("Flow speed", Float) = 1
		_FlowTexture ("Flow texture", 2D) = "white" {}
		_FlowColor ("Flow color", Vector) = (1,1,1,1)
		_EmissiveColor ("Emissive", Vector) = (0,0,0,1)
		_SSS ("SSS", Vector) = (0,0,0,1)
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