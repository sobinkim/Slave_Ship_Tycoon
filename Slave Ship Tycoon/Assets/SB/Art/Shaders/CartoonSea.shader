Shader "Fleet/Cartoon Sea"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Alpha", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FarColor ("Horizon Color", Color) = (0.28,0.61,0.65,1)
        _NearColor ("Foreground Color", Color) = (0.065,0.32,0.41,1)
        _HorizonY ("Horizon World Y", Float) = 3.16
        _DepthHeight ("Depth Height (World Units)", Float) = 5.56
        _BandStrength ("Broad Current Strength", Range(0,0.15)) = 0.025
        _RippleColor ("Ripple Color", Color) = (0.57,0.85,0.85,1)
        _RippleOpacity ("Ripple Opacity", Range(0,1)) = 0.32
        _RippleDensity ("Ripple Density", Range(0,1)) = 0.42
        _RippleScale ("Ripple Spacing", Range(0.3,3)) = 1
        _RippleLength ("Ripple Length", Range(0.1,0.34)) = 0.3
        _RippleThickness ("Ripple Thickness", Range(0.01,0.1)) = 0.035
        _RippleSpeed ("Ripple Drift Speed", Range(0,1)) = 0.34
        _SwaySpeed ("Wave Sway Speed", Range(0,3)) = 1.4
        _SwayAmount ("Wave Sway Amount", Range(0,0.3)) = 0.12
        _CurrentSpeed ("Broad Current Speed", Range(0,2)) = 0.45
        [PerRendererData] _TravelOffset ("Sailing Distance", Float) = 0
        _SwellScale ("Large Wave Size", Range(0.5,2)) = 1
        _SwellStrength ("Large Wave Shading", Range(0,1)) = 0.5
        _WaveLifetime ("Wave Lifetime (Seconds)", Range(3,14)) = 7
        _FoamColor ("Crest Foam Color", Color) = (0.68,0.9,0.88,1)
        _FoamOpacity ("Crest Foam Opacity", Range(0,1)) = 0.48
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
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 worldXY : TEXCOORD1;
                fixed4 color : COLOR;
            };
            sampler2D _MainTex;
            fixed4 _Color, _FarColor, _NearColor, _RippleColor, _FoamColor;
            float _HorizonY, _DepthHeight, _BandStrength;
            float _RippleOpacity, _RippleDensity, _RippleScale, _RippleLength, _RippleThickness, _RippleSpeed;
            float _SwaySpeed, _SwayAmount, _CurrentSpeed;
            float _TravelOffset;
            float _SwellScale, _SwellStrength, _WaveLifetime, _FoamOpacity;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldXY = mul(unity_ObjectToWorld, v.vertex).xy;
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            float2 Hash2(float2 cell)
            {
                float3 p = frac(float3(cell.x, cell.y, cell.x) * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yzx + 33.33);
                return frac((p.xx + p.yz) * p.zy);
            }

            // x: lit wave body, y: crest, z: broken foam, w: shadow below the crest.
            float4 Wavelet(float2 p, float2 cell, float time, float lengthScale)
            {
                float2 seed = Hash2(cell);
                float variation = frac(seed.x * 17.43 + seed.y * 4.27);
                float lifetime = max(_WaveLifetime, 0.01) * lerp(0.75, 1.3, variation);
                float phase = frac(time / lifetime + seed.x);
                float life = smoothstep(0.0, 0.22, phase) * (1.0 - smoothstep(0.62, 1.0, phase));
                float growth = lerp(0.6, 1.0, smoothstep(0.0, 0.5, phase));
                float2 local = p - cell - 0.5 - (seed - 0.5) * float2(0.74, 0.26);
                float halfLength = _RippleLength * lengthScale * lerp(0.8, 1.2, variation) * growth;
                float x = local.x / max(halfLength, 0.01);
                float cap = saturate(1.0 - x * x);
                float taper = sqrt(cap);
                float bend = 0.06 + 0.025 * sin(time * _SwaySpeed * 0.6 + seed.y * 6.283);
                float crestY = bend * sin(x * 2.1 + seed.y * 2.4)
                    + (variation - 0.5) * 0.12 * x + 0.025 * x * cap;
                float edgeDistance = abs(local.y - crestY);
                // Differentiate the continuous domain, so neighbor-cell seeds cannot widen a crest at a seam.
                float aa = max(fwidth(p.y) + fwidth(p.x) * 0.3 / max(halfLength, 0.01), 0.003);
                float thickness = _RippleThickness * lerp(0.7, 1.4, variation) * taper * growth
                    * lerp(0.45, 1.2, saturate(x * 0.5 + 0.5));
                float crest = 1.0 - smoothstep(thickness, thickness + aa, edgeDistance);
                float body = 1.0 - smoothstep(0.025 * taper, 0.13 * taper + aa,
                    abs(local.y - crestY + 0.055));
                float shadow = 1.0 - smoothstep(0.015 * taper, 0.05 * taper + aa,
                    abs(local.y - crestY + 0.07));
                float foamLife = smoothstep(0.16, 0.4, phase) * (1.0 - smoothstep(0.48, 0.82, phase));
                float foamBreaks = smoothstep(-0.2, 0.45, sin(x * 8.0 + seed.y * 12.0 + phase * 3.0));
                crest *= lerp(0.35, 1.0, foamBreaks);
                float foam = crest * foamBreaks * foamLife * step(variation, 0.35);
                // Select density independently from the position, avoiding repeated rows of low crests.
                float occupancy = Hash2(cell + float2(19.37, 5.73)).x;
                float envelope = smoothstep(0.0, 0.3, cap) * life * step(occupancy, _RippleDensity);
                return float4(body, crest, foam, shadow) * envelope;
            }

            float4 WaveLayer(float2 p, float time, float lengthScale)
            {
                float2 cell = floor(p);
                float4 waves = 0;
                // Horizontal neighbors let long crests cross cells; vertical extents stay within their row.
                [unroll]
                for (int x = -1; x <= 1; x++)
                    waves = max(waves, Wavelet(p, cell + float2(x, 0), time, lengthScale));
                return waves;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float depth = saturate((_HorizonY - i.worldXY.y) / max(_DepthHeight, 0.01));
                float motionFade = smoothstep(0.0, 0.18, depth);
                float perspective = lerp(0.55, 1.0, depth);
                float3 water = lerp(_FarColor.rgb, _NearColor.rgb, smoothstep(0.0, 1.0, depth));
                float time = _Time.y;
                float2 travelWorld = i.worldXY;
                // Project the stationary field first. Depth-weighting accumulated travel
                // shears the field more each second and stretches short waves into streaks.
                float cameraX = _WorldSpaceCameraPos.x;
                travelWorld.x = (travelWorld.x - cameraX) / perspective + cameraX;
                travelWorld.x += _TravelOffset;
                float currentPhase = time * _CurrentSpeed;
                float current = sin(travelWorld.y * 2.6 - currentPhase
                    + sin(travelWorld.x * 0.7 - currentPhase * 0.6) * 0.7) * 0.65;
                current += sin(travelWorld.y * 4.1 + travelWorld.x * 0.45
                    - currentPhase * 0.73) * 0.35;
                water += current * _BandStrength * float3(0.4, 0.7, 0.8);

                // Both wave layers share a continuous world-space sway, across mirrored parallax tiles.
                float2 world = travelWorld;
                world.y += sin(world.x * 1.3 - time * _SwaySpeed) * _SwayAmount * 0.34 * motionFade;
                world.x += sin(world.y * 1.7 - time * _SwaySpeed * 0.73) * _SwayAmount * 0.2 * motionFade;
                float2 largeP = float2(world.x + world.y * 0.28, world.y)
                    / (float2(1.9, 0.85) * max(_SwellScale, 0.01));
                largeP += float2(time * _RippleSpeed * 0.35, 0.0);
                float4 largeWaves = WaveLayer(largeP, time, 1.6);
                float2 smallP = float2(world.x + world.y * 0.18, world.y)
                    / (float2(0.95, 0.46) * max(_RippleScale, 0.01));
                smallP += float2(time * _RippleSpeed * 0.8 + 17.3, -time * _RippleSpeed * 0.06 + 23.7);
                float4 smallWaves = WaveLayer(smallP, time * 1.13 + 3.7, 1.0);
                float visibility = lerp(0.18, 1.0, motionFade);
                float body = max(largeWaves.x, smallWaves.x * 0.4) * _SwellStrength * visibility;
                float shadow = max(largeWaves.w, smallWaves.w * 0.35) * _SwellStrength * visibility;
                water *= 1.0 - shadow * 0.12;
                water = lerp(water, _RippleColor.rgb, body * 0.2);
                float crest = max(largeWaves.y * 0.65, smallWaves.y) * visibility;
                water = lerp(water, _RippleColor.rgb, crest * _RippleOpacity);
                float foam = max(largeWaves.z * 0.65, smallWaves.z) * visibility;
                water = lerp(water, _FoamColor.rgb, foam * _FoamOpacity);

                float alpha = tex2D(_MainTex, i.uv).a * i.color.a;
                return fixed4(water * i.color.rgb, alpha);
            }
            ENDCG
        }
    }
}
