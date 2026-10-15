Shader "Fleet/Sea Creature Surface Body"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _SwimPhase ("Stroke Phase", Float) = 0
        _SwimStrength ("Tail Bend", Range(0,0.05)) = 0.003
        _TailStart ("Tail Start In Sprite UV", Range(0,1)) = 0.38
        _TailBendMultiplier ("Tail Bend Multiplier", Range(0,2)) = 1
        _SinkProgress ("Sink Progress", Range(0,1)) = 0
        _SinkDistance ("Sink Distance", Float) = 0.4
        _Waterline ("Local Water Height", Float) = -0.2
        _WaterTint ("Water Contact Tint", Color) = (0.15,0.56,0.6,0.65)
        _ContactDepth ("Contact Tint Depth", Float) = 0.07
        [HDR] _HitEmissionColor ("Hit Emission Color", Color) = (1,0.86,0.56,1)
        _HitEmissionIntensity ("Hit Emission Intensity", Range(0,8)) = 1.8
        _HitEmissionFlash ("Hit Emission Flash", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" "DisableBatching"="True" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; float localY:TEXCOORD1; float2 world:TEXCOORD2; };
            sampler2D _MainTex;
            fixed4 _Color, _WaterTint;
            float4 _HitEmissionColor;
            float _SwimPhase, _SwimStrength, _TailStart, _TailBendMultiplier;
            float _SinkProgress, _SinkDistance, _Waterline, _ContactDepth;
            float _HitEmissionIntensity, _HitEmissionFlash;
            v2f vert(appdata v)
            {
                v2f o;
                v.vertex.y -= _SinkProgress * _SinkDistance;
                o.localY = v.vertex.y;
                o.world = mul(unity_ObjectToWorld,v.vertex).xy;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }
            float4 frag(v2f i):SV_Target
            {
                float tail = smoothstep(_TailStart,0.95,i.uv.x);
                float2 uv = i.uv;
                uv.y -= sin(_SwimPhase - i.uv.x * 8) * tail * _SwimStrength * _TailBendMultiplier;
                fixed4 spriteColor = tex2D(_MainTex,uv);
                float4 color = spriteColor * i.color;
                clip(color.a - 0.08);
                float surface = _Waterline + sin(i.world.x * 17 + _Time.y * 3) * 0.006;
                float depth = i.localY - surface;
                clip(depth);
                float contact = 1 - smoothstep(0,max(0.001,_ContactDepth),depth);
                color.rgb = lerp(color.rgb,_WaterTint.rgb,contact * _WaterTint.a);
                // Emit across the entire visible sprite, including its dark outline.
                // Sprite alpha and the clipped waterline still define the visible silhouette.
                color.rgb += _HitEmissionColor.rgb * _HitEmissionIntensity
                    * saturate(_HitEmissionFlash);
                color.a *= smoothstep(0,0.008,depth);
                return color;
            }
            ENDCG
        }
    }
}
