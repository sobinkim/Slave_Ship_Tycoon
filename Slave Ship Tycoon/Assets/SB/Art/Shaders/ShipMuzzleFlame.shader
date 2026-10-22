Shader "Fleet/Ship Muzzle Flame"
{
    Properties
    {
        _CoreColor ("Hot Core", Color) = (1, 0.95, 0.76, 1)
        _EdgeColor ("Flame Edge", Color) = (1, 0.24, 0.035, 1)
        _Brightness ("Brightness", Range(0, 4)) = 1.8
        _Opacity ("Opacity", Range(0, 1)) = 1
        _FlowSpeed ("Flame Flow", Range(0, 12)) = 7
        _NoiseScale ("Flame Detail", Range(1, 16)) = 7
        [Toggle] _Radial ("Soft Glow", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            fixed4 _CoreColor, _EdgeColor;
            float _Brightness, _Opacity, _FlowSpeed, _NoiseScale, _Radial;

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

            fixed4 frag(v2f i) : SV_Target
            {
                float x = i.uv.x;
                float y = (i.uv.y - 0.5) * 2;
                float flow = _Time.y * _FlowSpeed;
                float n = Noise(float2(x * _NoiseScale - flow, y * _NoiseScale * 0.6));
                float detail = Noise(float2(x * _NoiseScale * 2 - flow * 1.7, y * _NoiseScale * 1.3));
                float lateral = abs(y + (n - 0.5) * x * 0.3);
                float width = (0.17 + 0.5 * sin(x * UNITY_PI)) * pow(saturate(1 - x), 0.4);
                float flame = 1 - smoothstep(width * 0.3, width + 0.08, lateral);
                flame *= smoothstep(0, 0.055, x) * pow(saturate(1 - x), 0.35);
                flame *= saturate(n * 0.9 + detail * 0.7 - 0.08);
                float core = exp(-5 * lateral * lateral / max(0.01, width * width)) * (1 - x);
                float2 centered = i.uv * 2 - 1;
                float glow = saturate(exp(-3.5 * dot(centered, centered)) - 0.025);
                float density = lerp(flame, glow, _Radial);
                float heat = lerp(pow(saturate(core), 0.65), 0.55, _Radial);
                float alpha = density * i.color.a * _Opacity;
                clip(alpha - 0.002);
                fixed3 color = lerp(_EdgeColor.rgb, _CoreColor.rgb, heat);
                return fixed4(color * i.color.rgb * _Brightness, alpha);
            }
            ENDCG
        }
    }
}
