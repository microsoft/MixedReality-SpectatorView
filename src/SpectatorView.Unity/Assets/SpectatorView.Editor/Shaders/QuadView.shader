// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

Shader "SV/QuadView"
{
    Properties
    {
        _MainTex("Src Video Texture", 2D) = "white" {}
        _HologramTex("Hologram Texture", 2D) = "white" {}
        _HologramAlphaTex("Hologram Alpha Texture", 2D) = "white" {}
        _CompositeTex("Composite Texture", 2D) = "white" {}
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
                o.uv = v.uv / 0.5;
                return o;
            }
            
            sampler2D _MainTex;
            sampler2D _HologramTex;
            sampler2D _HologramAlphaTex;
            sampler2D _CompositeTex;

            fixed4 frag (v2f i) : SV_Target
            {
                half2 uv = lerp(i.uv, i.uv - 1, step(1, i.uv));
                half4 col;
                if (i.uv.x >= 1)
                {
                    if (i.uv.y < 1)
                    {
                        col = tex2D(_CompositeTex, uv);
                    }
                    else
                    {
                        col = tex2D(_HologramAlphaTex, uv);
                    }
                }
                else
                {
                    if (i.uv.y < 1)
                    {
                        col = tex2D(_MainTex, uv);
                    }
                    else
                    {
                        col = tex2D(_HologramTex, uv);
                    }
                }
                return col;
            }
            ENDCG
        }
    }
}
