#ifndef __UI_GLICHFUNCTION__
#define __UI_GLICHFUNCTION__
#include "UI_GlichEffectInput.hlsl"


struct a2f
{ 
	float3 positionOS:POSITION;
	float4 color : COLOR;
	float2 uv:TEXCOORD0;
};

struct v2f
{
	float4 positionCS:SV_POSITION;
	float4 color : COLOR;
	float2 uv:TEXCOORD0;
	//half3 positionNDC : TEXCOORD1;
};
float Noise(half speed,half amplitude,half indensity)
{
    float timeX = _Time.y*speed;
    float splitAmout = (1.0 + sin(timeX * 6.0)) * 0.5;
    splitAmout *= 1.0 + sin(timeX * 16.0) * 0.5;
    splitAmout *= 1.0 + sin(timeX * 19.0) * 0.5;
    splitAmout *= 1.0 + sin(timeX * 27.0) * 0.5;
    splitAmout = pow(splitAmout, amplitude);
    splitAmout *= (0.05 * indensity);
    return splitAmout;
}

float randomNoise(float2 c)
{
    return frac(sin(dot(c.xy + _Time.y, float2(12.9898, 78.233))) * 43758.5453);
}

float rand(float x,half speed) {
    float period = 10.0; // 周期（秒）
    float safeTime = fmod(_Time.y, period); // 时间在 [0, period) 循环
    return frac(sin(x+ safeTime*0.01*speed) * 43758.5453);
}

half CalculateGray(half theuv,half lineAmount, half lineSpeed)
{
				
    // 生成随机强度梯度线条
    float period = 10.0; // 周期（秒）
    float safeTime = fmod(_Time.y, period); // 时间在 [0, period) 循环
    half stripePos = theuv * lineAmount*5;
    int stripeIndex = floor(stripePos);
    half timeFactor = safeTime * lineSpeed;
    half gray = frac(sin(stripeIndex * 12.9898 + timeFactor * 0.5) * 43758.5453);
    return  gray;
}

// 像素效果 传入uv
void ColorLineEffect(inout half4 theCol, half2 uv)
{
	//half random = randomNoise(uv);
	half randomValue = rand(100,_ColorLineSpeed);
	half4 noiseTex = SAMPLE_TEXTURE2D(_NoiseTex,sampler_NoiseTex,half2(uv.y*_ColorLineScale,uv.y*_ColorLineScale+randomValue));
	half theColoR = smoothstep(0.2, 1,noiseTex.r*uv.y)*_ColorLineIntensity;
	half theColrG = smoothstep(0.2, 1,noiseTex.r*(1-uv.y))*_ColorLineIntensity;
	theCol = half4(theColoR,theColrG,0,max(theColoR,theColrG))*step(0.5,_ColorLine)+theCol*step(_ColorLine,0.5);
}

// 色散效果，传入颜色 顶点颜色 uv
void DispersionEffect(inout half4 theCol, half4 inputCol,half2 uv)
{
	half splitAmout =Noise(_DSSpeed,_DSAmplitude,_DSIndensity);// _Indensity * randomNoise(_Time.y,2);
	half4 colorR = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,half2(uv.x+step(0.5h,_HorizonTalToggle)*splitAmout,uv.y+step(0.5h,_VerticalToggle)*splitAmout))*inputCol;
	///half4 ColorG = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv);
	half4 colorG = theCol;
	half4 colorB = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,half2(uv.x-step(0.5h,_HorizonTalToggle)*splitAmout,uv.y-step(0.5h,_VerticalToggle)*splitAmout))*inputCol;
	theCol.rgb *= (1.0 - splitAmout * 0.5);
	theCol = half4(colorR.r*colorR.a,colorG.g*colorG.a,colorB.b*colorB.a,max(max(colorR.a,colorG.a),colorB.a));   //max(max(ColorR.r,ColorG.g),ColorB.b)

}


			
// 错位线条故障效果	传入uv		
void  BlockLineGlitchEffect(inout half2 uv)
{
	half grayU = CalculateGray(uv.y,_LineAmount,_LineSpeed);
	half grayV = CalculateGray(uv.x,_LineAmount,_LineSpeed);
	uv =half2(uv.x+grayU*_LineOffset*step(0.5h,_HorizonTalToggle2),uv.y+grayV*_LineOffset*step(0.5h,_VerticalToggle2));
	//return theuv;
}

// 扰动效果 传入uv
void DisturbEffect(inout half2 uv)
{
	half2 maskuv = uv - _MaskCenter.xy;
	half aspect = _ScreenParams.x / _ScreenParams.y;
	maskuv *= aspect;
	half distance = length(maskuv);
	half mask = smoothstep(_MaskRadius, _MaskRadius,distance);
	half2 uv1 = uv * _NoiseScale + half2(0,frac(_Time.y*_NoiseSpeed));
	half4 noiseTex = SAMPLE_TEXTURE2D(_NoiseTex,sampler_NoiseTex,uv1);
	half2 theuv = half2(uv.x , uv.y+noiseTex.r*_DisturbInstensity);
	uv = lerp(theuv,uv,mask);
	//return theuv;
}

