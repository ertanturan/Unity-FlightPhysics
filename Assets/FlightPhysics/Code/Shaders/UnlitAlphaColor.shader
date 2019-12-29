// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Indie-Pixel/Unlit/UnlitAlphaColor"
{
	Properties
	{
		_Color("Color", Color) = (1,0.251319,0,1)
		_OutlineColor("Outline Color", Color) = (1,0.251319,0,1)
		_Width("Width", Range( 0 , 1)) = 0.2
		_LineCount("Line Count", Range( 0 , 100)) = 5
		_AnimationSpeed("AnimationSpeed", Range( 0 , 10)) = 5
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ }
		Cull Front
		CGPROGRAM
		#pragma target 3.0
		#pragma surface outlineSurf Outline nofog  keepalpha noshadow noambient novertexlights nolightmap nodynlightmap nodirlightmap nometa noforwardadd vertex:outlineVertexDataFunc 
		
		
		
		struct Input
		{
			half filler;
		};
		uniform half _Width;
		uniform half4 _OutlineColor;
		
		void outlineVertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float outlineVar = _Width;
			v.vertex.xyz += ( v.normal * outlineVar );
		}
		inline half4 LightingOutline( SurfaceOutput s, half3 lightDir, half atten ) { return half4 ( 0,0,0, s.Alpha); }
		void outlineSurf( Input i, inout SurfaceOutput o )
		{
			o.Emission = _OutlineColor.rgb;
		}
		ENDCG
		

		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Unlit alpha:fade keepalpha noshadow nolightmap  nodirlightmap vertex:vertexDataFunc 
		struct Input
		{
			half2 uv_texcoord;
		};

		uniform half4 _Color;
		uniform half _LineCount;
		uniform half _AnimationSpeed;

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 outlineinfo11 = 0;
			v.vertex.xyz += outlineinfo11;
		}

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			o.Emission = _Color.rgb;
			float clampResult10 = clamp( sin( ( ( i.uv_texcoord.x * _LineCount ) + ( _Time.y * _AnimationSpeed ) ) ) , 0.0 , 1.0 );
			o.Alpha = ( _Color.a * clampResult10 );
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=15500
-119;392;1906;953;2706.641;323.5334;1;True;True
Node;AmplifyShaderEditor.RangedFloatNode;14;-2171.208,-259.4781;Float;False;Property;_LineCount;Line Count;3;0;Create;True;0;0;False;0;5;6;0;100;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;7;-2154.998,-489.3242;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TimeNode;18;-2162.515,5.434897;Float;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;20;-2248.918,215.8348;Float;False;Property;_AnimationSpeed;AnimationSpeed;4;0;Create;True;0;0;False;0;5;7.41;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;13;-1825.208,-392.4782;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;19;-1878.515,117.4348;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;6;-1613.474,466.1465;Float;False;1068.142;388.5989;Outline;4;11;3;5;4;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode;15;-1501.208,-156.8782;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;4;-1563.474,516.1464;Float;False;Property;_OutlineColor;Outline Color;1;0;Create;True;0;0;False;0;1,0.251319,0,1;0.04245281,1,0.8961043,0.2431373;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SinOpNode;8;-1306.575,-150.2952;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;5;-1560.474,754.1462;Float;False;Property;_Width;Width;2;0;Create;True;0;0;False;0;0.2;0.027;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;10;-902.4409,-117.1121;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;2;-995.4756,-491.3145;Float;False;Property;_Color;Color;0;0;Create;True;0;0;False;0;1,0.251319,0,1;1,0.4643592,0.25,1;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OutlineNode;3;-1159.474,666.1462;Float;False;0;True;None;0;0;Front;3;0;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;11;-780.0887,667.5976;Float;False;outlineinfo;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;9;-450.2407,-27.11206;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;12;-242.6348,259.0767;Float;False;11;0;1;FLOAT3;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Half;False;True;2;Half;ASEMaterialInspector;0;0;Unlit;Indie-Pixel/Unlit/UnlitAlphaColor;False;False;False;False;False;False;True;False;True;False;False;False;False;False;True;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;False;0;False;Transparent;;Transparent;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0.08;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;13;0;7;1
WireConnection;13;1;14;0
WireConnection;19;0;18;2
WireConnection;19;1;20;0
WireConnection;15;0;13;0
WireConnection;15;1;19;0
WireConnection;8;0;15;0
WireConnection;10;0;8;0
WireConnection;3;0;4;0
WireConnection;3;1;5;0
WireConnection;11;0;3;0
WireConnection;9;0;2;4
WireConnection;9;1;10;0
WireConnection;0;2;2;0
WireConnection;0;9;9;0
WireConnection;0;11;12;0
ASEEND*/
//CHKSM=C10827146A9E2784D375AB39EFC0AC9C91B86A80