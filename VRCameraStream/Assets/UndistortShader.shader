Shader "Custom/StereoUndistortShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _K1 ("Radial Distortion K1", Float) = -0.15
        _K2 ("Radial Distortion K2", Float) = 0.05
        _P1 ("Tangential Distortion P1", Float) = 0.0
        _P2 ("Tangential Distortion P2", Float) = 0.0
        _CenterX ("Optical Center X", Float) = 0.5
        _CenterY ("Optical Center Y", Float) = 0.5
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
            float _K1;
            float _K2;
            float _P1;
            float _P2;
            float _CenterX;
            float _CenterY;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            float2 undistort(float2 uv)
            {
                // Convert UV to centered coordinates
                float2 centered = uv - float2(_CenterX, _CenterY);
                
                float r2 = centered.x * centered.x + centered.y * centered.y;
                float r4 = r2 * r2;
                
                // Radial distortion
                float radialDistortion = 1.0 + _K1 * r2 + _K2 * r4;
                
                // Tangential distortion
                float2 tangentialDistortion = float2(
                    2.0 * _P1 * centered.x * centered.y + _P2 * (r2 + 2.0 * centered.x * centered.x),
                    _P1 * (r2 + 2.0 * centered.y * centered.y) + 2.0 * _P2 * centered.x * centered.y
                );
                
                // Apply distortions
                float2 distorted = centered * radialDistortion + tangentialDistortion;
                
                // Convert back to UV space
                return distorted + float2(_CenterX, _CenterY);
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