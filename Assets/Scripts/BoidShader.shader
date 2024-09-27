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

            void to_normal_rad(inout float rad)
            {
                if (rad < 0 || rad >= 2 * 3.14159265359)
                {
                    rad = rad - floor(rad / (2 * 3.14159265359)) * 2 * 3.14159265359;
                }
            }

            v2f vert(const appdata v, const uint instance_id: SV_InstanceID)
            {
                v2f o;
                boid_data boid_instance_data = data[instance_id];
                float2 boid_position = boid_instance_data.position;
                float2 boid_direction = boid_instance_data.direction;
                float2 corrected_vertex_pos = float2(v.vertex_pos.x, v.vertex_pos.y) - boid_position;

                o.pos = UnityObjectToClipPos(float4(corrected_vertex_pos, 0, 1.0));
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
