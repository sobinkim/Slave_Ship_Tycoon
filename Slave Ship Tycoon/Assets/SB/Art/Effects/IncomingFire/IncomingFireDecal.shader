Shader "SB/IncomingFireDecal"
{
    Properties
    {
        _WarningColor("Warning color", Color) = (1,.06,.08,1)
        _RingRadius("Ring radius", Range(.2,.85)) = .70
        _CoreWidth("Ring width", Range(.005,.2)) = .035
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            float4 _WarningColor;
            float _RingRadius, _CoreWidth;
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            float4 frag(v2f i):SV_Target
            {
                float radius=length(i.uv*2-1);
                float distanceToRing=abs(radius-_RingRadius);
                float edge=max(fwidth(radius),.001);
                float coverage=1-smoothstep(_CoreWidth*.5-edge,_CoreWidth*.5+edge,distanceToRing);
                return float4(_WarningColor.rgb,_WarningColor.a*coverage);
            }
            ENDCG
        }
    }
}
