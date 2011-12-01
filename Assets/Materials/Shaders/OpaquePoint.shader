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
#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"
#include "ExplodedShaderLib.cginc"

float attA, attB, attC;
float minSize, maxSize;

PointV2F vert (PointVIn v)
{
	PointV2F o;
	
	o.pos = mul (UNITY_MATRIX_MVP, v.vertex);

	float d = length(o.pos);
	//o.fog = o.pos.z;

	float attenuation = Quadratic(d, attA, attB, attC);
	float size = max(minSize, maxSize * min(1.0, attenuation));

    // billboard...
	Billboard( o.pos, v.texcoord.xy, size );

    // pass the color along...
    o.color = v.color;

 	return o;
}

half4 frag(PointV2F i) : COLOR { 
	return i.color; 
}

ENDCG	

		}
	}
}
