Shader "Fleet/Ship Water Contact"
{
    Properties
    {
        [PerRendererData] _MainTex ("Contact Quad", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _ShadowColor ("Contact Shadow Color", Color) = (0.025,0.14,0.17,1)
        _ShadowOpacity ("Contact Shadow Opacity", Range(0,0.5)) = 0.18
        _FoamColor ("Resting Ripple Color", Color) = (0.56,0.81,0.81,1)
        _FoamOpacity ("Resting Ripple Opacity", Range(0,0.5)) = 0.16
        _RippleSpeed ("Resting Ripple Cycles Per Second", Range(0,1)) = 0.24
        _RippleAmount ("Resting Ripple Size Amount", Range(0,0.1)) = 0.018
        _FoamWidth ("Resting Ripple Thickness", Range(0.01,0.12)) = 0.045
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
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            struct contact_v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 worldXY : TEXCOORD1;
                fixed4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            fixed4 _ShadowColor, _FoamColor;
            float _ShadowOpacity, _FoamOpacity, _RippleSpeed, _RippleAmount, _FoamWidth;

            contact_v2f vert(appdata_t v)
            {
                contact_v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float4 flipped = UnityFlipSprite(v.vertex, _Flip);
                o.vertex = UnityObjectToClipPos(flipped);
                o.worldXY = mul(unity_ObjectToWorld, flipped).xy;
                o.uv = v.texcoord;
                o.color = v.color * _Color * _RendererColor;
                return o;
            }

            fixed4 frag(contact_v2f i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;
                // Shadow stays close to the hull; only the surrounding water breathes.
                float shadowDistance = length(p / float2(0.94, 0.65));
                float shadow = (1.0 - smoothstep(0.48, 1.0, shadowDistance))
                    * _ShadowOpacity * _ShadowColor.a;

                float phase = _Time.y * _RippleSpeed * 6.283185
                    + i.worldXY.x * 1.8 + i.worldXY.y * 0.75;
                float breath = sin(phase);
                float rippleRadius = 1.0 + breath * _RippleAmount;
                float2 rippleP = float2(p.x / (0.96 * rippleRadius), (p.y + 0.02) / 0.55);
                float edge = abs(length(rippleP) - 1.0);
                float aa = max(fwidth(edge), 0.012);
                float ring = 1.0 - smoothstep(_FoamWidth, _FoamWidth + aa, edge);
                // Lower-facing broken arcs sit at the waterline instead of encircling the whole ship.
                float front = 1.0 - smoothstep(-0.10, 0.25, p.y);
                float fragments = smoothstep(0.15, 0.80,
                    sin(p.x * 11.0 + phase * 0.45) * 0.5 + 0.5);
                float foam = ring * front * fragments * _FoamOpacity * _FoamColor.a
                    * lerp(0.85, 1.0, breath * 0.5 + 0.5);
                foam *= 1.0 - smoothstep(0.88, 1.0, abs(p.x));

                float coverage = SampleSpriteTexture(i.uv).a * i.color.a;
                float alpha = foam + shadow * (1.0 - foam);
                float3 color = _FoamColor.rgb * foam + _ShadowColor.rgb * shadow * (1.0 - foam);
                return fixed4(color * i.color.rgb * coverage, alpha * coverage);
            }
            ENDCG
        }
    }
}
