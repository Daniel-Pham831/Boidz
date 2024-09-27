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
                float rotationInRad; //0 means vector2.up
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
                boid_data boid_instance_data = data[instance_id];

                float cosAngle = cos(boid_instance_data.rotationInRad);
                float sinAngle = sin(boid_instance_data.rotationInRad);

                float2 vertex_pos = v.vertex_pos * boidSize;
                float2 after_rotated_vertex_pos = float2(
                    (vertex_pos.x * cosAngle + vertex_pos.y * sinAngle),
                    -(vertex_pos.x * sinAngle - vertex_pos.y * cosAngle)
                );
    
                after_rotated_vertex_pos += boid_instance_data.position;

                o.pos = UnityObjectToClipPos(float4(after_rotated_vertex_pos, 0, 1.0));
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
