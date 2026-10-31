Shader "Custom/Particle/Additive"
{
	Properties
	{
		[HDR]_Color("Color", Color) = (1, 1, 1, 1)
		_MainTex("Base (RGB)", 2D) = "white"{}
		_Strength("Strength", Range(1, 10)) = 1
		_Alpha("Alpha", Range(0, 1)) = 1
		[Enum(LEqual,2, Always,6)] _ZTest ("ZTest", Float) = 2
		[Toggle]_ParticleClip ("ParticleClip", Float) = 0
		[HideInInspector]_MinX ("Min X", Float) = -100
		[HideInInspector]_MaxX ("Max X", Float) = 100
		[HideInInspector]_MinY ("Min Y", Float) = -100
		[HideInInspector]_MaxY ("Max Y", Float) = 100
	}
	
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline" = "UniversalPipeline" }
		Blend SrcAlpha One
		ZWrite Off
		ZTest [_ZTest]
		Cull Off
		
		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

			TEXTURE2D(_MainTex);			SAMPLER(sampler_MainTex);
CBUFFER_START(UnityPerMaterial)
			uniform float4 _MainTex_ST;
			half _Alpha = 1; 
			float _ParticleClip;
			float _MinX;
			float _MaxX;
            float _MinY;
            float _MaxY;
			half _Strength;
			half4 _Color;
CBUFFER_END

			struct appdata_t 
			{
				float4 vertex : POSITION;
				half4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};
			
			struct v2f
			{
				half4 uv:TEXCOORD0;
				half4 pos:POSITION;
				half4 color : COLOR;
			};
			
			v2f vert(appdata_t IN)
			{
				v2f output;
				output.uv.xy =  TRANSFORM_TEX(IN.texcoord, _MainTex);
				output.pos = TransformObjectToHClip(IN.vertex.xyz);
				output.color = IN.color;
				if (_ParticleClip)
				{
					output.uv.zw = mul(unity_ObjectToWorld, IN.vertex).xy;
				}
				
				return output;
			}
			
			half4 frag(v2f IN):COLOR
			{
				half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv.xy) * IN.color * _Color;
				col *= _Strength;
				col.a *= _Alpha;
				if(_ParticleClip)
				{
					col.a *= (IN.uv.z >= _MinX );
					col.a *= (IN.uv.z <= _MaxX);
					col.a *= (IN.uv.w >= _MinY);
					col.a *= (IN.uv.w <= _MaxY); 
				}
				return col;
			}
			
			ENDHLSL
		}
	}
}