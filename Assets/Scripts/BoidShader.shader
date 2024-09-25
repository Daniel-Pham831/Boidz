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
            Cull Back // Cull back faces to avoid rendering unnecessary geometry

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
            };

            uniform fixed4 _Color;

            v2f vert(appdata v)
            {
                v2f o;

                // Transform the vertex position to clip space
                float4 clipPos = UnityObjectToClipPos(v.vertex);

                // Simple frustum culling based on clip space coordinates
                if (clipPos.x < -clipPos.w || clipPos.x > clipPos.w ||
                    clipPos.y < -clipPos.w || clipPos.y > clipPos.w ||
                    clipPos.z < 0.0 || clipPos.z > clipPos.w)
                {
                    // Move the vertex far off-screen if it's outside the frustum
                    o.pos = float4(4000,4000, 4000, 1);
                    o.color = fixed4(0,0,0,0);
                }else
                {
                    o.pos = clipPos;
                    o.color = _Color;
                }
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
