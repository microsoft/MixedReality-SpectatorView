Shader "SV/Blur"
{
    Properties
    {
        _MaskTexture("MaskTexture", 2D) = "white" {}
        _BlurSize("BlurSize", Range(0, 10)) = 5
    }

    Category
     {
            // We must be transparent, so other objects are drawn before this one.
            Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Opaque" }


        SubShader
        {
            // No culling or depth
            Cull Off ZWrite Off ZTest Always
            // Horizontal blur
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

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    return o;
                }

                float4 _MaskTexture_TexelSize;
                Texture2D _MaskTexture;
                SamplerState sampler_point_clamp;
                float _BlurSize;

                fixed4 frag(v2f i) : SV_Target
                {
                    half4 sum = half4(0,0,0,0);
                    float sumX = 0.0;

                    #define MASKSAMPLE(weight, kernelx) _MaskTexture.Sample(sampler_point_clamp, float2(i.uv[0] - kernelx * _BlurSize * _MaskTexture_TexelSize.x, i.uv[1])).r * weight;

                    sumX += MASKSAMPLE(0.025, -5.0);
                    sumX += MASKSAMPLE(0.05, -4.0);
                    sumX += MASKSAMPLE(0.085, -3.0);
                    sumX += MASKSAMPLE(0.09, -2.0);
                    sumX += MASKSAMPLE(0.15, -1.0);
                    sumX += MASKSAMPLE(0.2, 0.0);
                    sumX += MASKSAMPLE(0.15, 1.0);
                    sumX += MASKSAMPLE(0.09, 2.0);
                    sumX += MASKSAMPLE(0.085, 3.0);
                    sumX += MASKSAMPLE(0.05, 4.0);
                    sumX += MASKSAMPLE(0.025, 5.0);

                    sum.r = sumX;

                    return sum;
                }
                ENDCG
            }
            
            GrabPass
            {
            }

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #include "UnityCG.cginc"

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 texcoord: TEXCOORD0;
                };

                struct v2f
                {
                    float4 vertex : POSITION;
                    float4 uvgrab : TEXCOORD0;
                    float2 uvmain : TEXCOORD1;
                };

                float4 _MaskSampler_ST;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uvgrab = ComputeGrabScreenPos(o.vertex);
                    o.uvmain = TRANSFORM_TEX(v.texcoord, _MaskSampler);
                    return o;
                }

                float4 _GrabTexture_TexelSize;
                sampler2D _GrabTexture;
                SamplerState sampler_point_clamp;
                float _BlurSize;

                fixed4 frag(v2f i) : SV_Target
                {
                    half4 sum = half4(0,0,0,0);
                    float sumY = 0.0;

                    #define GRABPIXEL(weight,kernely) tex2Dproj(_GrabTexture, i.uvgrab + half4(0, kernely * _BlurSize * _GrabTexture_TexelSize.y , 0, 0)).r * weight;

                    sumY += GRABPIXEL(0.025, -5.0);
                    sumY += GRABPIXEL(0.05, -4.0);
                    sumY += GRABPIXEL(0.085, -3.0);
                    sumY += GRABPIXEL(0.09, -2.0);
                    sumY += GRABPIXEL(0.15, -1.0);
                    sumY += GRABPIXEL(0.2, 0.0);
                    sumY += GRABPIXEL(0.15, 1.0);
                    sumY += GRABPIXEL(0.09, 2.0);
                    sumY += GRABPIXEL(0.085, 3.0);
                    sumY += GRABPIXEL(0.05, 4.0);
                    sumY += GRABPIXEL(0.025, 5.0);

                    sum.r = sumY;
                    return sum;
                }
                ENDCG
            }
            
        }
     }
}
