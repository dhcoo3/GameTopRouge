Shader "Custom/Mask/AlphaBlend"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white"{}
		_TintColor("Color", Color) = (1, 1, 1, 1)
		_CustomMask("Mask (RGB)", 2D) = "white"{}
		_BlendMaskColor("混合Mask贴图", Range(0, 1)) = 0
		_Strength("Strength", Range(1, 10)) = 1
		_SpeedU("水平速度", float) = 0
		_SpeedV("垂直速度", float) = 0
		_MaskSpeedU("Mask水平速度", float) = 0
		_MaskSpeedV("Mask垂直速度", float) = 0
		_Alpha("Alpha", Range(0, 1)) = 1
	}
	
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline" = "UniversalPipeline" }
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
        Cull Off
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
			uniform float4 _CustomMask_ST;
			half _Strength;
			half4 _TintColor;
			half _SpeedU;
			half _SpeedV;
			half _MaskSpeedU;
			half _MaskSpeedV;
			half _BlendMaskColor;
			half _Alpha = 1;
CBUFFER_END
			
			struct appdata
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				half4 color : COLOR;
			};

			struct v2f
			{
				half2 uv:TEXCOORD0;
				half2 maskUV:TEXCOORD1;
				half4 pos:POSITION;
				half4 color : COLOR;
			};
			
			v2f vert(appdata IN)
			{
				v2f output;
				output.uv = TRANSFORM_TEX(IN.texcoord, _MainTex);
				if (_SpeedU != 0)
				{
					output.uv.x += frac(_Time.x * _SpeedU);
				}
				if (_SpeedV != 0)
				{
					output.uv.y += frac(_Time.x * _SpeedV);
				}
				output.maskUV =  TRANSFORM_TEX(IN.texcoord, _CustomMask);
				if (_MaskSpeedU != 0)
				{
					output.maskUV.x += frac(_Time.x * _MaskSpeedU);
				}
				if (_MaskSpeedV != 0)
				{
					output.maskUV.y += frac(_Time.x * _MaskSpeedV);
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
				half4 maskCol = SAMPLE_TEXTURE2D(_CustomMask, sampler_CustomMask, IN.maskUV);
				if (_BlendMaskColor > 0)
				{
					col.rgb *= maskCol.rgb * _BlendMaskColor;
				}
				col.a *= maskCol.r;
				col.a = saturate(col.a);
				col.a *= _Alpha;
				return col;
			}
			
			ENDHLSL
		}
	}
}