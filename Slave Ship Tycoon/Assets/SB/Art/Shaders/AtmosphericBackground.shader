Shader "Fleet/Atmospheric Background"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _ShadowColor ("Palette Shadow", Color) = (0.49,0.70,0.79,1)
        _LightColor ("Palette Light", Color) = (0.96,0.96,0.91,1)
        _ShadowLuma ("Source Shadow Brightness", Range(0,1)) = 0.65
        _LightLuma ("Source Light Brightness", Range(0,1)) = 0.91
        _HazeColor ("Horizon Haze Color", Color) = (0.83,0.91,0.89,1)
        _HorizonY ("Horizon World Y", Float) = 3.16
        _HazeHeight ("Haze Height (World Units)", Range(0.1,5)) = 1.2
        _HazeStrength ("Horizon Haze Strength", Range(0,1)) = 0.35
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            struct atmosphere_v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float worldY : TEXCOORD1;
                fixed4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            fixed4 _ShadowColor, _LightColor, _HazeColor;
            float _ShadowLuma, _LightLuma, _HorizonY, _HazeHeight, _HazeStrength;

            atmosphere_v2f vert(appdata_t v)
            {
                atmosphere_v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float4 flipped = UnityFlipSprite(v.vertex, _Flip);
                o.vertex = UnityObjectToClipPos(flipped);
                o.worldY = mul(unity_ObjectToWorld, flipped).y;
                o.uv = v.texcoord;
                o.color = v.color * _Color * _RendererColor;
                return o;
            }

            fixed4 frag(atmosphere_v2f i) : SV_Target
            {
                fixed4 source = SampleSpriteTexture(i.uv);
                float luminance = dot(source.rgb, float3(0.2126, 0.7152, 0.0722));
                float shade = saturate((luminance - _ShadowLuma) / max(_LightLuma - _ShadowLuma, 0.01));
                float3 color = lerp(_ShadowColor.rgb, _LightColor.rgb, shade);
                // World height keeps the mist continuous across scrolling and mirrored tiles.
                float haze = 1.0 - smoothstep(0.0, max(_HazeHeight, 0.01), i.worldY - _HorizonY);
                color = lerp(color, _HazeColor.rgb, haze * _HazeStrength);
                return fixed4(color * i.color.rgb, source.a * i.color.a);
            }
            ENDCG
        }
    }
}
