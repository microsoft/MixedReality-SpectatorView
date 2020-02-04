Shader "SV/Blur"
{
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _MainTex("Tint Color (RGB)", 2D) = "white" {}
        _Size("Size", Range(0, 20)) = 1
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
            GrabPass
            {
                    Tags { "LightMode" = "Always" }
            }
            Pass
            {
                Tags { "LightMode" = "Always" }

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

                Texture2D _MaskTexture;
                float _BlurIntensity;
                SamplerState sampler_point_clamp;
                sampler2D _GrabTexture;
                float4 _GrabTexture_TexelSize;
                float _Size;

                fixed4 frag(v2f i) : SV_Target
                {
                    half4 sum = half4(0,0,0,0);

                        #define GRABPIXEL(weight,kernelx) tex2Dproj( _GrabTexture, i.uvgrab + half4(kernelx*_Size*_GrabTexture_TexelSize.x,0,0,0)) * weight

                        sum += GRABPIXEL(0.03, -4.0);
                        sum += GRABPIXEL(0.08, -3.0);
                        sum += GRABPIXEL(0.14, -2.0);
                        sum += GRABPIXEL(0.17, -1.0);
                        sum += GRABPIXEL(0.18, 0.0);
                        sum += GRABPIXEL(0.17, 1.0);
                        sum += GRABPIXEL(0.14, 2.0);
                        sum += GRABPIXEL(0.08, 3.0);
                        sum += GRABPIXEL(0.03, 4.0);

                        return sum;
                }
                ENDCG
            }
        }
        }
}
