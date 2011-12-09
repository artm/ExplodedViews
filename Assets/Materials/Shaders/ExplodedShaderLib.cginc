#ifndef EXPLDED_SHADER_LIB
#define EXPLDED_SHADER_LIB

struct PointVIn {
    float4 vertex : POSITION;
    float4 texcoord : TEXCOORD;
    float4 color : COLOR;
};

struct PointV2F {
    float4 pos : POSITION;
    float4 color : COLOR;
};

struct TexPointV2F {
    float4 pos : POSITION;
    float4 color : COLOR;
    float4 texcoord : TEXCOORD;
};

// Shift a billboard corner size pixels along its corner
inline void Billboard(inout float4 pos, in float2 corner, float2 size)
{
	pos.xy += size * 2.0 / _ScreenParams.xy * corner * pos.w;
}

inline void Billboard(inout float4 pos, in float2 corner, float size)
{
	Billboard(pos, corner, float2(size));
}

inline float Quadratic(float d, float a, float b, float c)
{
	return 1.0 / (a*d*d + b*d + c);
}

// returns (distance, size)
inline float AttenuatedSize(float4 pos, float a, float b, float c, float minSize, float maxSize)
{
	return (pos.w == 1.0) ? minSize : max(minSize, maxSize * min(1.0, Quadratic(pos.z, a, b, c)));
}

inline float exp2(float x)
{
	return exp( - x * x );
}

#endif // EXPLDED_SHADER_LIB