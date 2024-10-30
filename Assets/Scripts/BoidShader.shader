Shader "Custom/BoidShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        boidSize ("Boid Size", Float) = 0.5
        _MainTex ("Texture Frame 1", 2D) = "white" {}
        _MainTex2 ("Texture Frame 2", 2D) = "white" {}
        _MainTex3 ("Texture Frame 3", 2D) = "white" {}
        _MainTex4 ("Texture Frame 4", 2D) = "white" {}
        _FrameRate ("Frame Rate", Float) = 8.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha 

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
                float2 uv : TEXCOORD0;
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
                float2 uv : TEXCOORD0;
                uint instance_id : SV_InstanceID;
            };

            uniform half4 _Color;
            uniform half boidSize;
            uniform sampler2D _MainTex;
            uniform sampler2D _MainTex2;
            uniform sampler2D _MainTex3;
            uniform sampler2D _MainTex4;
            uniform float _FrameRate;  // Control the speed of animation

            StructuredBuffer<boid_data> data;

            v2f vert(const appdata v, const uint instance_id: SV_InstanceID)
            {
                v2f o;
                boid_data boid_instance_data = data[instance_id];
                float2 dir = boid_instance_data.dir;
                float boid_size = max(0.005, boidSize);

                // Calculate rotated vertex position in world space
                float4 rotated_vertex_pos_in_world_space = float4(
                    (v.vertex_pos.x * dir.y + v.vertex_pos.y * dir.x)*boid_size + boid_instance_data.position.x,
                    -(v.vertex_pos.x * dir.x - v.vertex_pos.y * dir.y)*boid_size + boid_instance_data.position.y,
                    0,
                    1.0
                );

                o.pos = UnityObjectToClipPos(rotated_vertex_pos_in_world_space);
                o.uv = v.uv;
                o.instance_id = instance_id;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // Determine the frame based on time
                float time = (_Time.y + (i.instance_id % 4)*0.1666) * _FrameRate;
                int frame = fmod(time, 4);

                // Select the correct texture based on the frame
                half4 sampledColor;
                if (frame == 0)
                    sampledColor = tex2D(_MainTex, i.uv);
                else if (frame == 1)
                    sampledColor = tex2D(_MainTex2, i.uv);
                else if (frame == 2)
                    sampledColor = tex2D(_MainTex3, i.uv);
                else
                    sampledColor = tex2D(_MainTex4, i.uv);

                return sampledColor * _Color;  // Apply color tint
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}