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
                float2 dir;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            uniform half4 _Color;
            uniform half boidSize;
            StructuredBuffer<boid_data> data;

            v2f vert(const appdata v, const uint instance_id: SV_InstanceID)
            {
                v2f o;
                boid_data boid_instance_data = data[instance_id];
                float2 dir = boid_instance_data.dir;
                float boid_size = max(0.005, boidSize);

                // i know this looks wtf, but to avoid creating new var,
                // i'm just calculating the rotated vertex position here
                float4 rotated_vertex_pos_in_world_space = float4(
                    (v.vertex_pos.x * dir.y + v.vertex_pos.y * dir.x)*boid_size + boid_instance_data.position.x,
                    -(v.vertex_pos.x * dir.x - v.vertex_pos.y * dir.y)*boid_size + boid_instance_data.position.y,
                    0,
                    1.0
                );

                o.pos = UnityObjectToClipPos(rotated_vertex_pos_in_world_space);
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
