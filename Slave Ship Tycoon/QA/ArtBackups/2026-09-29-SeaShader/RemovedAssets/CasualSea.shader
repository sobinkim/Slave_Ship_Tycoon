Shader "Fleet/Casual Sea"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sea Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FlowSpeed ("Flow Speed", Range(0,2)) = 0.55
        _DriftUV ("Pattern Drift (UV X / Y)", Vector) = (0.0025,0.004,0,0)
        _WaveFrequency ("Wave Frequency (World X / Y)", Vector) = (1.1,4.3,0,0)
        _EdgeFade ("Tile Edge Protection (UV)", Range(0.005,0.2)) = 0.04
        _ShimmerStrength ("Soft Highlight Strength", Range(0,0.2)) = 0.045
        _HighlightThreshold ("Highlight Brightness Threshold", Range(0,1)) = 0.32
        _HighlightSoftness ("Highlight Mask Softness", Range(0.01,0.5)) = 0.22
        _HighlightColor ("Highlight Color", Color) = (0.92,0.93,0.82,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Sprite Flip", Vector) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

        CGINCLUDE
        #include "UnitySprites.cginc"
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float2 positionWS : TEXCOORD1;
            half4 color : COLOR;
            UNITY_VERTEX_OUTPUT_STEREO
        };
        half4 _HighlightColor;
        float4 _DriftUV;
        float4 _WaveFrequency;
        float _FlowSpeed;
        float _EdgeFade;
        float _ShimmerStrength;
        float _HighlightThreshold;
        float _HighlightSoftness;

        Varyings SeaVertex(appdata_t v)
        {
            Varyings o = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            v2f sprite = SpriteVert(v);
            o.positionCS = sprite.vertex;
            o.positionWS = mul(unity_ObjectToWorld, UnityFlipSprite(v.vertex, _Flip)).xy;
            o.uv = sprite.texcoord;
            o.color = sprite.color;
            return o;
        }

        half4 SeaFragment(Varyings i) : SV_Target
        {
            float time = _Time.y * _FlowSpeed;
            // Keep the horizon and mirrored tile borders fixed. Interior artwork drifts gently.
            float2 edgeDistance = min(i.uv, 1.0 - i.uv);
            float edgeMask = smoothstep(0.0, _EdgeFade, min(edgeDistance.x, edgeDistance.y));
            float2 phase = i.positionWS * _WaveFrequency.xy;
            float2 drift = float2(sin(phase.y + time), sin(phase.x - time * 0.73));
            float2 uv = i.uv + drift * _DriftUV.xy * edgeMask;
            half4 sea = SampleSpriteTexture(uv);

            // World-space highlight phase stays continuous across flipped/recycled tiles.
            half brightness = dot(sea.rgb, half3(0.2126, 0.7152, 0.0722));
            half lightMask = smoothstep(_HighlightThreshold, _HighlightThreshold + _HighlightSoftness, brightness);
            half shimmer = 0.5 + 0.5 * sin(phase.x * 0.6 + phase.y * 0.5 - time);
            sea.rgb = lerp(sea.rgb, _HighlightColor.rgb, lightMask * shimmer * _ShimmerStrength);
            return sea * i.color;
        }
        ENDCG

        Pass
        {
            CGPROGRAM
            #pragma vertex SeaVertex
            #pragma fragment SeaFragment
            #pragma multi_compile_instancing
            ENDCG
        }
    }
}
