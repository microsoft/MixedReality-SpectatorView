Shader "SV/AlphaBlend"
{
	Properties
	{
        _BackTex("BackTex", 2D) = "white" {}
        _MainTex("FrontTex", 2D) = "white" {}
	}
	SubShader
	{
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
			
			sampler2D _MainTex;
            sampler2D _BackTex;

			fixed4 frag (v2f i) : SV_Target
			{
				half4 front = tex2D(_MainTex, i.uv);
                half4 back = tex2D(_BackTex, i.uv);
				half4 col = lerp(back, front, front.a);
                col.a = 1;
				return col;
			}
			ENDCG
		}
	}
}
