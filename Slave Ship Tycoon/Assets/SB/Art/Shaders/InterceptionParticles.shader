Shader "Fleet/Interception Particles"
{
    Properties
    {
        [HDR] _Color ("Tint", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(0,3)) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DestinationBlend ("Destination Blend", Float) = 10
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha [_DestinationBlend]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; };
            float4 _Color;
            float _Brightness;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                return o;
            }
            half4 frag(v2f i) : SV_Target
            {
                return half4(i.color.rgb * _Brightness, i.color.a);
            }
            ENDCG
        }
    }
}
