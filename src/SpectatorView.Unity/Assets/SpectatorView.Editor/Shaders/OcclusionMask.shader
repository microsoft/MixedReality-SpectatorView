Shader "SV/OcclusionMask"
{
    Properties
    {
        _BodyMaskTexture("BodyMaskTexture", 2D) = "white" {}
        _DepthTexture("DepthTexture", 2D) = "white" {}
		_MinHologramDepth("Starting Hologram Depth", Float) = 0
        _MaxOcclusionDepth("Maximum Occlusion Depth", Float) = 10.0
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
            float _MinHologramDepth;
            float _MaxOcclusionDepth;

            fixed4 frag (v2f i) : SV_Target
            {
                half4 maskVal = fixed4(0,0,0,0);

                float rawHologramDepth = SAMPLE_DEPTH_TEXTURE(_LastCameraDepthTexture, i.uv);
                float hologramDepth = LinearEyeDepth(rawHologramDepth);
                float kinectDepth = _DepthTexture.Sample(sampler_point_clamp, float2(i.uv[0], 1-i.uv[1])).r * 65535 * 0.001; // Incoming texture is R16

				bool useKinectDepth = (kinectDepth > 0.0f) && (kinectDepth < _MaxOcclusionDepth);
				bool useHologramDepth = (hologramDepth > _MinHologramDepth) && (rawHologramDepth < 1.0f);

				float isHologramOccluded = 0.0f;
				if (!useHologramDepth || 
					(useHologramDepth && useKinectDepth && (kinectDepth < hologramDepth)))
				{
					isHologramOccluded = 1.0f;
				}

                float bodyMask = _BodyMaskTexture.Sample(sampler_point_clamp, float2(i.uv[0], i.uv[1])).r * 65535;

                maskVal.r = max(1 - isHologramOccluded, 1 - bodyMask);
                
                return maskVal;
            }
            ENDCG
        }
    }
}
