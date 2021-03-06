﻿Shader "Unlit/sphShader"
{
	Properties
	{

	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct Particle
			{
				float3 pos;
				float3 vel;
				float3 f;
				float3 color;
				float d;
				float p;
				uint3 extra;
			};

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float3 color : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};


			RWStructuredBuffer<Particle> cloud : register(u1);

			v2f vert (uint id : SV_VertexID)
			{
				v2f o;
				Particle p = cloud[id];
				o.vertex = UnityObjectToClipPos(p.pos);
				o.color = cloud[id].color;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = fixed4(i.color,1); 
				return col;
			}
			ENDCG
		}

		

	}
}
