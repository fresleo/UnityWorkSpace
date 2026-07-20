// 雨滴
Shader "ReformSim/Raindrop"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    }

    SubShader
    {
        //Cull Off 
        ZWrite Off ZTest Always
        
        //Blend OneMinusDstColor OneMinusSrcColor
        Blend One OneMinusSrcAlpha
        //Blend SrcAlpha OneMinusSrcAlpha

        pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID // necessary only if you want to access instanced properties in fragment Shader.
            };

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o); // necessary only if you want to access instanced properties in the fragment Shader.

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }

            sampler2D _MainTex;

            // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
            // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
            // #pragma instancing_options assumeuniformscaling
            UNITY_INSTANCING_BUFFER_START(Props)
                // put more per-instance properties here
                UNITY_DEFINE_INSTANCED_PROP(float, _Size)
            UNITY_INSTANCING_BUFFER_END(Props)

            half4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i); // necessary only if any instanced properties are going to be accessed in the fragment Shader.

                half4 texColor = tex2D(_MainTex, i.uv);
                float size = UNITY_ACCESS_INSTANCED_PROP(Props, _Size);
                
                half4 color = half4(texColor.rg * texColor.a, size * texColor.a, texColor.a);
                return color;
            }
            ENDCG
        }
    }
}