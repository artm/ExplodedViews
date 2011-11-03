Shader "Exploded Views/Opaque Point" {
	Properties { 
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


PointV2F vert (PointVIn v)
{
	PointV2F o;
	
	o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
	o.fog = o.pos.z;

    // billboard...
	Billboard( o.pos, v.texcoord.xy, 1.0 );

    // pass the color along...
    o.color = v.color;

 	return o;
}

ENDCG	

		}
	}
}
