Shader "Exploded Views/Opaque Point" {
	Properties {
		minSize ("Min. Point Size", float) = 1
		maxSize ("Max. Point Size", float) = 1
		attA ("Quad. size attenuation", float) = 0
		attB ("Lin. size attenuation", float) = 0
		attC ("Const. size attenuation", float) = 0
		fogColor("Fog color", Color) = (0,0,0,1)
		fogDensity("Fog density", float) = 0.005
	}
	SubShader {
		Tags { "Queue" = "Geometry" }
		Pass {

		// we do the fog "manually" because it is broken on target 3.0 shaders on direct x
		Fog { Mode Off }

CGPROGRAM
// Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it does not contain a surface program
// or both vertex and fragment programs.
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
