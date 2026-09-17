Shader "Fleet/Ship Waterline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _SinkProgress ("Sink Progress", Range(0,1)) = 0
        _SinkDistance ("Sink Distance (Visual Local Units)", Float) = 0.9
        _Waterline ("Waterline (Visual Local Y)", Float) = -0.27
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
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; float localY : TEXCOORD1; };
            sampler2D _MainTex;
            fixed4 _Color;
            float _SinkProgress, _SinkDistance, _Waterline;
            v2f vert(appdata v)
            {
                v2f o;
                v.vertex.y -= _SinkProgress * _SinkDistance;
                o.localY = v.vertex.y;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                if (_SinkProgress > 0.0001) clip(i.localY - _Waterline);
                return tex2D(_MainTex, i.uv) * i.color;
            }
            ENDCG
        }
    }
}
