#ifndef PREETHAM_ATMOSPHERE_INCLUDED
#define PREETHAM_ATMOSPHERE_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

// PI
#define PI14  0.079577f  // 1 / (4*pi).
#define PI316 0.059683f  // 3 / (16 * pi).

// Exponent
#define ATMOSPHERE_EXPONENT(color) _AtmosphereExponent > 1.0 ? pow(color, _AtmosphereExponent) : color

// Sun
float3 _SunBetaMiePhase;
float _SunMieScattering;
float3 _SunMieColor;

// Moon
float3 _MoonBetaMiePhase;
float _MoonMieScattering;
float3 _MoonMieColor;

float _SunE;
float3 _SunAtmosphereTint;
float3 _MoonAtmosphereTint;
float3 _FadeParams;
float _AtmosphereExponent;

float3 _BetaRay;
float3 _BetaMie;
float _RayleighZenithLength;
float _MieZenithLength;

// Simplified Henyey Greenstein phase function for moon.
inline float3 MiePhaseSimplified(float cosTheta, float3 betaMiePhase, float scattering, float3 color)
{
	return (PI14 * (betaMiePhase.x / (betaMiePhase.y - (betaMiePhase.z * cosTheta)))) * scattering * color;
}

// Cornette Sharks Henyey Greenstein phase function with small changes.
inline float3 MiePhase(float cosTheta, float3 betaMiePhase, float scattering, float3 color)
{
	return (1.5 * betaMiePhase.x * ((1.0 + cosTheta * cosTheta) *
		pow(betaMiePhase.y - (betaMiePhase.z * cosTheta), -1.5))) * scattering * color;
}

inline float RayleighPhase(float cosTheta)
{
	return PI316 * (1.0 + (cosTheta * cosTheta));
}

// Defautl optical depth.
inline void OpticalDepth(float dir, inout float3 sr, inout float3 sm)
{
    float h = saturate((dir));
    float3 zenith = acos(h);
    zenith = (cos(zenith) + 0.15 * pow(93.885 - ((zenith * 180.0) / PI), -1.253));

    sr = _RayleighZenithLength / zenith;
    sm = _MieZenithLength / zenith;
}

inline float3 AtmosphericScattering(float3 sr, float3 sm, float2 cosTheta)
{
	float3 fex = exp(-(_BetaRay * sr + _BetaMie * sm)); // Combined extinction factor.
	float3 finalFex = saturate(lerp(1.0 - fex, (1.0 - fex) * (fex), (_FadeParams.z)));

	float3 sunBRT = RayleighPhase(cosTheta.x) * _BetaRay;
	float3 moonBRT = RayleighPhase(cosTheta.y) * _BetaRay;

	float3 sunMiePhase = MiePhase(cosTheta.x, _SunBetaMiePhase, _SunMieScattering, _SunMieColor);
	float3 sunBMT = (sunMiePhase * _BetaMie) * _SunMieColor * finalFex.r;

	float3 moonMiePhase = MiePhaseSimplified(cosTheta.y, _MoonBetaMiePhase, _MoonMieScattering, _MoonMieColor);
	float3 moonBMT = (moonMiePhase * _BetaMie);

	float3 SUN_BRMT = (sunBRT + sunBMT) / (_BetaRay + _BetaMie);
	float3 MOON_BRMT = (moonBRT + moonBMT) / (_BetaRay + _BetaMie);

	float3 inscatter = (_SunE * _FadeParams.x) * (SUN_BRMT * finalFex) * _SunAtmosphereTint;

	float3 nightInscatter = (_SunE * _FadeParams.y) * (MOON_BRMT * (1.0 - fex)) * _MoonAtmosphereTint;

	return (inscatter + nightInscatter) * 0.5;
}

#endif