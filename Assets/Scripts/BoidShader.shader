Shader "Custom/BoidShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        boidSize ("Boid Size", Float) = 0.5
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
                float4 vertex_pos : POSITION;
                uint vertex_id : SV_VertexID;
            };

            struct boid_data
            {
                float2 position;
                float2 direction;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            uniform half4 _Color;
            uniform half boidSize;
            StructuredBuffer<boid_data> data;
            uniform const float pi = 3.141592653589793238462;

            v2f vert(const appdata v, const uint instance_id: SV_InstanceID)
            {
                v2f o;

                // Retrieve boid data
                const boid_data boid_instance_data = data[instance_id];
                const float2 boid_position = boid_instance_data.position;

                // Assuming boid_direction is a unit vector representing the boid's orientation
                float2 boid_direction = boid_instance_data.direction;

                // The vertex position before transformation (from the mesh)
                float2 vertex_pos = v.vertex_pos.xy;

                // Rotate the vertex position by the boid's direction
                const float cos_angle = boid_direction.x;
                const float sin_angle = boid_direction.y;

                const float2 rotated_vertex_pos = float2(
                    vertex_pos.x * cos_angle - vertex_pos.y * sin_angle,
                    vertex_pos.x * sin_angle + vertex_pos.y * cos_angle
                );

                // Translate the rotated vertex position to the boid's position
                float2 transformed_vertex_pos = rotated_vertex_pos + boid_position;

                // Convert to clip space
                o.pos = UnityObjectToClipPos(float4(transformed_vertex_pos, 0, 1.0));
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
