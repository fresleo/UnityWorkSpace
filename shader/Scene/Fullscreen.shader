/*
 *   为了美术插入一个面片，并且想3D角色不受影响，所以需要将面片画到3D队列中
 *   不够通用的Shader，仅为了支持该需求   
 */

Shader "XKnight/Scene/Fullscreen"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        
        ZTest LEqual
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        // Default
        Pass
        {
            Name "Default"
            
            CGPROGRAM
            #pragma target 2.0
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

            sampler2D _MainTex; float4 _MainTex_ST;
            float _Luminance;

            v2f vert (appdata v)
            {
                v2f o = (v2f)0;
                
                o.uv = v.uv;
                
                float2 remap = o.uv.xy * 2.0f - 1.0f;
                // z轴改为1是为了让角色不受影响
                v.vertex.xyz = float3(remap, 0.99);
                #if UNITY_REVERSED_Z
                v.vertex.z = 0.001;
                #endif
                o.vertex = v.vertex;
                
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
