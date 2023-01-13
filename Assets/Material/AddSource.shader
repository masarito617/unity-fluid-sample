Shader "Unlit/AddSource"
{
    Properties
    {
        _Source ("Adding source", Vector) = (0, 1, 0.5, 0.5)
        _Radius ("Radius", Float) = 10
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

            float4 _Source; // 移動の大きさ(x,y), 現在のUV座標(z,w)
            float _Radius;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // (UV値 - 現在のマウス位置) の距離
                float2 dpdt = (i.uv - _Source.zw) / _Radius;
                // 遠くにあるほど小さくすることで筆のような塗り潰しにする
                return float4(
                    _Source.xy * saturate(1.0 - dot(dpdt, dpdt)), // 移動の大きさをかけているが、0〜1のため早く動かすと基本は小さくなる
                    saturate(1.0 - dot(dpdt, dpdt)), 0);
            }
            ENDCG
        }
    }
}
