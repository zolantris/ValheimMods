Shader "Custom/CombinedHeightBasedShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 0)
        _MaxHeight ("Max Height", Float) = 30.0
    }

    SubShader
    {
        Tags {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }
        LOD 100
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Stencil
        {
            Ref 3 // Stencil reference value to compare against
            Comp Always // Write to the stencil buffer
            Pass Keep // Replace stencil value
//            Fail DecrSat
//            ZFail DecrSat
        }

        Pass
        {
            Stencil
            {
                Ref 1 // Match the stencil reference value in SelectiveMask
                Comp Equal  // Render where stencil value is NOT equal to Ref
                Pass Keep // Keep the existing stencil value
//                Fail IncrSat
//                ZFail IncrSat
            }
             ZTest LEqual
             
            Cull Off // Render both sides

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "SharedHeightShader.cginc"
            ENDCG
        }


       Pass
        {
            Stencil
            {
                Ref 2 // Match the stencil reference value in SelectiveMask
                Comp Equal  // Render where stencil value is NOT equal to Ref
                Pass Keep // Keep the existing stencil value
            }
            ZTest GEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "SharedHeightShader.cginc"
            ENDCG
        }

       Pass
        {
            Stencil
            {
                Ref 5 // Match the stencil reference value in SelectiveMask
                Comp Greater  // Render where stencil value is NOT equal to Ref
                Pass DecrWrap // Keep the existing stencil value
            }
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "SharedHeightShader.cginc"
            ENDCG
        }

       Pass
        {
            Stencil
            {
                Ref 4 // Match the stencil reference value in SelectiveMask
                Comp Equal  // Render where stencil value is NOT equal to Ref
                Pass Keep // Keep the existing stencil value
            }
             ZTest Always

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "SharedHeightShader.cginc"
            ENDCG
        }
    }

    Fallback "Diffuse"
}
