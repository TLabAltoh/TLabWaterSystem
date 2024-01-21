Shader "Unlit/WaveEquation"
{
	Properties
	{
		_InputTex("Input", 2D) = "black" {}
		_PrevTex("Prev", 2D) = "black" {}
		_Prev2Tex("Prev2", 2D) = "black" {}
		_RoundAdjuster("Adjuster", Float) = 0.001
		_Stride("Stride", Float) = 1
		_Attenuation("Attenuation", Float) = 0.995
		_C("C", Float) = 0.1	// ”g‚Ì‘¬‚³
	}
	SubShader
	{
		ZTest Always
		Cull Off
		ZWrite Off
		ZTest Always

		LOD 100

		Tags { "RenderType" = "Opaque" }

		Pass
		{
			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_nicest
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

			v2f vert(appdata_img v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = MultiplyUV(UNITY_MATRIX_TEXTURE0, v.texcoord.xy);
				return o;
			}

			sampler2D _InputTex;
			float4 _InputTex_TexelSize;
			sampler2D _PrevTex;
			float4 _PrevTex_TexelSize;
			sampler2D _Prev2Tex;
			float4 _PrevTex2_TexelSize;
			float _Stride;
			float _RoundAdjuster;
			float _Attenuation;
			float _C;

			float4 frag(v2f i) : SV_Target
			{
				float2 stride = float2(_Stride, _Stride) * _PrevTex_TexelSize.xy;

				half4 prev = (tex2D(_PrevTex, i.uv) * 2) - 1;
				half4 prev_l = (tex2D(_PrevTex, float2(i.uv.x - stride.x, i.uv.y)) * 2) - 1;
				half4 prev_r = (tex2D(_PrevTex, float2(i.uv.x + stride.x, i.uv.y)) * 2) - 1;
				half4 prev_t = (tex2D(_PrevTex, float2(i.uv.x, i.uv.y - stride.y)) * 2) - 1;
				half4 prev_b = (tex2D(_PrevTex, float2(i.uv.x, i.uv.y + stride.y)) * 2) - 1;
				half4 prevprev = (tex2D(_Prev2Tex, i.uv) * 2) - 1;

				half vr = (prev.r * 2 - prevprev.r + (prev.g + prev_l.g + prev.b + prev_t.b - prev.r * 4) * _C);
				half vg = (prev.g * 2 - prevprev.g + (prev_r.r + prev.r + prev.a + prev_t.a - prev.g * 4) * _C);
				half vb = (prev.b * 2 - prevprev.b + (prev.a + prev_l.a + prev_b.r + prev.r - prev.b * 4) * _C);
				half va = (prev.a * 2 - prevprev.a + (prev_r.b + prev.b + prev_b.g + prev.g - prev.a * 4) * _C);

				/**
				* value = (value + 1) * 0.5;‚ðŽÀs‚·‚é‘O‚É_Attenuation‚ðæŽZ‚µ‚½‚Æ‚«‚Ì‹““®‚É‚Â‚¢‚Ä ...
				* _Attenuation‚Å 0.5‚ÉŒ¸Š‚µ‚æ‚¤‚Æ‚·‚é‚Æ”g‚ÌƒmƒCƒY‚ª‚Ð‚Ç‚­‚È‚éD
				* _Attenuation‚ðŽg‚í‚È‚¢ê‡”g‚Í0‚É”­ŽU‚·‚é‚ªC‚±‚Ìê‡ƒmƒCƒY‚Í‰ü‘P‚³‚ê‚éD
				*/

				//half vd = 1e-4 * _Time.w;
				//vr -= vd * (prev.r - prevprev.r) * (vr > 0 ? 1 : -1);
				//vg -= vd * (prev.g - prevprev.g) * (vg > 0 ? 1 : -1);
				//vb -= vd * (prev.b - prevprev.b) * (vb > 0 ? 1 : -1);
				//va -= vd * (prev.a - prevprev.a) * (va > 0 ? 1 : -1);

				float4 value = float4(vr, vg, vb, va);

				value = (value + 1) * 0.5;
				value += tex2D(_InputTex, i.uv);
				value *= _Attenuation;
				value += _RoundAdjuster;

				return value;
			}
			ENDCG
		}
	}
}
