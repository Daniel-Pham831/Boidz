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
            #pragma target 4.0  // Ensure you're using Shader Model 4.0 or higher

            #include "UnityCG.cginc"

            struct appdata
            {
                uint vertexID : SV_VertexID;
            };

            struct boid_data
            {
                float2 pos;   // Boid position
                float angle;  // Boid direction angle in radians
                float size;   // Precomputed boid size
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            uniform half4 _Color;
            
            StructuredBuffer<boid_data> data;

            v2f vert(appdata v)
            {
                v2f o;

                const int boid_index = v.vertexID / 3;
                boid_data boid = data[boid_index];

                float2 vertexPosition;

                // Define vertices for the triangle
                if (v.vertexID % 3 == 0)        // Tip vertex (forward)
                    vertexPosition = float2(0.0, boid.size);
                else if (v.vertexID % 3 == 1)   // Left base vertex
                    vertexPosition = float2(boid.size * 0.5, -boid.size * 0.5);
                else                            // Right base vertex
                    vertexPosition = float2(-boid.size * 0.5, -boid.size * 0.5);

                // Apply rotation based on the boid's angle
                float adjustedAngle = boid.angle; // The angle should already be adjusted in C#.
                float cosAngle = cos(adjustedAngle);
                float sinAngle = sin(adjustedAngle);
                float2 rotatedPosition = float2(
                    vertexPosition.x * cosAngle - vertexPosition.y * sinAngle,
                    vertexPosition.x * sinAngle + vertexPosition.y * cosAngle
                );

                // Translate to the boid's position
                rotatedPosition += boid.pos;

                // Transform to clip space
                o.pos = UnityObjectToClipPos(float4(rotatedPosition, 0.0, 1.0));
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
