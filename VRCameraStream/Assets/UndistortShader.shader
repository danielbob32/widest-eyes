Shader "Custom/UndistortShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Distortion ("Distortion Amount", Float) = -0.3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Distortion;

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv * 2.0 - 1.0; // Normalize UVs to -1 to 1
                float r = length(uv);
                float theta = atan2(uv.y, uv.x);

                // Apply distortion correction
                r = pow(r, 1.0 + _Distortion);

                uv = r * float2(cos(theta), sin(theta));
                uv = uv * 0.5 + 0.5; // Convert back to 0 to 1 range

                return tex2D(_MainTex, uv);
            }
            ENDCG
        }
    }
}
