Shader "Fleet/Smoke Cloud"
{
    Properties
    {
        _BodyColor ("Smoke Shadow", Color) = (0.23, 0.24, 0.25, 1)
        _LightColor ("Smoke Light", Color) = (0.42, 0.43, 0.44, 1)
        _Opacity ("Opacity Per Puff", Range(0,1)) = 0.32
        _DensityContrast ("Density Variation", Range(0.2,2)) = 0.9
        _RollSpeed ("Flow Speed", Range(0,2)) = 0.22
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
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
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            fixed4 _BodyColor, _LightColor;
            float _Opacity, _DensityContrast, _RollSpeed;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise(float2 p)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1,0)), f.x),
                    lerp(Hash(cell + float2(0,1)), Hash(cell + 1), f.x), f.y);
            }

            float Billow(float2 p)
            {
                float n = Noise(p) * 0.55;
                p = float2(p.x * 1.6 - p.y * 1.2, p.x * 1.2 + p.y * 1.6) + 7.3;
                n += Noise(p) * 0.28;
                p = float2(p.x * 1.6 - p.y * 1.2, p.x * 1.2 + p.y * 1.6) + 3.1;
                return n + Noise(p) * 0.17;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv * 2 - 1;
                float time = _Time.y * _RollSpeed;
                float2 flow = float2(time * 0.17, -time * 0.23);
                float2 warp = float2(Noise(p * 2.1 + flow), Noise(p * 2.1 + flow + 5.4)) - 0.5;
                float2 samplePoint = p * 3.2 + warp * 1.2 + flow;
                float billow = Billow(samplePoint);
                // Feather every edge and vary density continuously: no cloud outline or solid lobes.
                float feather = 1 - smoothstep(0.20, 1.0, dot(p, p));
                float density = saturate((billow - 0.24) * _DensityContrast * 2.4) * feather;
                float alpha = density * i.color.a * _Opacity;
                clip(alpha - 0.002);
                float light = saturate(0.45 + (billow - Billow(samplePoint + float2(-0.12,0.18))) * 2.5);
                fixed3 color = lerp(_BodyColor.rgb, _LightColor.rgb, light) * i.color.rgb;
                return fixed4(color, alpha);
            }
            ENDCG
        }
    }
}
