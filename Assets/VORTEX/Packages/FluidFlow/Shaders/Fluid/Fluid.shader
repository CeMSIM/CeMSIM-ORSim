Shader "Fluid Simulation/Fluid" {
	Properties {
		[Header (Main Settings)]
		_Color ("Color", Color) = (1, 1, 1, 1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_NormalTex ("Normal Map", 2D) = "bump" {}
		_NormalStrength ("Normal Strength", Range (0.0, 10.0)) = 1
		_Smoothness ("Smoothness", Range (0, 1)) = 0.5
		_Metallic ("Metallic", Range (0, 1)) = 0.0

		[HideInInspector]
		_FluidTex ("Fluid (RGBA)", 2D) = "black" {}
		[Header (Blood Settings)]
		_FluidColor ("Fluid Color", Color) = (1,0,0,1)

		_FluidSmoothness ("Fluid Smoothness", Range (0, 1)) = 0.2
		_FluidMetallic ("Fluid Metallic", Range (0,1)) = 0.5
		_FluidNormalStrength ("Fluid Normal Strength", Range (0.0, 10.0)) = 1
		_dryAmount ("Dry Amount", Range (0, 1)) = .7

		_noiseTex ("Noise", 2D) = "grey" {}
		_noiseScale ("Noise Scale", Float) = 10
		_noisePower ("Noise Power", Range (0, .1)) = .005
	}

	SubShader {
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows
		#pragma multi_compile _Fluid_UV1 _Fluid_UV2
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _NormalTex;
		sampler2D _FluidTex;
		sampler2D _noiseTex;

		struct Input {
			float2 uv_MainTex;
			float2 uv_NormalTex;
			#if defined(_Fluid_UV1)
			float2 uv_FluidTex;
			#elif defined(_Fluid_UV2)
			float2 uv2_FluidTex;
			#endif
		};

		half _FluidSmoothness;
		half _FluidMetallic;
		half _FluidNormalStrength;
		half _NormalStrength;
		half _Smoothness;
		half _Metallic;
		half _dryAmount;

		half _noiseScale;
		half _noisePower;


		fixed4 _Color;
		fixed4 _FluidColor;


		void surf (Input IN, inout SurfaceOutputStandard o) {
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;

			// noise to distort fluid
			float2 uvNoise = (tex2D (_noiseTex, IN.uv_MainTex * _noiseScale).xy - .5f) * _noisePower;

			// sample fluid texture
			#if defined(_Fluid_UV1)
			float4 blood = tex2D (_FluidTex, IN.uv_FluidTex + uvNoise);
			#elif defined(_Fluid_UV2)
			float4 blood = tex2D (_FluidTex, IN.uv2_FluidTex + uvNoise);
			#endif

			// fluid height
			float amount = min (blood.x, 1.0f);
			// dryness of the fluid
			float age = lerp (_dryAmount, 1.0f, blood.y);

			float3 normal = UnpackNormal (tex2D (_NormalTex, IN.uv_NormalTex));
			normal.xy *= _NormalStrength;

			// fluid normal
			float3 bloodNormal = float3(((float2)blood.zw - float2(.5f, .5f)) * 2 * _FluidNormalStrength, 1);
			float3 n = lerp (normal, bloodNormal, amount * age);

			o.Albedo = lerp (c.rgb, _FluidColor.rgb * age, amount * _FluidColor.a);
			o.Metallic = lerp (_Metallic, _FluidMetallic * age, amount);
			o.Smoothness = lerp (_Smoothness, _FluidSmoothness * age, amount);
			o.Normal = n;
			o.Alpha = c.a;
		}

		ENDCG
	}
	FallBack "Diffuse"
}
