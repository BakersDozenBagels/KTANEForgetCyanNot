Shader "Custom/ScreenShader"
{
	Properties
	{
		_LeftA ("Base A", 2D) = "white" {}
		[NoScaleOffset]
		_LeftB ("Alt A", 2D) = "white" {}
		_RightA ("Base B", 2D) = "white" {}
		[NoScaleOffset]
		_RightB ("Alt B", 2D) = "white" {}

		_ColorA ("Background Color A", Color) = (1,1,1,1)
		_ColorB ("Background Color B", Color) = (1,1,1,1)
		_BeamColor ("Beam Color", Color) = (1,1,1,1)
		_Position ("Position", Range(-0.2, 1.2)) = 0.5
	}
	SubShader
	{
		Pass
		{
			Tags { "RenderType"="Opaque" }
			LOD 200

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#include "UnityCG.cginc"
			
			uniform sampler2D _LeftA;
			uniform float4 _LeftA_ST;
			uniform sampler2D _LeftB;
			uniform sampler2D _RightA;
			uniform float4 _RightA_ST;
			uniform sampler2D _RightB;
			uniform fixed4 _ColorA;
			uniform fixed4 _ColorB;
			uniform fixed4 _BeamColor;
			uniform float _Position;

			struct Appdata
			{
				float2 uv : TEXCOORD0;
				float4 pos : POSITION;
			};

			struct Interpolators
			{
				float2 uv : TEXCOORD0;
				float4 uv2 : TEXCOORD1;
				float4 pos : SV_POSITION;
			};

			Interpolators vert (Appdata IN) 
			{
				Interpolators OUT;
				OUT.uv = IN.uv;
				OUT.uv2 = float4(TRANSFORM_TEX(IN.uv, _LeftA), TRANSFORM_TEX(IN.uv, _RightA));
				OUT.pos = UnityObjectToClipPos(IN.pos);
				return OUT;
			}

			fixed4 frag (Interpolators IN) : SV_TARGET
			{
				float x = IN.uv.x - (IN.uv.y - .5) / 3;
				
				fixed4 bgcol = x < _Position ? _ColorA : _ColorB;

				fixed4 fgA = x < _Position ? tex2D(_LeftB, IN.uv2.xy) : tex2D(_LeftA, IN.uv2.xy);
				fixed4 fgB = x < _Position ? tex2D(_RightB, IN.uv2.zw) : tex2D(_RightA, IN.uv2.zw);
				
				fixed4 col = lerp(lerp(bgcol, fgA, fgA.a), fgB, fgB.a);

				return lerp(_BeamColor, col, clamp((abs(x - _Position) - 0.015) * 20, 0, 1));
			}
			ENDCG
		}
	}
	FallBack "Diffuse"
}
