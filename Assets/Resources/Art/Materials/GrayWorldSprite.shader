Shader "Custom/GrayWorldSpriteWithOutline"
{
    Properties
    {
        _MainTex          ("Sprite Texture", 2D) = "white" {}
        _Color            ("Tint Color", Color) = (1,1,1,1)
        _GrayAmount       ("Gray Amount", Range(0,1)) = 1
        _OutlineColor     ("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness ("Outline Thickness", Range(0,0.1)) = 0.005
    }
    SubShader
    {
        Tags { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            // 顶点输入
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };
            // 插值
            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float4 col   : COLOR;
            };

            sampler2D _MainTex;
            float4   _MainTex_ST;
            float4   _MainTex_TexelSize;   // <—— 必须声明
            float4   _Color;
            float    _GrayAmount;
            float4   _OutlineColor;
            float    _OutlineThickness;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                o.col = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 取原图与顶点色
                fixed4 src = tex2D(_MainTex, i.uv) * i.col;
                // 灰度计算
                float gray = dot(src.rgb, float3(0.299, 0.587, 0.114));
                float3 grayCol = float3(gray, gray, gray);
                // 在原色与灰度中插值
                float3 colorRGB = lerp(src.rgb, grayCol, _GrayAmount);
                float alpha = src.a;

                // 计算描边采样偏移
                float2 offset = _OutlineThickness * _MainTex_TexelSize.xy;
                // 8 方向取最大 alpha
                float maxA = 0;
                maxA = max(maxA, tex2D(_MainTex, i.uv + float2( offset.x,  0     )).a);
                maxA = max(maxA, tex2D(_MainTex, i.uv + float2(-offset.x,  0     )).a);
                maxA = max(maxA, tex2D(_MainTex, i.uv + float2( 0     ,  offset.y)).a);
                maxA = max(maxA, tex2D(_MainTex, i.uv + float2( 0     , -offset.y)).a);
                maxA = max(maxA, tex2D(_MainTex, i.uv + float2( offset.x,  offset.y)).a);
                maxA = max(maxA, tex2D(_MainTex, i.uv + float2( offset.x, -offset.y)).a);
                maxA = max(maxA, tex2D(_MainTex, i.uv + float2(-offset.x,  offset.y)).a);
                maxA = max(maxA, tex2D(_MainTex, i.uv + float2(-offset.x, -offset.y)).a);
                float outlineA = saturate(maxA - alpha);

                // 合成：主图 + 描边
                float3 finalRGB   = colorRGB * alpha + _OutlineColor.rgb * outlineA;
                float  finalAlpha = alpha + outlineA;

                return fixed4(finalRGB, finalAlpha);
            }
            ENDCG
        }
    }
}
