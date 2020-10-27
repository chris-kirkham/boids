
Shader "Instanced/Boid_InstancedIndirectCompute"
{
	Properties{
		_Colour("Colour", Color) = (1, 1, 1, 1)
		_Emission("Emission", Color) = (0, 0, 0, 1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
			// Physically based Standard lighting model
			#pragma surface surf Standard addshadow
			#pragma multi_compile_instancing
			#pragma instancing_options procedural:setup

			sampler2D _MainTex;

			struct Input {
				float2 uv_MainTex;
			};

			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				StructuredBuffer<float4> positionBuffer;
				//StructuredBuffer<float4> colorBuffer;
			#endif

		void setup()
		{
	#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			/// Positions are calculated in the compute shader.
			/// here we just use them.
			float4 position = positionBuffer[unity_InstanceID];
			float scale = position.w;

			unity_ObjectToWorld._11_21_31_41 = float4(scale, 0, 0, 0);
			unity_ObjectToWorld._12_22_32_42 = float4(0, scale, 0, 0);
			unity_ObjectToWorld._13_23_33_43 = float4(0, 0, scale, 0);
			unity_ObjectToWorld._14_24_34_44 = float4(position.xyz, 1);
			unity_WorldToObject = unity_ObjectToWorld;
			unity_WorldToObject._14_24_34 *= -1;
			unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
	#endif
		}

		float4 _Colour;
		float4 _Emission;
		half _Glossiness;
		half _Metallic;

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			float4 col = 1.0f;

	#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			//col = colorBuffer[unity_InstanceID];
			col = _Colour;
	#else
			col = float4(1, 0, 1, 1);
	#endif
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * col;
			o.Albedo = c.rgb;
			o.Emission = _Emission.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
		}
			FallBack "Diffuse"
}