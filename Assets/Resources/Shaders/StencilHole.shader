Shader "Custom/StencilHole"
{
    // HoleBox Shader - 在 SourceBox 內部時顯示為藍色凹洞內壁
    // 當重疊其他 HoleBox 時，內部不重複渲染
    // WebGL 相容版本

    Properties
    {
        _Color ("Hole Color (outside)", Color) = (1, 0.5, 0, 0.3)
        _HoleInnerColor ("Hole Inner Color", Color) = (0.15, 0.4, 0.75, 0.9)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+1" }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            fixed4 _Color;
            fixed4 _HoleInnerColor;

            // SourceBox 參數 - 使用 float 而非 int (WebGL 相容)
            float4 _SourceBox0_Min;
            float4 _SourceBox0_Max;
            float4 _SourceBox1_Min;
            float4 _SourceBox1_Max;
            float _SourceBoxCount;

            // 其他 HoleBox 參數（用來檢測重疊）
            float4 _OtherHoleBox0_Min;
            float4 _OtherHoleBox0_Max;
            float4 _OtherHoleBox1_Min;
            float4 _OtherHoleBox1_Max;
            float _OtherHoleBoxCount;

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
                float3 viewDir : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                // 計算視角方向用於判斷正反面 (替代 VFACE)
                o.viewDir = normalize(_WorldSpaceCameraPos - o.worldPos);
                return o;
            }

            bool IsInsideBox(float3 pos, float3 boxMin, float3 boxMax)
            {
                return pos.x > boxMin.x && pos.x < boxMax.x &&
                       pos.y > boxMin.y && pos.y < boxMax.y &&
                       pos.z > boxMin.z && pos.z < boxMax.z;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 檢查是否在其他 HoleBox 內部，如果是就不渲染（避免重疊）
                if (_OtherHoleBoxCount > 0.5 && IsInsideBox(i.worldPos, _OtherHoleBox0_Min.xyz, _OtherHoleBox0_Max.xyz))
                    discard;
                if (_OtherHoleBoxCount > 1.5 && IsInsideBox(i.worldPos, _OtherHoleBox1_Min.xyz, _OtherHoleBox1_Max.xyz))
                    discard;

                bool isInsideSource = false;

                // 檢查是否在任何 SourceBox 內
                if (_SourceBoxCount > 0.5 && IsInsideBox(i.worldPos, _SourceBox0_Min.xyz, _SourceBox0_Max.xyz))
                    isInsideSource = true;
                if (_SourceBoxCount > 1.5 && IsInsideBox(i.worldPos, _SourceBox1_Min.xyz, _SourceBox1_Max.xyz))
                    isInsideSource = true;

                // 使用 viewDir 和 normal 的點積判斷正反面 (替代 VFACE，WebGL 相容)
                float facing = dot(i.worldNormal, i.viewDir);
                float3 normal = (facing > 0) ? i.worldNormal : -i.worldNormal;

                // 光照
                float3 lightDir = normalize(float3(0.5, 1, 0.3));
                float ndotl = max(0.3, dot(normal, lightDir));

                fixed4 col;

                if (isInsideSource)
                {
                    // 在 SourceBox 內部 - 渲染為藍色凹洞內壁
                    col = _HoleInnerColor;
                }
                else
                {
                    // 在 SourceBox 外部 - 渲染為半透明橘色
                    col = _Color;
                }

                col.rgb *= ndotl;
                return col;
            }
            ENDCG
        }
    }
}
