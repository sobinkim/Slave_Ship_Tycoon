Shader "Fleet/Ship Cloth Waterline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [PerRendererData] _EnvironmentTint ("Environment Lighting", Color) = (1,1,1,1)
        _SpriteAlphaCutoff ("Sprite Edge Alpha Cutoff", Range(0,1)) = 0
        _SinkProgress ("Sink Progress", Range(0,1)) = 0
        _SinkDistance ("Sink Distance (Visual Local Units)", Float) = 0.9
        _Waterline ("Waterline (Visual Local Y)", Float) = -0.27
        [PerRendererData] _SurfaceSubmersion ("Surface Submersion", Range(0,1)) = 0
        [PerRendererData] _SurfacePlane ("Surface Plane In Hull Coordinates", Vector) = (0,1,0.4,0)
        [PerRendererData] _SurfaceParams ("Contact Depth, Fade Depth, Wave Height", Vector) = (0.055,0.025,0.008,0)
        [PerRendererData] _SurfaceTint ("Surface Water Tint", Color) = (0.2,0.52,0.57,0.55)
        [Header(Hit Emission)]
        [PerRendererData][HDR] _HitEmissionColor ("Hit Emission Color", Color) = (1,1,1,1)
        [PerRendererData] _HitEmissionIntensity ("Hit Emission Intensity", Range(0,8)) = 1.8
        [PerRendererData] _HitEmissionFlash ("Hit Emission Flash", Range(0,1)) = 0
        [Header(Cloth Motion)]
        _FlutterStrength ("State Flutter Strength", Range(0,1.5)) = 0.8
        _FlutterSpeed ("Wind Cycles Per Second", Range(0.1,4)) = 1.2
        _FlutterPhase ("Wind Phase (Radians)", Float) = 0
        _FlagAmplitude ("Flag Travel (Normalized Canvas)", Range(0,0.04)) = 0.012
        _SailAmplitude ("Sail Travel (Normalized Canvas)", Range(0,0.04)) = 0.008
        [Header(Cloth Regions In Image Coordinates)]
        _FlagBounds ("Flag Bounds (Left Top Right Bottom)", Vector) = (0,0,0,0)
        _FlagPoleTip ("Flag Pole X And Tip X", Vector) = (0,0,0,0)
        _SailTop ("Sail Top (Left XY Right XY)", Vector) = (0,0,0,0)
        _SailBottom ("Sail Bottom (Left XY Right XY)", Vector) = (0,0,0,0)
        _SailFootLift ("Sail Foot Curve Lift", Range(0,0.2)) = 0
        [Header(Sprite Mapping)]
        _ClothCanvas ("Hull Canvas (Local Width Height Pivot XY)", Vector) = (1,1,0.5,0.5)
        _ClothTransform ("Renderer To Hull (Scale XY Offset XY)", Vector) = (1,1,0,0)
        _ClothUvScale ("Canvas Displacement To Texture UV", Vector) = (1,1,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" "DisableBatching"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; float2 localXY : TEXCOORD1; float2 cloth : TEXCOORD2; float worldX : TEXCOORD3; };
            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _EnvironmentTint;
            float _SinkProgress, _SinkDistance, _Waterline, _SurfaceSubmersion;
            float4 _SurfacePlane, _SurfaceParams;
            fixed4 _SurfaceTint;
            fixed4 _HitEmissionColor;
            float _HitEmissionIntensity, _HitEmissionFlash;
            float _SpriteAlphaCutoff;
            float _FlutterStrength, _FlutterSpeed, _FlutterPhase, _FlagAmplitude, _SailAmplitude, _SailFootLift;
            float4 _FlagBounds, _FlagPoleTip, _SailTop, _SailBottom, _ClothCanvas, _ClothTransform, _ClothUvScale;
            v2f vert(appdata v)
            {
                v2f o;
                float2 hullPosition = v.vertex.xy * _ClothTransform.xy + _ClothTransform.zw;
                o.cloth = hullPosition / _ClothCanvas.xy + _ClothCanvas.zw;
                o.cloth.y = 1 - o.cloth.y;
                v.vertex.y -= _SinkProgress * _SinkDistance;
                o.localXY = v.vertex.xy;
                o.worldX = mul(unity_ObjectToWorld, v.vertex).x;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }
            float2 ClothDisplacement(float2 p)
            {
                float phase = _Time.y * 6.2831853 * _FlutterSpeed + _FlutterPhase;
                float2 flagSize = max(_FlagBounds.zw - _FlagBounds.xy, 0.0001);
                float2 flagPoint = (p - _FlagBounds.xy) / flagSize;
                float flagMask = smoothstep(0, 0.08, flagPoint.x) * (1 - smoothstep(0.92, 1, flagPoint.x))
                    * smoothstep(0, 0.12, flagPoint.y) * (1 - smoothstep(0.88, 1, flagPoint.y));
                float poleToTip = _FlagPoleTip.y - _FlagPoleTip.x;
                float flagFree = saturate((p.x - _FlagPoleTip.x) / (abs(poleToTip) > 0.0001 ? poleToTip : 1));
                float2 flag = float2(0.3 * sin(phase - flagFree * 5), sin(phase * 1.25 - flagFree * 7))
                    * flagFree * flagMask * _FlagAmplitude;
                float sailWidth = max(_SailTop.z - _SailTop.x, 0.0001);
                float u = (p.x - _SailTop.x) / sailWidth;
                float topY = lerp(_SailTop.y, _SailTop.w, u);
                float bottomY = lerp(_SailBottom.y, _SailBottom.w, u) - 4 * saturate(u) * (1 - saturate(u)) * _SailFootLift;
                float v = (p.y - topY) / max(bottomY - topY, 0.0001);
                float leftX = lerp(_SailTop.x, _SailBottom.x, saturate(v));
                float rightX = lerp(_SailTop.z, _SailBottom.z, saturate(v));
                float side = (p.x - leftX) / max(rightX - leftX, 0.0001);
                float sailMask = smoothstep(-0.05, 0.02, side) * (1 - smoothstep(0.98, 1.05, side))
                    * smoothstep(0, 0.22, v) * (1 - smoothstep(1, 1.1, v));
                float2 sail = float2(sin(phase - v * 4 + u * 3), 0.35 * sin(phase + u * 5))
                    * saturate(v) * sailMask * _SailAmplitude;
                return (flag + sail) * _FlutterStrength;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                // Normal sailing uses an authored shallow surface; retain the original sinking fallback.
                if (_SurfaceSubmersion < 0.5 && _SinkProgress > 0.0001) clip(i.localXY.y - _Waterline);
                float2 displacement = ClothDisplacement(i.cloth);
                float2 uv = i.uv - float2(displacement.x, -displacement.y) * _ClothUvScale.xy;
                fixed4 sprite = tex2D(_MainTex, uv);
                clip(sprite.a - _SpriteAlphaCutoff);
                fixed4 result = sprite * i.color;
                if (_SurfaceSubmersion > 0.5)
                {
                    float wave = sin(i.worldX * 14 + _Time.y * 2.4) * _SurfaceParams.z;
                    float depth = dot(float3(i.localXY, 1), _SurfacePlane.xyz) - wave;
                    float fade = max(0.001, _SurfaceParams.y);
                    clip(depth + fade);
                    float contact = 1 - smoothstep(0, max(0.001, _SurfaceParams.x), depth);
                    result.rgb = lerp(result.rgb, _SurfaceTint.rgb, contact * _SurfaceTint.a);
                    result.a *= smoothstep(-fade, 0, depth);
                }
                result.rgb += _HitEmissionColor.rgb * _HitEmissionIntensity * saturate(_HitEmissionFlash) * sprite.a;
                result.rgb *= _EnvironmentTint.rgb;
                return result;
            }
            ENDCG
        }
    }
}
