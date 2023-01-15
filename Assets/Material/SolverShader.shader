Shader "StableFluid/SolverShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BgColor ("BgColor", Color) = (1,1,1,1)
        _AddColor ("AddColor", Color) = (1,1,1,1)
        _Strength ("Strength", float) = 3
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D SolverTex;

            float4 _BgColor;
            float4 _AddColor;
            float _Strength;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed3 solver = tex2D(SolverTex, i.uv);
                fixed4 col = _BgColor;
                col.xyz += _AddColor * solver.z * _Strength;
                col.a = 1;
                return col;
            }
            ENDCG
        }
    }
}
