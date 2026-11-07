Shader "BKG/SkyClouds"
{
    Properties
    {
        _MainTex ("Cloud Pattern", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _TileSize ("Blocks Per Tile", Float) = 768
        _Wind ("Wind (x z blocks per second)", Vector) = (2.5, 0.8, 0, 0)
        _FadeStart ("Fade Start", Float) = 450
        _FadeEnd ("Fade End", Float) = 900
    }

    SubShader
    {
        //chmury to plaska warstwa ze wzorem w wspolrzednych swiata (nie jedzie za kamera), przesuwana wiatrem; rysowana za sloncem, przed terenem, bez mgly,
        //a z odlegloscia od kamery wygasa, zeby krawedz warstwy nie bylo widac
        Tags { "Queue" = "Background+2" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

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

            sampler2D _MainTex;
            fixed4 _Color;
            float _TileSize;
            float4 _Wind;
            float _FadeStart;
            float _FadeEnd;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = (i.world.xz - _Wind.xy * _Time.y) / _TileSize;
                fixed4 c = tex2D(_MainTex, uv) * _Color;
                float dist = length(i.world.xz - _WorldSpaceCameraPos.xz);

                c.a *= 1.0 - smoothstep(_FadeStart, _FadeEnd, dist);

                return c;
            }
            ENDCG
        }
    }
}
