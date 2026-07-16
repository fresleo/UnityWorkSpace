// 改造自 "Skybox/6 Sided"
Shader "XKnight/Scene/MeshSkybox/6 Sided"
{
    Properties
    {
        _Tint ("Tint Color", Color) = (.5, .5, .5, .5)
        [Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
        _Rotation ("Rotation", Range(0, 360)) = 0

        [NoScaleOffset] _FrontTex ("Front [+Z]   (HDR)", 2D) = "grey" {}
        [NoScaleOffset] _BackTex ("Back [-Z]   (HDR)", 2D) = "grey" {}
        [NoScaleOffset] _LeftTex ("Left [+X]   (HDR)", 2D) = "grey" {}
        [NoScaleOffset] _RightTex ("Right [-X]   (HDR)", 2D) = "grey" {}
        [NoScaleOffset] _UpTex ("Up [+Y]   (HDR)", 2D) = "grey" {}
        [NoScaleOffset] _DownTex ("Down [-Y]   (HDR)", 2D) = "grey" {}
    }

    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "PreviewType"="Skybox" }
        
        Cull Off ZWrite Off

        CGINCLUDE
        #include_with_pragmas "../Skybox 6 Sided.hlsl"
        ENDCG

        Pass
        {
            Name "MeshSkybox 6 Sided 1"
            Tags { "LightMode"="MeshSkybox" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            sampler2D _FrontTex;
            half4 _FrontTex_HDR;
            half4 frag(v2f i) : SV_Target { return skybox_frag(i, _FrontTex, _FrontTex_HDR); }
            ENDCG
        }

        Pass
        {
            Name "MeshSkybox 6 Sided 2"
            Tags { "LightMode"="MeshSkybox" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            sampler2D _BackTex;
            half4 _BackTex_HDR;
            half4 frag(v2f i) : SV_Target { return skybox_frag(i, _BackTex, _BackTex_HDR); }
            ENDCG
        }

        Pass
        {
            Name "MeshSkybox 6 Sided 3"
            Tags { "LightMode"="MeshSkybox" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            sampler2D _LeftTex;
            half4 _LeftTex_HDR;
            half4 frag(v2f i) : SV_Target { return skybox_frag(i, _LeftTex, _LeftTex_HDR); }
            ENDCG
        }

        Pass
        {
            Name "MeshSkybox 6 Sided 4"
            Tags { "LightMode"="MeshSkybox" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            sampler2D _RightTex;
            half4 _RightTex_HDR;
            half4 frag(v2f i) : SV_Target { return skybox_frag(i, _RightTex, _RightTex_HDR); }
            ENDCG
        }

        Pass
        {
            Name "MeshSkybox 6 Sided 5"
            Tags { "LightMode"="MeshSkybox" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            sampler2D _UpTex;
            half4 _UpTex_HDR;
            half4 frag(v2f i) : SV_Target { return skybox_frag(i, _UpTex, _UpTex_HDR); }
            ENDCG
        }

        Pass
        {
            Name "MeshSkybox 6 Sided 6"
            Tags { "LightMode"="MeshSkybox" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            sampler2D _DownTex;
            half4 _DownTex_HDR;
            half4 frag(v2f i) : SV_Target { return skybox_frag(i, _DownTex, _DownTex_HDR); }
            ENDCG
        }

        // Always pass 是为了 Inspector 的预览窗准备的，不能用来做正式渲染
        Pass
        {
            Name "MeshSkybox 6 Sided Always 1"
            Tags { "LightMode"="Always" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            sampler2D _FrontTex;
            half4 _FrontTex_HDR;
            half4 frag(v2f i) : SV_Target { return skybox_frag(i, _FrontTex, _FrontTex_HDR); }
            ENDCG
        }

        Pass
        {
            Name "MeshSkybox 6 Sided Always 2"
            Tags { "LightMode"="Always" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            sampler2D _BackTex;
            half4 _BackTex_HDR;
            half4 frag(v2f i) : SV_Target { return skybox_frag(i, _BackTex, _BackTex_HDR); }
            ENDCG
        }

        Pass
        {
            Name "MeshSkybox 6 Sided Always 3"
            Tags { "LightMode"="Always" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            sampler2D _LeftTex;
            half4 _LeftTex_HDR;
            half4 frag(v2f i) : SV_Target { return skybox_frag(i, _LeftTex, _LeftTex_HDR); }
            ENDCG
        }

        Pass
        {
            Name "MeshSkybox 6 Sided Always 4"
            Tags { "LightMode"="Always" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            sampler2D _RightTex;
            half4 _RightTex_HDR;
            half4 frag(v2f i) : SV_Target { return skybox_frag(i, _RightTex, _RightTex_HDR); }
            ENDCG
        }

        Pass
        {
            Name "MeshSkybox 6 Sided Always 5"
            Tags { "LightMode"="Always" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            sampler2D _UpTex;
            half4 _UpTex_HDR;
            half4 frag(v2f i) : SV_Target { return skybox_frag(i, _UpTex, _UpTex_HDR); }
            ENDCG
        }

        Pass
        {
            Name "MeshSkybox 6 Sided Always 6"
            Tags { "LightMode"="Always" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            sampler2D _DownTex;
            half4 _DownTex_HDR;
            half4 frag(v2f i) : SV_Target { return skybox_frag(i, _DownTex, _DownTex_HDR); }
            ENDCG
        }
    }
}