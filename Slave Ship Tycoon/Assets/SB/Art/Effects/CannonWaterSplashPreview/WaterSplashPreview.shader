Shader "SB/WaterSplashPreview"
{
    Properties
    {
        _MainTex("4 x 2 splash sheet", 2D) = "white" {}
        _SecondRowOffset("Second row baseline correction (texture UV)", Float) = 0.048828125
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
            sampler2D _MainTex;
            float _SecondRowOffset;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                // The generated second row's waterline is 50 pixels higher; align it at sampling time.
                if(i.uv.y<0.5) { i.uv.y+=_SecondRowOffset; if(i.uv.y>=0.5) return 0; }
                return tex2D(_MainTex,i.uv)*i.color;
            }
            ENDCG
        }
    }
}
