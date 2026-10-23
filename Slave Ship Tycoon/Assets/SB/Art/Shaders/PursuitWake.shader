Shader "Fleet/Pursuit Wake"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _CrestColor ("Water Crest", Color) = (0.47,0.75,0.79,1)
        _FoamColor ("Broken Foam", Color) = (0.67,0.86,0.86,1)
        _Opacity ("Wake Opacity", Range(0,1)) = 0.7
        _Thickness ("Crest Thickness", Range(0.005,0.06)) = 0.018
        _Breakup ("Crest Breakup", Range(0,1)) = 0.65
        [PerRendererData] _WakeRole ("Trailing Wave", Float) = 0
        [PerRendererData] _WakeTime ("Movement Time", Float) = 0
        [PerRendererData] _WakeSeed ("Wave Variation", Float) = 0
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex SpriteVert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            fixed4 _CrestColor, _FoamColor;
            float _Opacity, _Thickness, _Breakup, _WakeRole, _WakeTime, _WakeSeed;
            float Stroke(float distance, float width, float aa)
            {
                return 1 - smoothstep(width, width + aa, abs(distance));
            }
            fixed4 frag(v2f i) : SV_Target
            {
                // Full-rect contact quad: no baked white wedge or painted outline.
                float2 p = i.texcoord;
                float t = _WakeTime;
                float y = p.y - .5;
                float aa = max(fwidth(y), .002);
                float crest, body, foam;
                if (_WakeRole < .5)
                {
                    float behindBow = saturate((.9 - p.x) / .8);
                    float edge = .035 + .32 * sqrt(behindBow);
                    edge += .012 * sin(p.x * 14 + t * 3);
                    float envelope = smoothstep(.04, .2, p.x) * (1 - smoothstep(.87, .95, p.x));
                    float taper = lerp(.45, 1, sin(behindBow * 3.14159));
                    float breaks = lerp(1, .25 + .75 * smoothstep(-.4, .6,
                        sin(p.x * 31 + t * 7 + sign(y) * 2)), _Breakup);
                    float distance = abs(y) - edge;
                    crest = Stroke(distance, _Thickness * taper, aa) * breaks;
                    body = Stroke(distance + .025, .035 * taper, aa) * .13;
                    float outer = Stroke(abs(y) - edge - .075, _Thickness * .5 * taper, aa);
                    crest += outer * .28 * smoothstep(.25, .5, behindBow) * breaks;
                    foam = crest * smoothstep(.4, .85, sin(p.x * 19 - t * 5 + y * 8)) * .38;
                    crest *= envelope;
                    body *= envelope;
                    foam *= envelope;
                }
                else
                {
                    // Detached, tapered wavelets use the sea's curved, interrupted crest style.
                    float x = (p.x - .5) * 2;
                    float cap = saturate(1 - x * x);
                    float taper = sqrt(cap);
                    float curve = .065 * sin(x * 2.1 + _WakeSeed + t * .6) + .035 * x;
                    float breaks = lerp(1, .2 + .8 * smoothstep(-.3, .55,
                        sin(x * 9 + _WakeSeed + t * 4)), _Breakup);
                    float envelope = smoothstep(0, .3, cap);
                    crest = Stroke(y - curve, _Thickness * 1.25 * taper, aa) * breaks * envelope;
                    body = Stroke(y - curve + .045, .045 * taper, aa) * envelope * .1;
                    foam = crest * smoothstep(.55, .95, sin(x * 5 + _WakeSeed - t * 3)) * .3;
                }
                float strength = saturate(crest + body);
                fixed4 color = fixed4(lerp(_CrestColor.rgb, _FoamColor.rgb, saturate(foam)),
                    strength * _Opacity) * i.color;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
