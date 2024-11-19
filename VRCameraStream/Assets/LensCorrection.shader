Shader "Custom/StereoLensCorrection"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _K1 ("K1", Float) = -0.23657366
        _K2 ("K2", Float) = 0.04110457
        _K3 ("K3", Float) = -0.00263397
        _P1 ("P1", Float) = -0.00055951
        _P2 ("P2", Float) = -0.00124154
        _Fx ("Focal X", Float) = 497.50002164
        _Fy ("Focal Y", Float) = 502.86627903
        _Cx ("Center X", Float) = 687.2781095
        _Cy ("Center Y", Float) = 388.52699115
        _RectMapL ("Left Rectification Map", 2D) = "black" {}
        _RectMapR ("Right Rectification Map", 2D) = "black" {}
        [Toggle] _UseRectificationMaps ("Use Rectification Maps", Float) = 0
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
            float _K1, _K2, _K3, _P1, _P2;
            float _Fx, _Fy, _Cx, _Cy;
            float _UseRectificationMaps;
            sampler2D _RectMapL;
            sampler2D _RectMapR;

            static const float EYE_WIDTH = 1280.0;
            static const float EYE_HEIGHT = 720.0;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            float2 applyBasicDistortion(float2 p)
            {
                float r2 = p.x * p.x + p.y * p.y;
                float r4 = r2 * r2;
                float r6 = r4 * r2;
                
                float radial = 1.0 + _K1 * r2 + _K2 * r4 + _K3 * r6;
                
                float2 tangential = float2(
                    2.0 * _P1 * p.x * p.y + _P2 * (r2 + 2.0 * p.x * p.x),
                    _P1 * (r2 + 2.0 * p.y * p.y) + 2.0 * _P2 * p.x * p.y
                );
                
                return float2(
                    p.x * radial + tangential.x,
                    p.y * radial + tangential.y
                );
            }

            float2 applyRectificationMap(float2 uv, bool isRightEye)
            {
                // The UV coordinates here are already in per-eye space (0-1)
                float2 rectified;
                if (isRightEye)
                {
                    rectified = tex2D(_RectMapR, uv).xy;
                }
                else
                {
                    rectified = tex2D(_RectMapL, uv).xy;
                }
                return rectified;
            }
            
            float2 undistort(float2 uv)
            {
                bool isRightEye = uv.x >= 0.5;
                float localUV_x = isRightEye ? (uv.x - 0.5) * 2.0 : uv.x * 2.0;
                float2 localUV = float2(localUV_x, uv.y);
                
                float2 pixel_coord = float2(
                    localUV.x * EYE_WIDTH,
                    localUV.y * EYE_HEIGHT
                );
                
                float2 p = float2(
                    (pixel_coord.x - _Cx) / _Fx,
                    (pixel_coord.y - _Cy) / _Fy
                );
                
                float2 corrected;
                if (_UseRectificationMaps > 0.5)
                {
                    corrected = applyRectificationMap(localUV, isRightEye);
                }
                else
                {
                    corrected = applyBasicDistortion(p);
                    corrected = float2(
                        corrected.x * _Fx + _Cx,
                        corrected.y * _Fy + _Cy
                    );
                    corrected = corrected / float2(EYE_WIDTH, EYE_HEIGHT);
                }
                
                float2 finalUV = corrected;
                finalUV.x = isRightEye ? (finalUV.x * 0.5 + 0.5) : (finalUV.x * 0.5);
                
                return finalUV;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 undistortedUV = undistort(i.uv);
                
                if (undistortedUV.x < 0 || undistortedUV.x > 1 || 
                    undistortedUV.y < 0 || undistortedUV.y > 1)
                {
                    return fixed4(0, 0, 0, 1);
                }
                
                return tex2D(_MainTex, undistortedUV);
            }
            ENDCG
        }
    }
}