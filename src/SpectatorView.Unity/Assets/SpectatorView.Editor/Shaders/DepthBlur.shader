Shader "SV/DepthBlur"
{
    Properties
    {
        _BodyMaskTexture("BodyMaskTexture", 2D) = "white" {}
        _DepthTexture("DepthTexture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            Texture2D _BodyMaskTexture;
            Texture2D _DepthTexture;
            sampler2D_float _LastCameraDepthTexture;
            SamplerState sampler_point_clamp;

            fixed4 frag(v2f i) : SV_Target
            {
                float rawHologramDepth = SAMPLE_DEPTH_TEXTURE(_LastCameraDepthTexture, i.uv);
                float hologramDepth = LinearEyeDepth(rawHologramDepth);
                float kinectDepth = _DepthTexture.Sample(sampler_point_clamp, float2(i.uv[0], 1-i.uv[1])).r * 65535 * 0.001; // Incoming texture is R16

                float isHologramOccluded = kinectDepth > 0.0f && rawHologramDepth < 1.0f && kinectDepth < hologramDepth;

                half4 maskVal = fixed4(0,0,0,0);

                float blurAmount = 0.00900f;
                float sum = 0.0;

                sum += _BodyMaskTexture.Sample(sampler_point_clamp, float2(i.uv[0] - 5.0 * blurAmount, i.uv[1])).r * 0.025 * 65535;
                sum += _BodyMaskTexture.Sample(sampler_point_clamp, float2(i.uv[0] - 4.0 * blurAmount, i.uv[1])).r * 0.05 * 65535;
                sum += _BodyMaskTexture.Sample(sampler_point_clamp, float2(i.uv[0] - 3.0 * blurAmount, i.uv[1])).r * 0.09 * 65535;
                sum += _BodyMaskTexture.Sample(sampler_point_clamp, float2(i.uv[0] - 2.0 * blurAmount, i.uv[1])).r * 0.12 * 65535;
                sum += _BodyMaskTexture.Sample(sampler_point_clamp, float2(i.uv[0] - blurAmount, i.uv[1])).r * 0.15 * 65535;
                sum += _BodyMaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1])).r * 0.16 * 65535;
                sum += _BodyMaskTexture.Sample(sampler_point_clamp, float2(i.uv[0] + blurAmount, i.uv[1])).r * 0.15 * 65535;
                sum += _BodyMaskTexture.Sample(sampler_point_clamp, float2(i.uv[0] + 2.0 * blurAmount, i.uv[1])).r * 0.12 * 65535;
                sum += _BodyMaskTexture.Sample(sampler_point_clamp, float2(i.uv[0] + 3.0 * blurAmount, i.uv[1])).r * 0.09 * 65535;
                sum += _BodyMaskTexture.Sample(sampler_point_clamp, float2(i.uv[0] + 4.0 * blurAmount, i.uv[1])).r * 0.05 * 65535;
                sum += _BodyMaskTexture.Sample(sampler_point_clamp, float2(i.uv[0] + 5.0 * blurAmount, i.uv[1])).r * 0.025 * 65535;

                float sumY = 0.0;

                sumY += _BodyMaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1] - 5.0 * blurAmount)).r * 0.025 * 65535;
                sumY += _BodyMaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1] - 4.0 * blurAmount)).r * 0.05 * 65535;
                sumY += _BodyMaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1] - 3.0 * blurAmount)).r * 0.09 * 65535;
                sumY += _BodyMaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1] - 2.0 * blurAmount)).r * 0.12 * 65535;
                sumY += _BodyMaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1] - blurAmount)).r * 0.15 * 65535;
                sumY += _BodyMaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1])).r * 0.16 * 65535;
                sumY += _BodyMaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1] + blurAmount)).r * 0.15 * 65535;
                sumY += _BodyMaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1] + 2.0 * blurAmount)).r * 0.12 * 65535;
                sumY += _BodyMaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1] + 3.0 * blurAmount)).r * 0.09 * 65535;
                sumY += _BodyMaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1] + 4.0 * blurAmount)).r * 0.05 * 65535;
                sumY += _BodyMaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1] + 5.0 * blurAmount)).r * 0.025 * 65535;

                maskVal.r = (sum + sumY) / 2.0;

                return maskVal;
            }
            ENDCG
        }
    }
}
