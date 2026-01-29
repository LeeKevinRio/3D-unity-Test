Shader "Custom/StencilSource"
{
    // SourceBox Shader - 被 HoleBox 挖洞的部分不渲染
    // WebGL 相容版本

    Properties
    {
        _Color ("Color", Color) = (0.2, 0.5, 0.9, 0.5)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            fixed4 _Color;

            // HoleBox 參數 - 使用 float 而非 int (WebGL 相容)
            float4 _HoleBox0_Min;
            float4 _HoleBox0_Max;
            float4 _HoleBox1_Min;
            float4 _HoleBox1_Max;
            float _HoleBoxCount;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            bool IsInsideBox(float3 pos, float3 boxMin, float3 boxMax)
            {
                return pos.x >= boxMin.x && pos.x <= boxMax.x &&
                       pos.y >= boxMin.y && pos.y <= boxMax.y &&
                       pos.z >= boxMin.z && pos.z <= boxMax.z;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 檢查是否在 HoleBox 內，如果是就不渲染
                if (_HoleBoxCount > 0.5 && IsInsideBox(i.worldPos, _HoleBox0_Min.xyz, _HoleBox0_Max.xyz))
                    discard;
                if (_HoleBoxCount > 1.5 && IsInsideBox(i.worldPos, _HoleBox1_Min.xyz, _HoleBox1_Max.xyz))
                    discard;

                // 光照
                float3 lightDir = normalize(float3(0.5, 1, 0.3));
                float ndotl = max(0.3, dot(i.worldNormal, lightDir));

                fixed4 col = _Color;
                col.rgb *= ndotl;

                return col;
            }
            ENDCG
        }
    }

    FallBack "Transparent/Diffuse"
}
