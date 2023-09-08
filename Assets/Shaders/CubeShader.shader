Shader "Unlit/CubeShader"
{
    Properties
    {
        _MainTex("Main Texture", cube) = "white" {}
        _SpecularPower("Specular Power", Range(0, 100)) = 10
        _InternalColor("Internal Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            Tags{"LightMode" = "ForwardBase"}
            CGPROGRAM
            #pragma multi_compile_fwdbase
            #pragma vertex MyVertexProgram
            #pragma fragment MyFragmentProgram

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            samplerCUBE _MainTex;
            float _SpecularPower;
            float4 _InternalColor;

            struct appData
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float4 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float3 localPos : TEXCOORD3;
                SHADOW_COORDS(4)
            };

            v2f MyVertexProgram(appData v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.localPos = v.vertex;
                TRANSFER_SHADOW(o)
                return o;
            }

            float4 MyFragmentProgram(v2f i) : SV_Target
            {
                float3 albedo;
                if (abs(i.localPos.x) < 0.499f && abs(i.localPos.y) < 0.499f && abs(i.localPos.z) < 0.499f) {
                    albedo = _InternalColor.rgb;
                } else {
                    albedo = texCUBE(_MainTex, normalize(i.localPos)).rgb;
                }
                
                fixed3 diffuse = albedo * _LightColor0.rgb * DotClamped(_WorldSpaceLightPos0.xyz, i.worldNormal);
                fixed3 ambient = albedo * UNITY_LIGHTMODEL_AMBIENT.xyz;
                
                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos)
                
                return float4(ambient + diffuse * atten, 1);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
