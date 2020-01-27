// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

Shader "SV/HoloAlpha"
{
    Properties
    {
        _BackTex("BackTex", 2D) = "white" {}
        _FronTex("FrontTex", 2D) = "white" {}
        _OcclusionTexture("OcclusionTexture", 2D) = "white" {}
        _Alpha("Alpha", float) = 0.9
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

            sampler2D _BackTex;
            sampler2D _FrontTex;
            float _Alpha;
            sampler2D_float _LastCameraDepthTexture;
            Texture2D _OcclusionTexture;
            SamplerState sampler_point_clamp;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 backCol = tex2D(_BackTex, i.uv);
                fixed4 frontCol = tex2D(_FrontTex, i.uv);

                float rawHologramDepth = SAMPLE_DEPTH_TEXTURE(_LastCameraDepthTexture, i.uv);
                float hologramDepth = LinearEyeDepth(rawHologramDepth);
                float kinectDepth = _OcclusionTexture.Sample(sampler_point_clamp, float2(i.uv[0], 1-i.uv[1])).r * 65535 * 0.001; // Incoming texture is R16

                float isHologramOccluded = kinectDepth > 0.0f && rawHologramDepth < 1.0f && kinectDepth < hologramDepth;
                float alpha = min(1.0f - isHologramOccluded, _Alpha);

                fixed4 composite = backCol * (1 - alpha) + frontCol * alpha;
                composite.a = 1.0f;
                return composite;
            }
            ENDCG
        }
    }
}
