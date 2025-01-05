Shader "Lux Lit Particles/ Bumped" {
	Properties {
		_Color ("Color", Vector) = (1,1,1,1)
		_MainTex ("Normal (RG) Depth (B) Alpha (A)", 2D) = "white" {}
		[Space(8)] [Toggle(EFFECT_HUE_VARIATION)] _EnableAlbedo ("Enable Albedo", Float) = 0
		[NoScaleOffset] _AlbedoMap ("    Albedo (RGB)", 2D) = "white" {}
		[Space(8)] [Toggle(_EMISSION)] _EnableEmission ("Enable Emission", Float) = 0
		_EmissionColor ("    Emission Color", Vector) = (1,1,1,1)
		[NoScaleOffset] _EmissionMap ("    Emission (RGB) Alpha (A)", 2D) = "white" {}
		_EmissionScale ("    Emission Scale", Float) = 1
		[Space(8)] [Toggle(_FLIPBOOK_BLENDING)] _EnableFlipbookBlending ("Enable Flipbook Blending", Float) = 0
		[LuxParticles_HelpDrawer] _HelpFlip ("If enabled you have to adjust the Vertex Stream.", Float) = 0
		[Space(8)] _InvFade ("Soft Particles Factor", Range(0.01, 3)) = 1
		[Header(Camera Fade Distances)] [Space(4)] _CamFadeDistance ("Near (X) Far (Y) FarRange (Z)", Vector) = (4,150,25,0)
		[Header(Lighting)] [Space(4)] [Toggle(GEOM_TYPE_BRANCH)] _AddLightsPerPixel ("Full Per Pixel Lighting", Float) = 0
		_WrappedDiffuse ("Wrapped Diffuse", Range(0, 1)) = 0.2
		_Translucency ("Translucency", Range(0, 1)) = 0.5
		_AlphaInfluence ("    Depth Influence", Range(0, 4)) = 1
		[Header(Shadows)] [Space(4)] [Toggle(GEOM_TYPE_FROND)] _EnableShadows ("Enable directional Shadows", Float) = 0
		[Toggle(GEOM_TYPE_MESH)] _ShadowsPerPixel ("    Per Pixel Shadows", Float) = 0
		_ShadowExtrude ("        Extrude", Range(0, 10)) = 1
		_ShadowDensity ("Casted Shadows Density", Range(0.5, 2)) = 1
		[Header(Ambient Lighting)] [Space(4)] [Toggle(GEOM_TYPE_BRANCH_DETAIL)] _PerPixelAmbientLighting ("    Per Pixel Ambient Lighting", Float) = 0
		[LuxParticles_HelpDrawer] _HelpFlip ("Only check if Ambient Source is set to Gradient or Skybox.", Float) = 0
		[Toggle(GEOM_TYPE_LEAF)] _LocalAmbientLighting ("    Enable Light Probes", Float) = 0
		[LuxParticles_HelpDrawer] _HelpFlip ("If enabled you have to add the LuxParticles_LocalAmbientLighting script.", Float) = 0
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