Shader "Custom/Role"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            Tags {"LightMode"="ForwardBase"}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma target 4.5

            struct RoleState
            {
                float3 Position;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            StructuredBuffer<RoleState> RoleStates;

            struct appdata_custom {
                uint instanceID : SV_InstanceID;
                float4 vertex : POSITION;
                float4 texcoord : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata_custom v)
            {
                float3 p = RoleStates[v.instanceID].Position;
                float4x4 mat = {
                    1,0,0,p.x,
                    0,1,0,p.y,
                    0,0,1,p.z,
                    0,0,0,1,
                };
                float4 wpos = mul(mat, v.vertex);

                v2f o;
                o.pos = UnityObjectToClipPos(float4(wpos.xyz, 1));
                o.uv = v.texcoord.xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }           
}