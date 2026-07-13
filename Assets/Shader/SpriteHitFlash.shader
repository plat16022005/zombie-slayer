// SpriteHitFlash.shader
// Shader đơn giản cho SpriteRenderer 2D:
//   _FlashAmount = 0  → render sprite bình thường (có alpha)
//   _FlashAmount = 1  → render sprite thành solid _FlashColor (giữ alpha)
//   _FadeAmount = 0   → alpha giữ nguyên
//   _FadeAmount = 1   → alpha = 0 (mờ hoàn toàn)
Shader "Custom/SpriteHitFlash"
{
    Properties
    {
        [PerRendererData] _MainTex   ("Sprite Texture", 2D) = "white" {}
        _Color                       ("Tint", Color)        = (1,1,1,1)
        
        // Hit flash
        _FlashColor                  ("Flash Color", Color) = (1,1,1,1)
        _FlashAmount                 ("Flash Amount", Range(0,1)) = 0

        // Fade out
        _FadeAmount                  ("Fade Amount", Range(0,1)) = 0

        // SpriteRenderer internals
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip          ("Flip", Vector)         = (1,1,1,1)
        [HideInInspector] _AlphaTex      ("External Alpha", 2D)   = "white" {}
        [HideInInspector] _EnableExternalAlpha ("EnableExternalAlpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType"      = "Transparent"
            "PreviewType"     = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha   // Premultiplied alpha (chuẩn SpriteRenderer)

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            float4 _FlashColor;
            float  _FlashAmount;
            float  _FadeAmount;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.vertex = UnityFlipSprite(IN.vertex, _Flip);
                OUT.vertex = UnityObjectToClipPos(OUT.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
                
                // --- Fade logic ---
                // Giảm alpha theo _FadeAmount (0 -> 1)
                c.a = c.a * (1.0 - _FadeAmount);
                
                // Premultiply alpha
                c.rgb *= c.a; 

                // --- Flash logic ---
                // Lerp màu RGB sang FlashColor, giữ nguyên alpha
                c.rgb = lerp(c.rgb, _FlashColor.rgb * c.a, _FlashAmount);

                return c;
            }
            ENDCG
        }
    }
}
