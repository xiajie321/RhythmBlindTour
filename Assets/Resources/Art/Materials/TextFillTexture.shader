Shader "Custom/UI/TextFillTexture_GrayTint"
{
    Properties
    {
        _MainTex        ("Font Atlas",     2D)   = "white" {}
        _FillTex        ("Fill Texture",   2D)   = "white" {}
        _Color          ("Base Tint Color",Color)= (1,1,1,1)
        _GrayAmount     ("Gray Amount",    Range(0,1)) = 0
        _TintToBlack    ("Tint To Black",  Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off  
        ZWrite Off  
        Blend SrcAlpha OneMinusSrcAlpha  
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;      // 动态字体 Atlas  
            float4   _MainTex_ST;
            sampler2D _FillTex;      // 填充纹理  
            float4   _FillTex_ST;
            float4   _Color;         // 基础 Tint  
            float    _GrayAmount;    // 灰度强度  
            float    _TintToBlack;   // 从白到黑的 Tint  

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uvM : TEXCOORD0;
                float2 uvF : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uvM = TRANSFORM_TEX(v.uv, _MainTex);
                o.uvF = TRANSFORM_TEX(v.uv, _FillTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 1) 取字体 Atlas，只用 alpha 做蒙版
                float mask = tex2D(_MainTex, i.uvM).a * _Color.a;

                // 2) 取填充纹理 & 应用基础 Tint  
                fixed4 fillCol = tex2D(_FillTex, i.uvF);
                float3 colorRGB = fillCol.rgb * _Color.rgb;

                // 3) 灰度插值  
                float gray = dot(colorRGB, float3(0.299, 0.587, 0.114));
                float3 grayRGB = float3(gray, gray, gray);
                colorRGB = lerp(colorRGB, grayRGB, _GrayAmount);

                // 4) 从白到黑的 Tint  
                //    当 _TintToBlack=0 时，保持上一步结果；=1 时，完全黑色  
                colorRGB = lerp(colorRGB, float3(0,0,0), _TintToBlack);

                return fixed4(colorRGB, mask);
            }
            ENDCG
        }
    }
}
