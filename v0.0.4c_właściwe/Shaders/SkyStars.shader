Shader "BKG/SkyStars"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        //gwiazdy to male kwadraty z kolorem wierzcholkow (jasnosc); rysowane przed sloncem i terenem, bez mgly, a ponizej horyzontu gasna
        Tags { "Queue" = "Background" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR0;
            };

            v2f vert (appdata v)
            {
                v2f o;

                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 dir = normalize(world - _WorldSpaceCameraPos);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.color.a *= saturate((dir.y + 0.03) / 0.15);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.color;
            }
            ENDCG
        }
    }
}
