Shader "SV/ColorCorrection"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
			float _RScale;
			float _GScale;
			float _BScale;
			float _HOffset;
			float _SOffset;
			float _VOffset;
			float _Brightness;
			float _Contrast;
			float _Gamma;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

			float3 RGBToHSV(float3 rgb)
			{
				float epsilon = 1e-10;
				float4 P = (rgb.g < rgb.b) ? float4(rgb.bg, -1.0, 2.0 / 3.0) : float4(rgb.gb, 0.0, -1.0 / 3.0);
				float4 Q = (rgb.r < P.x) ? float4(P.xyw, rgb.r) : float4(rgb.r, P.yzx);
				float C = Q.x - min(Q.w, Q.y);
				float H = abs((Q.w - Q.y) / (6 * C + epsilon) + Q.z);
				return float3(H, C / (Q.x + epsilon), Q.x);
			}

			float3 HSVToRGB(float3 hsv)
			{
				float R = abs(hsv.x * 6 - 3) - 1;
				float G = 2 - abs(hsv.x * 6 - 2);
				float B = 2 - abs(hsv.x * 6 - 4);
				return ((saturate(float3(R, G, B)) - 1) * hsv.y + 1) * hsv.z;
			}

			float4 ColorCorrect(float4 input, float3 rgbScale, float3 hsvOffs, float brightness, float contrast, float gamma)
			{
				input.rgb *= rgbScale;

				float3 hsv = RGBToHSV(input.rgb);
				if (hsvOffs.x < 0.0f) hsvOffs.x += 2.0f;
				hsv += hsvOffs;
				hsv.x = fmod(hsv.x, 1.0f);
				hsv = saturate(hsv);	// Prevent illegal colors

				input.rgb = HSVToRGB(hsv);
				input.rgb = contrast * (pow(input.rgb, 1.0f / gamma) - 0.5f) + 0.5f + brightness;

				return input;
			}

            float4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
                return ColorCorrect(col, float3(_RScale, _GScale, _BScale), float3(_HOffset, _SOffset, _VOffset), _Brightness, _Contrast, _Gamma);
            }
            ENDCG
        }
    }
}
