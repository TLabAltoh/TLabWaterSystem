Shader "Unlit/WaveEquation"
{
	Properties
	{
		_InputTex("Input", 2D) = "black" {}
		_PrevTex("Prev", 2D) = "black" {}
		_Prev2Tex("Prev2", 2D) = "black" {}
		_RoundAdjuster("Adjuster", Float) = 0
		_Stride("Stride", Float) = 1
		_Attenuation("Attenuation", Float) = 0.992
		_C("C", Float) = 0.1
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

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _InputTex;
			sampler2D _PrevTex;
			float4 _PrevTex_TexelSize;
			sampler2D _Prev2Tex;
			float _Stride;
			float _RoundAdjuster;
			float _Attenuation;
			float _C;

			fixed4 frag(v2f i) : SV_Target
			{
				float2 stride = float2(_Stride, _Stride) * _PrevTex_TexelSize.xy;

				//half4 prev = (tex2D(_PrevTex, i.uv) * 2) - 1;

				//half value = prev.r * 2 - (tex2D(_Prev2Tex, i.uv).r * 2 - 1) +
				//			 ((tex2D(_PrevTex, half2(i.uv.x + stride.x, i.uv.y)).r * 2 - 1) +
				//			  (tex2D(_PrevTex, half2(i.uv.x - stride.x, i.uv.y)).r * 2 - 1) +
				//			  (tex2D(_PrevTex, half2(i.uv.x, i.uv.y + stride.y)).r * 2 - 1) +
				//			  (tex2D(_PrevTex, half2(i.uv.x, i.uv.y - stride.y)).r * 2 - 1) -
				//			  prev.r * 4) * _C;

				half4 prev = tex2D(_PrevTex, i.uv);
				half value = prev.r * 2 - tex2D(_Prev2Tex, i.uv).r +
					 (tex2D(_PrevTex, half2(i.uv.x + stride.x, i.uv.y)).r +
					  tex2D(_PrevTex, half2(i.uv.x - stride.x, i.uv.y)).r +
					  tex2D(_PrevTex, half2(i.uv.x, i.uv.y + stride.y)).r +
					  tex2D(_PrevTex, half2(i.uv.x, i.uv.y - stride.y)).r -
					  prev.r * 4) * _C;

				/* AttenuationÇ≈îgÇ…å∏êäÇâ¡Ç¶ÇÈ */
				value *= _Attenuation;

				float4 input = tex2D(_InputTex, i.uv);
				value += input.r;
				value += _RoundAdjuster;
				return fixed4(value, 0, 0, 1);
			}
			ENDCG
		}
	}
}
