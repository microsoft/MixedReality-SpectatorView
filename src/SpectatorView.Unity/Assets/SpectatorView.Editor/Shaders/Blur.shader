Shader "SV/Blur"
{
    Properties
    {
        _MaskTexture("MaskTexture", 2D) = "white" {}
        _BlurIntensity("BlurIntensity", float) = 0.00375
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

            Texture2D _MaskTexture;
            float _BlurIntensity;
            SamplerState sampler_point_clamp;

            fixed4 frag(v2f i) : SV_Target
            {
                half4 blurVal = fixed4(0,0,0,0);
                float sumX = 0.0;

                // Sample +/- 5 pixels from origin in X direction with assigned weight to generate average
                sumX += _MaskTexture.Sample(sampler_point_clamp, float2(i.uv[0] - 5.0 * _BlurIntensity, i.uv[1])).r * 0.025;
                sumX += _MaskTexture.Sample(sampler_point_clamp, float2(i.uv[0] - 4.0 * _BlurIntensity, i.uv[1])).r * 0.05;
                sumX += _MaskTexture.Sample(sampler_point_clamp, float2(i.uv[0] - 3.0 * _BlurIntensity, i.uv[1])).r * 0.085;
                sumX += _MaskTexture.Sample(sampler_point_clamp, float2(i.uv[0] - 2.0 * _BlurIntensity, i.uv[1])).r * 0.09;
                sumX += _MaskTexture.Sample(sampler_point_clamp, float2(i.uv[0] - _BlurIntensity, i.uv[1])).r * 0.15;
                sumX += _MaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1])).r * 0.2;
                sumX += _MaskTexture.Sample(sampler_point_clamp, float2(i.uv[0] + _BlurIntensity, i.uv[1])).r * 0.15;
                sumX += _MaskTexture.Sample(sampler_point_clamp, float2(i.uv[0] + 2.0 * _BlurIntensity, i.uv[1])).r * 0.09;
                sumX += _MaskTexture.Sample(sampler_point_clamp, float2(i.uv[0] + 3.0 * _BlurIntensity, i.uv[1])).r * 0.085;
                sumX += _MaskTexture.Sample(sampler_point_clamp, float2(i.uv[0] + 4.0 * _BlurIntensity, i.uv[1])).r * 0.05;
                sumX += _MaskTexture.Sample(sampler_point_clamp, float2(i.uv[0] + 5.0 * _BlurIntensity, i.uv[1])).r * 0.025;

                float sumY = 0.0;
                
                // Sample +/- 5 pixels from origin in Y direction with assigned weight to generate average
                sumY += _MaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1] - 5.0 * _BlurIntensity)).r * 0.025;
                sumY += _MaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1] - 4.0 * _BlurIntensity)).r * 0.05;
                sumY += _MaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1] - 3.0 * _BlurIntensity)).r * 0.085;
                sumY += _MaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1] - 2.0 * _BlurIntensity)).r * 0.09;
                sumY += _MaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1] - _BlurIntensity)).r * 0.15;
                sumY += _MaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1])).r * 0.2;
                sumY += _MaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1] + _BlurIntensity)).r * 0.15;
                sumY += _MaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1] + 2.0 * _BlurIntensity)).r * 0.09;
                sumY += _MaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1] + 3.0 * _BlurIntensity)).r * 0.085;
                sumY += _MaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1] + 4.0 * _BlurIntensity)).r * 0.05;
                sumY += _MaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1] + 5.0 * _BlurIntensity)).r * 0.025;
                
                blurVal.r = (sumX + sumY) / 2.0;

                return blurVal;
            }
            ENDCG
        }
    }
}
