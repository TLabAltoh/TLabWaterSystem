Shader "Unlit/WaveNormal"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_RefTex("Ref",2D) = "black" {}
		_BumpMap("Normalmap", 2D) = "bump" {}
		_BumpAmt("BumpAmt", Range(0,9999)) = 0
		_WaveTex("Wave",2D) = "gray" {}
		_ParallaxScale("Parallax Scale", Float) = 1
		_NormalScaleFactor("Normal Scale Factor", Float) = 1
	}
	SubShader
	{
		Tags {"Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent"}
		ZWrite On
		Cull Back
		Blend SrcAlpha OneMinusSrcAlpha

		CGINCLUDE
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
			float4 ref : TEXCOORD1;
		};

		sampler2D _MainTex;
		sampler2D _RefTex;
		sampler2D _BumpMap;
		sampler2D _WaveTex;
		float4 _MainTex_ST;
		float4 _RefTex_TexelSize;
		float4 _WaveTex_TexelSize;
		float4x4 _RefW;
		float4x4 _RefVP;
		float _BumpAmt;
		float _ParallaxScale;
		float _NormalScaleFactor;

		float ease_in(float k)
		{
			// http://marupeke296.com/TIPS_No19_interpolation.html
			return 1 / (1 + exp(-8 * (k - 1))) * 2;
		}

		v2f vert(appdata v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.ref = mul(_RefVP, mul(_RefW, v.vertex));
			o.uv = TRANSFORM_TEX(v.uv, _MainTex);
			return o;
		}

		fixed4 frag(v2f i) : SV_Target
		{
			/* 波動方程式の解を_WaveTexで受け取り、波による歪み具合をbumpに加算 */
			/* _WaveTexは波の高さなので、高さの変化量から法線を求める			*/
			float2 shiftX = { _WaveTex_TexelSize.x,  0 };
			float2 shiftZ = { 0, _WaveTex_TexelSize.y };
			shiftX *= _ParallaxScale * _NormalScaleFactor;
			shiftZ *= _ParallaxScale * _NormalScaleFactor;

			/* 各軸で両方向での波の高さの変化量を取得 */
			float texX = tex2Dlod(_WaveTex, float4(i.uv.xy + shiftX, 0, 0)).r;
			float texx = tex2Dlod(_WaveTex, float4(i.uv.xy - shiftX, 0, 0)).r;
			float texZ = tex2Dlod(_WaveTex, float4(i.uv.xy + shiftZ, 0, 0)).r;
			float texz = tex2Dlod(_WaveTex, float4(i.uv.xy - shiftZ, 0, 0)).r;

			float texC = tex2Dlod(_WaveTex, float4(i.uv.xy, 0, 0)).r;

			/* 各軸の変化量の外積(その面における法線の向き)を求める */
			//float3 du = { 1, 0, _NormalScaleFactor * (texX.x - texx.x) };
			//float3 dv = { 0, 1, _NormalScaleFactor * (texZ.x - texz.x) };
			//float2 bump = (normalize(cross(du, dv)) + 1) * 0.5;			// 0〜1の範囲に戻す

			float3 normal = normalize(float3(texX - texx, texZ - texz, 0.1));

			return float4(_NormalScaleFactor * ease_in(texC) * normal, 1);
		}

		ENDCG

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}
}
