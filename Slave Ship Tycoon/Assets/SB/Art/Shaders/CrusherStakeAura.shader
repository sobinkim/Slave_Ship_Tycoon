Shader "Fleet/Crusher Stake Aura"
{
    Properties
    {
        [PerRendererData] _MainTex ("Stake Sprite", 2D) = "white" {}
        _Color ("Sprite Tint", Color) = (1,1,1,1)
        _AuraColor ("Aura Color", Color) = (1,0.075,0.10,1)
        _RimColor ("Rim Highlight", Color) = (1,0.38,0.40,1)
        _ShieldCenter ("Shield UV Center", Vector) = (0.5,0.5,0,0)
        _ShieldHalfSize ("Shield UV Half Size", Vector) = (0.35,0.20,0,0)
        _EdgeWidth ("Edge Width", Range(0.01,0.25)) = 0.065
        _GlowWidth ("Glow Width", Range(0.01,0.5)) = 0.15
        _GlowOpacity ("Glow Opacity", Range(0,1)) = 0.4
        _FillOpacity ("Shell Opacity", Range(0,1)) = 0.32
        [PerRendererData] _CoatingProgress ("Coating Progress", Range(0,1)) = 0
        _RevealSoftness ("Coating Front Softness", Range(0.01,0.5)) = 0.16
        _HexDensity ("Hexagon Density", Range(1,12)) = 3.5
        _HexWidth ("Hexagon Line Width", Range(0.005,0.12)) = 0.018
        _HexOpacity ("Hexagon Opacity", Range(0,1)) = 0.3
        _PulseAmount ("Pulse Amount", Range(0,0.5)) = 0.10
        _PulseSpeed ("Pulse Speed", Range(0,10)) = 3
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex;
            fixed4 _Color, _AuraColor, _RimColor;
            float4 _ShieldCenter, _ShieldHalfSize;
            float _EdgeWidth, _GlowWidth, _GlowOpacity, _FillOpacity;
            float _HexDensity, _HexWidth, _HexOpacity, _PulseAmount, _PulseSpeed;
            float _CoatingProgress, _RevealSoftness;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv;
                o.color=v.color*_Color;
                return o;
            }
            float edgeDistance(float2 p, float2 a, float2 b)
            {
                float2 edge=b-a;
                return dot(p-a,float2(edge.y,-edge.x))/length(edge);
            }
            float shellDistance(float2 p)
            {
                // Counter-clockwise outline: flat rear, beveled corners and pointed nose.
                float d=edgeDistance(p,float2(-1,-0.65),float2(-0.78,-1));
                d=max(d,edgeDistance(p,float2(-0.78,-1),float2(0.42,-0.90)));
                d=max(d,edgeDistance(p,float2(0.42,-0.90),float2(1,0)));
                d=max(d,edgeDistance(p,float2(1,0),float2(0.42,0.90)));
                d=max(d,edgeDistance(p,float2(0.42,0.90),float2(-0.78,1)));
                d=max(d,edgeDistance(p,float2(-0.78,1),float2(-1,0.65)));
                return max(d,edgeDistance(p,float2(-1,0.65),float2(-1,-0.65)));
            }
            float hexLines(float2 p)
            {
                const float2 cell=float2(1.7320508,3);
                float2 a=p-cell*floor(p/cell)-cell*0.5;
                float2 q=p-float2(0.8660254,1.5);
                float2 b=q-cell*floor(q/cell)-cell*0.5;
                float2 h=dot(a,a)<dot(b,b)?a:b;
                h=abs(h);
                float edge=0.8660254-max(h.x,dot(h,float2(0.5,0.8660254)));
                return 1-smoothstep(_HexWidth,_HexWidth+fwidth(edge),edge);
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=(i.uv-_ShieldCenter.xy)/max(_ShieldHalfSize.xy,0.001);
                float d=shellDistance(p);
                float aa=max(fwidth(d),0.002);
                float inside=1-smoothstep(-aa,aa,d);
                float border=1-smoothstep(_EdgeWidth,_EdgeWidth+aa,abs(d));
                float glow=exp(-max(d,0)/max(_GlowWidth,0.001))*_GlowOpacity;
                float uvEdge=min(min(i.uv.x,1-i.uv.x),min(i.uv.y,1-i.uv.y));
                glow*=smoothstep(0,0.06,uvEdge);
                float pulse=1-_PulseAmount+_PulseAmount*(0.5+0.5*sin(_Time.y*_PulseSpeed));
                // The sprite points along +X: coat the nose first, then the rear collar.
                float front=lerp(1+_RevealSoftness,-1-_RevealSoftness,_CoatingProgress);
                float reveal=smoothstep(front-_RevealSoftness,front+_RevealSoftness,p.x);
                reveal*=step(0.0001,_CoatingProgress);
                reveal=lerp(reveal,1,step(0.9999,_CoatingProgress));
                float hex=hexLines(float2(p.x*1.7,p.y)*_HexDensity)*inside*_HexOpacity;
                float rim=1-smoothstep(_EdgeWidth*0.2,_EdgeWidth*0.2+aa,abs(d+_EdgeWidth*0.35));
                float sweep=(1-smoothstep(0,_RevealSoftness,abs(p.x-front)))*inside;
                sweep*=step(0.0001,_CoatingProgress)*(1-step(0.9999,_CoatingProgress));
                float shellAlpha=saturate(max(max(border,inside*_FillOpacity),hex)+(1-inside)*glow);
                shellAlpha=max(shellAlpha,sweep*0.6)*_AuraColor.a*pulse*reveal;
                float3 shellColor=lerp(_AuraColor.rgb,_RimColor.rgb,saturate(max(rim,hex)+sweep*0.6));
                fixed4 metal=tex2D(_MainTex,i.uv)*i.color;
                // The coating is over the metal, so its filled face remains visible on opaque pixels.
                float alpha=shellAlpha+metal.a*(1-shellAlpha);
                float3 premul=shellColor*shellAlpha+metal.rgb*metal.a*(1-shellAlpha);
                return fixed4(premul/max(alpha,0.0001),alpha);
            }
            ENDCG
        }
    }
}
