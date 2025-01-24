Shader "Custom/LitParticles" {
	Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_NormalTex ("Normal", 2D) = "bump" {}
		_BumpScale ("Normal power", Range(0, 4)) = 1
		_LightNormalFactor ("Light normal factor", Range(0, 1)) = 0
		_ZFadeDistance ("Soft particle fade distance", Float) = 0.3
		_CameraFadeDistanceMin ("Camera fade distance min", Float) = 0
		_CameraFadeDistanceMax ("Camera fade distance max", Float) = 0.1
		_CameraYFadeDistance ("Camera Y fade distance", Float) = 0
		[MaterialToggle] _Billboard ("Billboard", Float) = 0
		[MaterialToggle] _SkyMask ("SkyMask", Float) = 0
		_NoiseTex ("Noise", 2D) = "white" {}
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