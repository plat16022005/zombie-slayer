Shader "UI/Vignette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 0, 0, 1)
        _Radius ("Vignette Radius", Range(0, 2)) = 0.8
        _Softness ("Vignette Softness", Range(0, 1)) = 0.5
        
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _Radius;
            float _Softness;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Toạ độ uv từ 0 đến 1, dời tâm về (0,0) (từ -0.5 đến 0.5)
                float2 center = IN.texcoord - float2(0.5, 0.5);
                
                // Khoảng cách từ tâm đến viền (ở 4 góc là ~0.707)
                float dist = length(center); 
                
                // Tính toán hiệu ứng mờ dần (vignette)
                float vignette = smoothstep(_Radius, _Radius - _Softness, dist);
                
                // Ở tâm (vignette = 1) thì alpha = 0 (trong suốt)
                // Ở ngoài viền (vignette = 0) thì alpha = 1 (hiện màu đỏ)
                float alpha = 1.0 - vignette;
                
                // Kết hợp màu sắc
                fixed4 color = IN.color;
                color.a *= alpha;
                
                return color;
            }
            ENDCG
        }
    }
}
