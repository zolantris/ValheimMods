Shader "Custom/SailShader" {
	Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_MainNormal ("Normal", 2D) = "" {}
		_MainColor ("Color", Vector) = (1,1,1,1)
		_Wet ("Wet", Range(0, 1)) = 0
		_PatternTex ("Pattern", 2D) = "white" {}
		_PatternRotation ("Pattern Rotation", Range(0, 1)) = 0
		_PatternColor ("Pattern Color", Vector) = (1,1,1,1)
		_LogoTex ("Logo", 2D) = "white" {}
		_LogoRotation ("Logo Rotation", Range(0, 1)) = 0
		_LogoColor ("Logo Color", Vector) = (1,1,1,1)
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