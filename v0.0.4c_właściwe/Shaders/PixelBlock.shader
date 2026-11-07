Shader "BKG/PixelBlock"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _TopShade ("Top Shade", Range(0, 1)) = 1
        _SideShadeA ("North/South Shade", Range(0, 1)) = 0.8
        _SideShadeB ("East/West Shade", Range(0, 1)) = 0.6
        _BottomShade ("Bottom Shade", Range(0, 1)) = 0.5
        _NightColor ("Night Multiplier", Color) = (0.28, 0.34, 0.55, 1)
        _Light ("Light Multiplier", Range(0, 1)) = 1
        [HideInInspector] _SrcBlend ("Src Blend", Float) = 1
        [HideInInspector] _DstBlend ("Dst Blend", Float) = 0
        [HideInInspector] _ZWrite ("Z Write", Float) = 1
        [HideInInspector] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _TopShade;
            float _SideShadeA;
            float _SideShadeB;
            float _BottomShade;
            fixed4 _NightColor;
            float _Light;
            float _BKG_NightDarkness;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float shade : TEXCOORD1;
                float light : TEXCOORD3;
                UNITY_FOG_COORDS(2)
            };

            v2f vert (appdata v)
            {
                v2f o;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                //swiatlo i AO ChunkMesher zapisuje w kolorze wierzcholkow (gladko interpolowane po scianie)
                o.light = v.color.r * _Light;

                float3 n = UnityObjectToWorldNormal(v.normal);
                float3 a = abs(n);

                if (a.y > a.x && a.y > a.z)
                    o.shade = n.y > 0 ? _TopShade : _BottomShade;
                else
                    o.shade = a.x > a.z ? _SideShadeB : _SideShadeA;

                UNITY_TRANSFER_FOG(o, o.pos);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * _Color;

                c.rgb *= i.shade;
                c.rgb *= lerp(fixed3(1, 1, 1), _NightColor.rgb, _BKG_NightDarkness * i.light);
                c.rgb *= i.light;

                UNITY_APPLY_FOG(i.fogCoord, c);

                return c;
            }
            ENDCG
        }
    }
}
