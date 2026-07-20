#ifndef CLOUD_SHADOW
#define CLOUD_SHADOW

TEXTURE2D(_CloudShadowTex);		SAMPLER(sampler_CloudShadowTex);

float _CloudShadowTiling;
float2 _CloudShadowSpeed;
half4 _CloudShadowColor;

void CloudShadow(inout half3 col, float3 positionWS)
{
	float2 cloudUV = positionWS.xz * _CloudShadowTiling + _Time.x * _CloudShadowSpeed;
	half4 cloudCol= _CloudShadowColor * SAMPLE_TEXTURE2D(_CloudShadowTex, sampler_CloudShadowTex, cloudUV);
	col = lerp(col, col * cloudCol.rgb, cloudCol.a);
}

#endif