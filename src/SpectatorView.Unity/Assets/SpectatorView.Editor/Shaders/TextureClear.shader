Shader "SV/TextureClear"
{
	Properties
	{
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always
        Pass
        {
            Color(0, 0, 0, 0)
        }
	}
}
