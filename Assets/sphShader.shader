Shader "Unlit/sphShader"
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

			#define WALL_DAMPING  0.3
			#define TIME_STEP 1.0
			// time step written in as 1, debugging

			v2f vert (uint id : SV_VertexID)
			{
				v2f o;
				Particle p = cloud[id];
				o.vertex = UnityObjectToClipPos(p.pos);

				float3 accel = p.f / p.d;
				float3 v_n = p.vel + 1 * accel;
				float3 p_n = p.pos + 1 * v_n;
				/**
				if (p_n.x < -100) { 
					p_n.x = -100;
					v_n.x *= -1 * WALL_DAMPING;
				}
				if (p_n.x > 100) { 
					p_n.x = 100;
					v_n.x *= -1 * WALL_DAMPING;
				}
				if (p_n.y < -100) { 
					p_n.y = -100;
					v_n.y *= -1 * WALL_DAMPING;
				}
				if (p_n.y > 100) { 
					p_n.y = 100;
					v_n.y *= -1 * WALL_DAMPING;
				}
				if (p_n.z < -100) { 
					p_n.z = -100;
					v_n.z *= -1 * WALL_DAMPING;
				}
				if (p_n.z > 100) { 
					p_n.z = 100;
					v_n.z *= -1 * WALL_DAMPING;
				}
				**/
				cloud[id].vel = v_n;
				cloud[id].pos = p_n;
				o.color = cloud[id].color;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = fixed4(i.color,1); 
				// col.r = 255;
				return col;
			}
			ENDCG
		}

		

	}
}
