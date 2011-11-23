Shader "Exploded Views/Opaque Point" {
	Properties {
		minSize ("Min. Point Size", float) = 1
		maxSize ("Max. Point Size", float) = 1
		attA ("Quad. size attenuation", float) = 0
		attB ("Lin. size attenuation", float) = 0
		attC ("Const. size attenuation", float) = 0
	}
	SubShader {
		Tags { "Queue" = "Geometry" }
		Pass {
CGPROGRAM
// Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it does not contain a surface program
// or both vertex and fragment programs.
#pragma exclude_renderers gles
#pragma vertex vert

#include "UnityCG.cginc"
#include "ExplodedShaderLib.cginc"

float attA, attB, attC;
float minSize, maxSize;

PointV2F vert (PointVIn v)
{
	PointV2F o;
	
	o.pos = mul (UNITY_MATRIX_MVP, v.vertex);

	float d = length(o.pos);
	o.fog = d;

	float size = (o.pos.w == 1.0) ? 1 : max(minSize, maxSize * min(1.0, Quadratic(d, attA, attB, attC)));


    // billboard...
	Billboard( o.pos, v.texcoord.xy, size );

    // pass the color along...
    o.color = v.color;

 	return o;
}

ENDCG	

		}
	}
}
