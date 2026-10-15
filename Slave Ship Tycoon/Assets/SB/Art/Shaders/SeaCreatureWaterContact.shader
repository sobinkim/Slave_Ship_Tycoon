Shader "Fleet/Sea Creature Water Contact"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _SurfaceKind ("0 Shadow, 1 Broken Ring, 2 Foam", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            fixed4 _Color; float _SurfaceKind;
            v2f vert(appdata v) {v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=i.uv*2-1;
                float r=length(p);
                float angle=atan2(p.y,p.x);
                float alpha;
                if(_SurfaceKind<0.5) alpha=(1-smoothstep(0.28,1,r))*0.7;
                else if(_SurfaceKind<1.5)
                {
                    float ring=smoothstep(0.55,0.67,r)*(1-smoothstep(0.84,0.96,r));
                    float gaps=smoothstep(-0.25,0.4,sin(angle*5+0.8));
                    alpha=ring*gaps;
                }
                else
                {
                    float edge=0.75+0.1*sin(angle*3+1);
                    alpha=(1-smoothstep(edge-0.16,edge,r))*(0.75+0.25*sin(p.x*7));
                }
                return fixed4(i.color.rgb,i.color.a*alpha);
            }
            ENDCG
        }
    }
}
