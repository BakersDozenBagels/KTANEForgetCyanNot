Shader "Custom/SpinShader"
{
	Properties
	{
		[MainColor]
		_BeamColor ("Beam Color", Color) = (1,1,1,1)
		_Color1 ("BG Color 1", Color) = (1,1,1,1)
		_Color2 ("BG Color 2", Color) = (1,1,1,1)
		_Position1 ("Beam 1 Position", Range(0.0, 1.0)) = 0.5
		_Position2 ("Beam 2 Position", Range(0.0, 1.0)) = 0.5
		[HideInInspector]
		_MainTex ("Dummy Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Lambert fullforwardshadows
		#pragma target 3.0

		struct Input
		{
			float2 uv_MainTex : TEXCOORD0;
		};

		uniform float4 _BeamColor;
		uniform float4 _Color1;
		uniform float4 _Color2;
		uniform float _Position1;
		uniform float _Position2;

		void surf (Input IN, inout SurfaceOutput o) 
		{
			float x = IN.uv_MainTex.x;

			fixed4 outer = _Position1 > _Position2 ? _Color1 : _Color2;
			fixed4 inner = _Position1 > _Position2 ? _Color2 : _Color1;

			const float BeamWidth = 0.01;
			const float BeamFadeWidth = 0.025;

			#define R(x) x > 0.5 ? 1 - x : x
			float t = clamp((min(R(abs(_Position1 - x)), R(abs(_Position2 - x))) - BeamWidth) * (1 / BeamFadeWidth), 0, 1);
			#undef R

			o.Albedo = 
				max(_Position1, _Position2) > (1 - BeamWidth) && x < max(_Position1, _Position2) - (1 - BeamWidth) ? _BeamColor :
			    x < max(min(_Position1, _Position2) - BeamWidth, 0) ? lerp(_BeamColor, outer, t) :
				x < min(min(_Position1, _Position2) + BeamWidth, 1) ? _BeamColor :
				x < max(max(_Position1, _Position2) - BeamWidth, 0) ? lerp(_BeamColor, inner, t) :
				x < min(max(_Position1, _Position2) + BeamWidth, 1) ? _BeamColor :
				min(_Position1, _Position2) < BeamWidth && x > min(_Position1, _Position2) + (1 - BeamWidth) ? _BeamColor :
				lerp(_BeamColor, outer, t);

			o.Emission = o.Albedo * .2;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
