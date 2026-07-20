// 雨滴特效
Shader "Hidden/ReformSim/RaindropEffect"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _RaindropTex ("Raindrop Texture", 2D) = "white" {}
        _DropletTex ("Droplet Texture", 2D) = "white" {}
        
        _Refraction ("Refraction", Vector) = (0.4, 0.6, 0, 0)
        _LightPosition ("Light Position", Vector) = (-1, -1, 2, 0)
        _RaindropColor ("Raindrop Color", Color) = (0.2, 0.2, 0.2, 0.8)
        _AlphaSmoothRange ("Alpha Smooth Range", Vector) = (0.95, 1.0, 0, 0)
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _RaindropTex;
            sampler2D _DropletTex;
            
            float4 _Refraction;
            float4 _LightPosition;
            half4 _RaindropColor;
            float4 _AlphaSmoothRange;

            half4 frag(v2f i) : SV_Target
            {
                half4 raindropTexColor = tex2D(_RaindropTex, i.uv);
                half4 dropletTexColor = tex2D(_DropletTex, i.uv);
                half4 blendTexColor = raindropTexColor + dropletTexColor - raindropTexColor * dropletTexColor * 2.0;

                float refractive = _Refraction.x + blendTexColor.b * _Refraction.y;
                float2 refractiveUV = i.uv - (blendTexColor.xy - float2(0.5, 0.5)) * float2(refractive, refractive);
                half4 texColor = tex2D(_MainTex, refractiveUV.xy);

                float3 normal = normalize(float3((blendTexColor.xy - float2(0.5, 0.5)) * 2, -1.0));
                float3 lightDir = normalize(_LightPosition.w * float3(i.uv, 0.0) - _LightPosition.xyz);
                float3 diffuseColor = saturate(dot(lightDir, normal)) * _RaindropColor.rgb * _RaindropColor.a;

                half4 color = texColor + half4(diffuseColor, 1.0);

                half4 bgColor = tex2D(_MainTex, i.uv);

                half alpha = max(dropletTexColor.a, raindropTexColor.a);
                alpha = smoothstep(_AlphaSmoothRange.x, _AlphaSmoothRange.y, alpha);

                color = color * alpha + bgColor * (1 - alpha);

                color.a = 1.0;
                return color;
            }
            ENDCG
        }
    }
}