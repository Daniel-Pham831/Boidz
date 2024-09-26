Shader "Custom/BoidShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                uint vertexID : SV_VertexID;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            uniform fixed4 _Color;
            StructuredBuffer<float2> vertexPositions;

            v2f vert(appdata v)
            {
                v2f o;

                // Get the vertex position from the buffer
                float2 position = vertexPositions[v.vertexID];

                // Transform to clip space
                o.pos = UnityObjectToClipPos(float4(position, 0.0,1.0));

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