//闪烁效果 传入颜色 uv
void FlashEffect(inout half4 theCol, half2 uv)
{
	half4 maskTex = SAMPLE_TEXTURE2D(_FlashMaskTex,sampler_FlashMaskTex,uv);
	half randomValue = rand(100,_FlashSpeed);
				
	half randomRGB;
	randomRGB = randomValue <0.3 ?  maskTex.r : (randomValue<0.6?maskTex.g:maskTex.b);
	half randomRGB2 = randomValue <0.4 ?  maskTex.r : (randomValue<0.8?maskTex.g:maskTex.b);
	half lerpnum = saturate(step(0.01h,randomRGB2)); 
	half3 randomCol = lerp(0,1,randomValue*randomRGB);
	half3 randomCol2 = lerp (theCol.rgb,1-theCol.rgb,randomValue*lerpnum);
	half3 themask=  lerp(randomCol,randomCol2,theCol.a); 
				
	theCol.rgb = lerp(theCol.rgb,themask,lerpnum); 
	theCol.a = max(theCol.a,lerpnum);
}

// 像素效果 传入uv
void PixelEffect(inout half2 uv)
{
	uv = floor(uv*_PixelScale)/_PixelScale*step(0.5,_Pixel)+uv*step(_Pixel,0.5);
}

			// void Unity_Rotate_Radians_float(float2 UV, float2 Center, float Rotation, out float2 Out)
			// {
			//     UV -= Center;
			//     float s = sin(Rotation);
			//     float c = cos(Rotation);
			//     float2x2 rMatrix = float2x2(c, -s, s, c);
			//     rMatrix *= 0.5;
			//     rMatrix += 0.5;
			//     rMatrix = rMatrix * 2 - 1;
			//     UV.xy = mul(UV.xy, rMatrix);
			//     UV += Center;
			//     Out = UV;
			// }
			// inline float2 unity_voronoi_noise_randomVector (float2 UV, float offset)
			// {
			//     float2x2 m = float2x2(15.27, 47.63, 99.41, 89.98);
			//     UV = frac(sin(mul(UV, m)) * 46839.32);
			//     return float2(sin(UV.y*+offset)*0.5+0.5, cos(UV.x*offset)*0.5+0.5);
			// }
			//
			// void Unity_Voronoi_float(float2 UV, float AngleOffset, float CellDensity, out float Out, out float Cells)
			// {
			//     float2 g = floor(UV * CellDensity);
			//     float2 f = frac(UV * CellDensity);
			//     float t = 8.0;
			//     float3 res = float3(8.0, 0.0, 0.0);
			//
			//     for(int y=-1; y<=1; y++)
			//     {
			//         for(int x=-1; x<=1; x++)
			//         {
			//             float2 lattice = float2(x,y);
			//             float2 offset = unity_voronoi_noise_randomVector(lattice + g, AngleOffset);
			//             float d = distance(lattice + offset, f);
			//             if(d < res.x)
			//             {
			//                 res = float3(d, offset.x, offset.y);
			//                 Out = res.x;
			//                 Cells = res.y;
			//             }
			//         }
			//     }
			// }

//波点效果 传入颜色 uv
void PolkaDotEffect(inout half4 theCol, half2 uv)
{
	half4 maskTex = SAMPLE_TEXTURE2D(_PolkadotMaskTex,sampler_PolkadotMaskTex,uv);
	// // 纠正屏幕比率
	// half2 screenUV = half2(screenPos.x,screenPos.y/(_ScreenParams.x/_ScreenParams.y));
	
	half2 theUV = uv*_PolkaDotDensity;
	//Unity_Rotate_Radians_float(theUV,half2(0.5,0.5),_PolkaDotRotation,theUV);
	//half result,cells;
	//Unity_Voronoi_float(theUV,0,5,result,cells);
	half4 dotTex = SAMPLE_TEXTURE2D(_PolkadotTex,sampler_PolkadotTex,theUV);
	half4 col = dotTex.r*lerp(theCol,maskTex,_UseMask);  //step(result,0.3)*
	col.a = col.r+col.g+col.b;
	theCol = lerp(col,theCol+col,_UseMask);
	//return col;
}

void EdgeDetection(inout half4 color,half2 uv, half2 theuv)
{
	half4 neighbor =  SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv + theuv * _MainTex_TexelSize.xy);
	color =_OutlineColor * step(neighbor.a,_AlphaThreshold) +  color * step(_AlphaThreshold,neighbor.a);   //
	// if(neighbor.a < _AlphaThreshold)
	// {
	// 	color = _OutlineColor;
	// }
}

			
// 描边效果（较消耗）  传入颜色 uv  四次采样
half4 OutlineEffect( half4 color,half2 uv)
{
	clip(color.a - _AlphaThreshold);
	// 4方向偏移量（上下左右）
	half2 offsets[4] =
	{
		half2(0,_OutlineWidth),
		half2(0,-_OutlineWidth),
		half2(_OutlineWidth,0),
		half2(-_OutlineWidth,0)
	};
	EdgeDetection(color,uv,offsets[0]);
	EdgeDetection(color,uv,offsets[1]);
	EdgeDetection(color,uv,offsets[2]);
	EdgeDetection(color,uv,offsets[3]);

	// For循环取代
	 //边缘检测：若周边任意像素透明，则判定为边缘
	 // for(int i = 0; i < 4; i++)
	 // {
	 // 	half4 neighbor =  SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv + offsets[i] * _MainTex_TexelSize.xy);
	 // 	if(neighbor.a < _AlphaThreshold)
	 // 	{
	 // 		return _OutlineColor;
	 // 	}
	 // }
	return color;
}
#endif