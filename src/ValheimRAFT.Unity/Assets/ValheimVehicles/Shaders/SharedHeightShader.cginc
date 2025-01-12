#pragma once
#include "UnityStandardCore.cginc"

float _MaxHeight;

struct appdata_t
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};


struct VertexOutputForwardBaseWithWorldPos : VertexOutputForwardBase
{
    float worldY : TEXCOORD1;
};

VertexOutputForwardBaseWithWorldPos vert(VertexInput v)
{
    VertexOutputForwardBaseWithWorldPos o;
    VertexOutputForwardBase fb = vertForwardBase(v);

    o.pos = fb.pos;
    o.tex = fb.tex;
    o.eyeVec = fb.eyeVec;
    o.ambientOrLightmapUV = fb.ambientOrLightmapUV;
    o._ShadowCoord = fb._ShadowCoord;

    // extension.
    float4 worldPosition = mul(unity_ObjectToWorld, v.vertex);
    o.worldY = worldPosition.y;

    return o;
}

fixed4 frag(VertexOutputForwardBaseWithWorldPos i) : SV_Target
{
    // Check if the world Y position is above the maximum height
    if (i.worldY > _MaxHeight)
    {
        // Fully transparent for the case where world Y is above max height
        return fixed4(0, 0, 0, 0);
    }

    VertexOutputForwardBase o;
    o.pos = i.pos;
    o.tex = i.tex;
    o.eyeVec = i.eyeVec;
    o.ambientOrLightmapUV = i.ambientOrLightmapUV;
    o._ShadowCoord = i._ShadowCoord;


    return fragForwardBase(o);
}
