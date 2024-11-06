Shader "Custom/RightEyeUndistortShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _K1 ("K1", Float) = -0.15
        _K2 ("K2", Float) = 0.05
        _P1 ("P1", Float) = 0.0
        _P2 ("P2", Float) = 0.0
        _CenterX ("Center X", Float) = 0.5
        _CenterY ("Center Y", Float) = 0.5
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

            float _K1, _K2, _P1, _P2;
            float _CenterX, _CenterY;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float2 undistort(float2 uv)
            {
                // Adjust UVs to sample the right half of the texture
                uv.x = 0.5 + (uv.x * 0.5); // Right half of the texture

                // Center the UVs
                float2 center = float2(0.5 + _CenterX * 0.5, _CenterY);
                float2 centered = uv - center;

                // Apply distortion
                float r2 = dot(centered, centered);
                float radial = 1.0 + _K1 * r2 + _K2 * r2 * r2;
                float2 tan = float2(
                    2.0 * _P1 * centered.x * centered.y + _P2 * (r2 + 2.0 * centered.x * centered.x),
                    _P1 * (r2 + 2.0 * centered.y * centered.y) + 2.0 * _P2 * centered.x * centered.y
                );
                float2 distorted = centered * radial + tan;

                // Return the distorted UVs
                return distorted + center;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 undistortedUV = undistort(i.uv);
                return tex2D(_MainTex, undistortedUV);
            }
            ENDCG
        }
    }
}
