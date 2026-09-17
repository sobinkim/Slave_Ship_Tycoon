Shader "SB/Fleet Shield"
{
    Properties
    {
        _Tint ("Tint", Color) = (0.1,0.55,1,0.65)
        _GridDensity ("Hexagon density", Float) = 7
        _GridStrength ("Hexagon opacity", Range(0,1)) = 0.22
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            fixed4 _Tint;
            float _GridDensity, _GridStrength;
            v2f vert(appdata v)
            {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=i.uv*_GridDensity;
                float2 repeat=float2(1.7320508,3);
                float2 a=p-repeat*floor(p/repeat+0.5);
                float2 b=p-float2(.8660254,1.5);
                b-=repeat*floor(b/repeat+0.5);
                float2 h=dot(a,a)<dot(b,b)?a:b;
                h=abs(h);
                float edge=max(dot(h,float2(.5,.8660254)),h.x);
                float lines=1-smoothstep(.025,.055,abs(edge-.8660254));
                float highlight=i.color.r;
                float3 rgb=lerp(_Tint.rgb, float3(.94,1,1), highlight*.3);
                float alpha=saturate(i.color.a+lines*_GridStrength*i.color.g)*_Tint.a;
                return fixed4(rgb,alpha);
            }
            ENDCG
        }
    }
}
