Shader "Exploded Views/Trilling Opaque Point" {
	Properties { 
		_TurbulenceAmplitude( "Turbulence amplitude", float ) = 0.03
		_TurbulenceFrequency( "Turbulence frequency", float ) = 5.0
		_TurbulenceCurliness( "Turbulence curliness", float ) = 1.0
		
		fogColor("Fog color", Color) = (0,0,0,1)
		fogDensity("Fog density", float) = 0.005
		
		_TunnelD ("Tunnel Distance", float) = 1.0
		_TunnelRadius("Tunnel Radius", float) = 0.75
		_TunnelAspect("Tunnel Aspect", float) = 1.33

		minSize ("Min. Point Size", float) = 1
		maxSize ("Max. Point Size", float) = 1
		attA ("Quad. size attenuation", float) = 0
		attB ("Lin. size attenuation", float) = 0
		attC ("Const. size attenuation", float) = 0

		_SubLod ( "SubLod", Range(0,1) ) = 0
	}
	
	// Levels of detail:
	//   0:	simple (possibly foggy) points
	// 250:	turbulence
	// 500:	turbulence + near-tint fading in + tunnel

	SubShader {
	LOD 500
	Tags { "Queue" = "Geometry" }
	Pass {

	// we do the fog "manually" because it is broken on target 3.0 shaders on direct x
	Fog { Mode Off }

CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"
#include "ExplodedShaderLib.cginc"
#include "noise3d.cginc"

uniform float _TurbulenceAmplitude, _TurbulenceCurliness, _TurbulenceFrequency;
uniform float _SubLod;
uniform float _TunnelD, _TunnelRadius, _TunnelAspect;
uniform float attA, attB, attC;
uniform float minSize, maxSize;
uniform float4 fogColor;
uniform float fogDensity;

PointV2F vert (PointVIn v)
{
	PointV2F o;
	
	//v.vertex = mul(UNITY_MATRIX_MV, v.vertex);
	v.vertex.xyz += _TurbulenceAmplitude * snoise3( _TurbulenceCurliness * v.vertex.xyz, _TurbulenceFrequency * _Time);
	o.pos = mul (UNITY_MATRIX_MVP, v.vertex);

	if (o.pos.w != 1.0) {
		float displacement = max(0.0, 1.0 - o.pos.z / _TunnelD);
		o.pos.xy += normalize(o.pos.xy)
						* displacement 
						* float2(1.0,_TunnelAspect)
						* _TunnelRadius;
	}

    // billboard...
	Billboard( o.pos, v.texcoord.xy, AttenuatedSize(o.pos, attA, attB, attC, minSize, maxSize) );

    // vertex color + fog
    o.color = lerp( v.color, fogColor, 1 - exp2( fogDensity * o.pos.z ));

 	return o;
}

half4 frag(PointV2F i) : COLOR { 
	return i.color;
}

ENDCG

		}
	}
	

	SubShader {
		LOD 250
		Tags { "Queue" = "Geometry" }

		Pass {
		
		Fog { Mode Off }

		
CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"
#include "ExplodedShaderLib.cginc"
#include "noise3d.cginc"

uniform float _TurbulenceAmplitude, _TurbulenceCurliness, _TurbulenceFrequency;
uniform float attA, attB, attC;
uniform float minSize, maxSize;
uniform float4 fogColor;
uniform float fogDensity;

PointV2F vert (PointVIn v)
{
	PointV2F o;

	//v.vertex = mul(UNITY_MATRIX_MV, v.vertex);
	v.vertex.xyz += _TurbulenceAmplitude * snoise3( _TurbulenceCurliness * v.vertex.xyz, _TurbulenceFrequency * _Time);
	o.pos = mul (UNITY_MATRIX_MVP, v.vertex);

    // billboard...
	Billboard( o.pos, v.texcoord.xy, AttenuatedSize(o.pos, attA, attB, attC, minSize, maxSize) );

    // vertex color + fog
    o.color = lerp( v.color, fogColor, 1 - exp2( fogDensity * o.pos.z ));

 	return o;
}

half4 frag(PointV2F i) : COLOR { 
	return i.color; 
}

ENDCG

		}
	}

	SubShader {
	LOD 0
	Pass {
CGPROGRAM
#pragma exclude_renderers gles
#pragma vertex vert

#include "UnityCG.cginc"
#include "ExplodedShaderLib.cginc"

uniform float attA, attB, attC;
uniform float minSize, maxSize;
uniform float4 fogColor;
uniform float fogDensity;

PointV2F vert (PointVIn v)
{
	PointV2F o;
	o.pos = mul (UNITY_MATRIX_MVP, v.vertex);

    // billboard...
	Billboard( o.pos, v.texcoord.xy, AttenuatedSize(o.pos, attA, attB, attC, minSize, maxSize) );

    // vertex color + fog
    o.color = lerp( v.color, fogColor, 1 - exp2( fogDensity * o.pos.z ));

 	return o;
}

ENDCG
		}
	}
}
