Shader "MapMagic/TerrainPreview" 
{
	Properties
	{
		_Control("Control (RGBA)", 2D) = "red" {}
		_Preview("Preview (RGBA)", 2D) = "red" {}
		_Whites("Whites", Color) = (0,1,0,1)
		_Blacks("Blacks", Color) = (1,0,0,1)

		_Rect("Rect", Vector) = (0,0,1,1)
		_Scale("Scale", Float) = 1 //pixel size
	}

	SubShader {
		Tags {
			"Queue" = "Geometry-100"
			"RenderType" = "Opaque"
		}

		CGPROGRAM
		#pragma surface surf Standard vertex:Vert fullforwardshadows
		#pragma multi_compile_fog
		#pragma target 3.0
		#pragma exclude_renderers gles
		#include "UnityPBSLighting.cginc"

		#pragma multi_compile __ _TERRAIN_NORMAL_MAP

		#define TERRAIN_STANDARD_SHADER
		#define TERRAIN_SURFACE_OUTPUT SurfaceOutputStandard
//		#include "TerrainSplatmapCommon.cginc"

		sampler2D _Preview;
		float4 _Preview_ST;
		half4 _Whites;
		half4 _Blacks;
		float4 _Rect;
		float _Scale;

		struct Input {
			//float2 tc_Control : TEXCOORD4;
			float3 worldPos; 
			float3 objPos;
			float3 pos;
			//UNITY_FOG_COORDS(5)
		};

		void Vert(inout appdata_full v, out Input data)
		{
			UNITY_INITIALIZE_OUTPUT(Input, data);
			//data.tc_Control = TRANSFORM_TEX(v.texcoord, _Preview);	// Need to manually transform uv here, as we choose not to use 'uv' prefix for this texcoord.
			data.pos = UnityObjectToClipPos(v.vertex);
			data.objPos = v.vertex;
			//UNITY_TRANSFER_FOG(data, pos);

		}

		void surf (Input IN, inout SurfaceOutputStandard o)
		{
			//half4 splat_control;
			//half weight;
			//fixed4 mixedDiffuse;
			//half4 defaultSmoothness = half4(_Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3);
			//SplatmapMix(IN, defaultSmoothness, splat_control, weight, mixedDiffuse, o.Normal);
			//o.Albedo = mixedDiffuse.rgb;
			//o.Alpha = weight;
			//o.Smoothness = mixedDiffuse.a;
			//o.Metallic = dot(splat_control, half4(_Metallic0, _Metallic1, _Metallic2, _Metallic3));

			//float2 uv = float2((IN.worldPos.x/_Scale -_Rect.x) / _Rect.z, (IN.worldPos.z/_Scale -_Rect.y) / _Rect.w); //float2(IN.worldPos.x/_Rect.z, IN.worldPos.z/ _Rect.w);// - float2(_Rect.x, _Rect.y);
			float2 uv = float2((IN.objPos.x / _Scale) / _Rect.z, (IN.objPos.z / _Scale) / _Rect.w); //float2(IN.worldPos.x/_Rect.z, IN.worldPos.z/ _Rect.w);// - float2(_Rect.x, _Rect.y);


			half4 color = tex2D(_Preview, uv);
			float val = (color.r+color.g+color.b)/3;
			//o.Albedo = _Whites*val + _Blacks*(1-val);
			o.Albedo = half3(min(1, (1 - val)*1.5), min(1, val*1.5), 0);
			o.Alpha = 1;
		}
		ENDCG
	}

	Dependency "BaseMapShader" = "MapMagic/TerrainPreview"

	Fallback "Nature/Terrain/Diffuse"
}