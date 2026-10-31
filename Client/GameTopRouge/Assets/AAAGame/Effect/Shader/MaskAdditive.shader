Shader "Custom/Mask/Additive"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white"{}
		[HDR]_TintColor("Color", Color) = (1, 1, 1, 1)
		_CustomMask("Mask (RGB)", 2D) = "white"{}
		_Strength("Strength", Range(1, 10)) = 1
		_SpeedU("水平速度", float) = 0
		_SpeedV("垂直速度", float) = 0
		_MaskSpeedU("Mask水平速度", float) = 0
		_MaskSpeedV("Mask垂直速度", float) = 0
		_Alpha("Alpha", Range(0, 1)) = 1
	}
	
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        Cull Off
        Blend SrcAlpha One

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

			TEXTURE2D(_MainTex);			SAMPLER(sampler_MainTex);
			TEXTURE2D(_CustomMask);			SAMPLER(sampler_CustomMask);
CBUFFER_START(UnityPerMaterial)
			uniform float4 _MainTex_ST;
			half4 _TintColor;
			uniform float4 _CustomMask_ST;
			float _Strength;
			float _SpeedU;
			float _SpeedV;
			float _MaskSpeedU;
			float _MaskSpeedV;
			float _Alpha = 1;
CBUFFER_END

			struct appdata
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				half4 color : COLOR;
			};

			struct v2f
			{
				float2 uv:TEXCOORD0;
				float2 maskUV:TEXCOORD1;
				float4 pos:POSITION;
				half4 color : COLOR;
				float3 vpos : TEXCOORD2;
			};
			
			v2f vert(appdata IN)
			{
				v2f output;
				output.uv =  TRANSFORM_TEX(IN.texcoord, _MainTex);
				output.vpos = IN.vertex.xyz;
				if (_SpeedU != 0)
				{
					output.uv.x += _Time.x * _SpeedU;
				}
				if (_SpeedV != 0)
				{
					output.uv.y += _Time.x * _SpeedV;
				}

				output.maskUV =  TRANSFORM_TEX(IN.texcoord, _CustomMask);
				if (_MaskSpeedU != 0)
				{
					output.maskUV.x += _Time.x * _MaskSpeedU;
				}
				if (_MaskSpeedV != 0)
				{
					output.maskUV.y += _Time.x * _MaskSpeedV;
				}
				output.pos = TransformObjectToHClip(IN.vertex.xyz);
				output.color = IN.color;
				return output;
			}
			
			half4 frag(v2f IN):COLOR
			{
				half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
				col *= _TintColor;
				col *= _Strength;
				col *= SAMPLE_TEXTURE2D(_CustomMask, sampler_CustomMask, IN.maskUV);
				col.a *= _Alpha;

				return col;
			}
			
			ENDHLSL
		}
	}
}