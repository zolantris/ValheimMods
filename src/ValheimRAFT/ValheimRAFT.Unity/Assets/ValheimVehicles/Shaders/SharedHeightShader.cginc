#pragma once

sampler2D _MainTex;
fixed4 _Color;
float _MaxHeight;

struct appdata_t
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
    float worldY : TEXCOORD1; 
};

v2f vert(appdata_t v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;

    float4 worldPosition = mul(unity_ObjectToWorld, v.vertex);
    o.worldY = worldPosition.y;

    return o ;
}

fixed4 frag(v2f i) : SV_Target
{
    // Check if the world Y position is above the maximum height
    if (i.worldY > _MaxHeight)
    {
        return fixed4(0, 0, 0, 0); // Fully transparent
    }

    fixed4 c = tex2D (_MainTex, i.uv) * _Color;
    return c * _Color.a;
}