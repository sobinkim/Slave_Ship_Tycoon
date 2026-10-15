Shader "Fleet/Sea Creature Hit"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline", Color) = (0.19,0.16,0.12,1)
        _OutlineWidth ("Outline Width", Range(0,0.12)) = 0.045
        [Enum(Contact Burst,0,Water Chip,1)] _Shape ("Shape", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _Shape;

            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 centeredUv = input.uv - 0.5;
                float angle = atan2(centeredUv.y, centeredUv.x);
                float rays = pow(abs(cos(angle * 4.0)), 6.0);
                float burstDistance = length(centeredUv) - (0.22 + rays * 0.26);
                float chipDistance = abs(centeredUv.x) * 0.9 + abs(centeredUv.y) * 0.8 - 0.31;
                float signedDistance = lerp(burstDistance, chipDistance, step(0.5, _Shape));
                float edge = max(fwidth(signedDistance), 0.002);
                float coverage = 1.0 - smoothstep(-edge, edge, signedDistance);
                float fill = 1.0 - smoothstep(-_OutlineWidth - edge, -_OutlineWidth + edge, signedDistance);
                fixed3 color = lerp(_OutlineColor.rgb, input.color.rgb, fill);
                return fixed4(color, coverage * input.color.a);
            }
            ENDCG
        }
    }
}
