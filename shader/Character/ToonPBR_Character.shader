Shader "XKnight/Character/ToonPBR_Character"
{
    Properties
    {
        // Surface Options
        [HideInInspector] _WorkflowMode ("WorkflowMode", Float) = 0.0
        [HideInInspector] _ShadingModel ("ShaidngModel", Float) = 0.0
        [HideInInspector] _Cull ("__cull", Float) = 2.0
        [HideInInspector] _SurfaceType ("__surface", Float) = 0.0
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _SrcBlendAlpha ("__srcA", Float) = 1
        [HideInInspector] _DstBlendAlpha ("__dstA", Float) = 0

        // Default
        _BaseMap ("基础纹理", 2D) = "white" {}
        _BaseColor ("基础调色", Color) = (1, 1, 1, 1)
        _BumpMap ("法线纹理", 2D) = "bump" {}
        _BumpScale ("法线比例", Range(0, 2)) = 1.0
        _PBRMaskMap ("PBR 遮罩 (r:阴影遮罩175, g:粗糙度, b:高光强度, a:自发光)", 2D) = "white" {}
        [Enum(Alpha,0, Fresnel,1)] _BaseMapA ("基础纹理A通道功能", float) = 0

        // CarToon
        // [NoScaleOffset] _DetailTex ("DetailTex",2D) = "white" {}

        // Emission
        [ToggleOff] _EmissionOn ("EmissionOn", Range(0, 1)) = 0
        [HDR] _EmissionColor ("EmissionColor", Color) = (0, 0, 0)
        _EmissionStrength ("EmissionStrength", Range(0, 2)) = 1

        // 实时阴影
        _ShadowmapReceiveWeight ("实时 Shadowmap 接收权重", Range(0, 1)) = 1
        _ShadowmapTintColor ("实时 Shadowmap 阴影调色", Color) = (1, 1, 1, 1)
        
        // 2阶阴影
        _Shadow1Color ("Shadow1Color", Color) = (0.5, 0.5, 0.5, 0.5)
        _Shadow1Step ("Shadow1 Step", Range(0, 1)) = 0.5
        _Shadow1Feather ("Shadow1 Feather", Range(0, 1)) = 0
        _Shadow2Color ("Shadow2Color", Color) = (0, 0, 0, 0)
        _Shadow2Step ("Shadow1 Step", Range(0, 1)) = 0.3
        _Shadow2Feather ("Shadow1 Feather", Range(0, 1)) = 0
        _DoubleShadeTintStrength ("2阶阴影替换色强度", Range(0, 1)) = 1

        // SDF 阴影
        _SDFShadowMap ("SDF 遮罩图 (R:面部阴影, A:=1 SDF+175 / =0 遮罩SDF)", 2D) = "white" {}
        [MaterialToggle] _SDFFullLight ("SDF 全亮", Range(0, 1)) = 0
        [MaterialToggle] _SDFMeshRenderMode ("未蒙皮的 Mesh 勾选", Range(0, 1)) = 0
        [MaterialToggle] _SDFInvert ("SDF 阴影反方向", Range(0, 1)) = 0
        
        _SDFShadowColor ("SDF 阴影颜色", Color) = (0.5, 0.5, 0.5, 0.5)
        _SDFShadowStep ("SDF 阴影步进", Range(0, 1)) = 0.5
        _SDFShadowFeather ("SDF 阴影羽化", Range(0, 1)) = 0
        
        _SDFMaskShadowColor ("SDF 遮罩区阴影颜色", Color) = (0.7, 0.7, 0.7, 0.5)
        _SDFMaskShadowFeather ("SDF 遮罩区阴影羽化", Range(0, 1)) = 0.2
        
        // 阴影遮罩控制
        _Source175Shadow ("175阴影的源", Float) = 0
        _TintBrightness ("暗部相对亮部明度", Range(0, 1)) = 0.5
        _TintShadowFactor ("顶点色阴影衰减", Range(0, 1)) = 1

        // 卡通头帘投影
        [HDR]_HairShadowColor ("卡通头帘投影 - 阴影颜色", Color) = (0, 0, 0, 0)
        _HairShadowOffsetX ("卡通头帘投影 - 采样偏移 - X", Range(-5, 5)) = 0
        _HairShadowOffsetY ("卡通头帘投影 - 采样偏移 - Y", Range(-20, 20)) = 0
        _HairShadowOffsetZ ("卡通头帘投影 - 采样偏移 - Z", Range(-1, 1)) = 0
        [ToggleOff] _HairShadowFixedStencil ("卡通头帘投影 - GUI 根据这个标记来固定模板设置", Range(0, 1)) = 1

        // 高光着色模式
        _SpecularShadingMode ("SpecularShadingMode", Float) = 0

        // 卡通高光 - 这里固定了高光工作流模式，所以金属性是废的
        _Metallic ("Metallic", Range(0.0, 5.0)) = 0.5
        [HDR] _SpecColor ("Specular", Color) = (0.2, 0.2, 0.2)

        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _SpecularStep ("SpecularStep", Range(0, 1)) = 0.5
        _SpecularFeather ("SpecularFeather", Range(0, 1)) = 0
        _FloodlightIntensity ("泛光强度", Range(0, 1)) = 0

        // 卡吉亚各向异性头发高光
        _AnisoShiftMap ("各向异性高光 - 细节纹理", 2D) = "white" {}
        _AnisoShiftScaleX ("各向异性高光 - UV偏移 - X", Range(1, 50)) = 1
        _AnisoShiftScaleY ("各向异性高光 - UV偏移 - Y", Range(1, 50)) = 1

        _AnisoSpecularColor ("各向异性高光 - 第1层 - 高光颜色", Color) = (1, 0.8, 0.63, 1)
        _AnisoSpread1 ("各向异性高光 - 第1层 - 扩散", Range(-1, 1)) = 0.25
        _AnsioSpeularShift ("各向异性高光 - 第1层 - 偏移", Range(-3, 3)) = 0.05
        _AnsioSpeularStrength ("各向异性高光 - 第1层 - 强度", Range(0, 64)) = 30
        _AnsioSpeularExponent ("各向异性高光 - 第1层 - 反射指数", Range(1, 1024)) = 275

        _AnisoSecondarySpecularColor ("各向异性高光 - 第2层 - 高光颜色", Color) = (0.87, 0.73, 0.62, 1)
        _AnisoSpread2 ("各向异性高光 - 第2层 - 扩散", Range(-1, 1)) = 0.17
        _AnsioSecondarySpeularShift ("各向异性高光 - 第2层 - 偏移", Range(-3, 3)) = 0.25
        _AnsioSecondarySpeularStrength ("各向异性高光 - 第2层 - 强度", Range(0, 64)) = 32
        _AnsioSecondarySpeularExponent ("各向异性高光 - 第2层 - 反射指数", Range(1, 1024)) = 151

        // Ramp
        [HideInInspector] _ShadowType ("ShadowType", Float) = 0.0
        _DiffuseRampMap ("DiffuseRampMap", 2D) = "white" {}
        _DiffuseRampIntensity ("DiffuseRampIntensity", Range(0.0, 1)) = 1.0
        [HDR] _DiffuseRampColor ("DiffuseRampColor", Color) = (1, 1, 1, 1)
        _DiffuseRampMapVertical ("Ramp 垂直索引", Range(0, 1)) = 0.5
        _DiffuseRampSaturate ("Ramp 饱和度", Range(-1, 1)) = 0

        // Diffuse Offset
        _DarkColorSmooth ("暗部颜色平滑", Range(0, 1)) = 0.05
        _ShadowColorSmooth ("阴影颜色平滑", Range(0, 1)) = 0.05
        _LightColorSmooth ("高光颜色平滑", Range(0, 1)) = 0.05
        _ShadowColorDiffuse ("暗部颜色渐变", Range(0, 1)) = 0
        _LightColorHSV_S ("高光颜色饱和度", Range(0, 1)) = 0.5
        _LightColorHSV_V ("高光颜色亮度", Range(0, 1)) = 0.1
        _DiffuseOcclusion ("AO强度", Range(0, 1)) = 0
        [Enum(Diffuse,0, Specular,1)] _LightColorMode ("高光变化模式", float) = 0
        _LightColorRangeMin ("边缘光最小范围", Range(0, 1)) = 0.25
        _LightColorRangeMax ("边缘光最大范围", Range(0, 1)) = 0.75


        // 深度边缘光
        [ToggleOff] _EnableRim ("开启深度边缘光", Range(0, 1)) = 0.0
        [HDR] _FrontRimColor ("前向颜色",Color) = (1, 1, 1, 1)
        [HDR] _BackRimColor ("背面颜色", Color) = (1, 1, 1, 1)
        
        _RimWidth ("边缘光宽度", Range(0, 1)) = 0.07
        _RimDepthCutOff ("深度截断值", float) = 0.05
        
        _RimControlMask ("局部控制遮罩", 2D) = "black" {}
        _RimWidth2 ("遮罩中的 边缘光宽度", Range(0, 1)) = 0.07
        _RimDepthCutOff2 ("遮罩中的 深度截断值", float) = 0.01

        //subsurface scattering
        [ToggleOff] _KnightToonScattingSurface ("EmissionOn", Range(0, 1)) = 0
        [HideInInspector] _DiffusionParameter_Asset("Diffusion Parameter", Vector) = (0, 0, 0, 0)
        _ScattingThicknessMap("厚度贴图", 2D) = "Black" {}
        [ToggleOn]_IsSingleChanelThicknessMap("是否是单通道厚度贴图", Range(0, 1)) = 0
        [DiffusionParameter]_DiffusionParameter("Diffusion Parameter Hash", Float) = 0
        // Outline
        [ToggleOff] _EnableOutline ("Enable Outline", Range(0, 1)) = 1.0
        [ToggleOff] _MiOutline ("Mi Outline", Range(0, 1)) = 0.0
        _MeshPreview ("Mesh Preview", Float) = 0
        
        [HDR] _OutlineColor ("OutlineColor", Color) = (0, 0, 0, 0)
        
        [MaterialToggle] _EnableLocalOutlineColor ("Enable OutlineLocalColor", Float) = 0
        [HDR] _OutlineLocalColor ("OutlineLocalColor", Color) = (0, 0, 0, 0)
        
        _OutlineWidth ("OutlineWidth", Range(0.0, 15.0)) = 5.0
        _OutlinePower ("OutlinePower", Range(0.1, 1.5)) = 0.6
        _OutlineFadeStart ("描边渐隐 - 开始距离", float) = 0
        _OutlineFadeEnd ("描边渐隐 - 结束距离", float) = 50

        // 溶解
        _DissolveType ("溶解类型", Float) = 0

        _DissolveEdgeOn ("溶解边缘开关", Int) = 0
        _EdgeWidth ("溶解边缘宽度", Range(0.0001, 2)) = 0.1
        [HDR] _EdgeColor1 ("溶解边缘颜色1", Color) = (1, 0, 0, 1)
        [HDR] _EdgeColor2 ("溶解边缘颜色2", Color) = (0, 1, 0, 1)

        _DissolveTex ("溶解渐变遮罩", 2D) = "white" {}
        _DissolveTex_Channel ("溶解渐变遮罩 - 通道", Vector) = (1, 0, 0, 0)
        _DissolveFadingMin ("溶解渐变最小值", Range(0.0001, 1)) = 0
        _DissolveFadingMax ("溶解渐变最大值", Range(0.0001, 1)) = 0.2

        _DissolveCutoff ("溶解 Cutoff", Range(-1, 1)) = 0.5
        _DissolveCutoffMultiplier ("溶解 Cutoff 的乘数", float) = 1

        _DissolveDir ("溶解方向（世界空间）", Vector) = (0, 1, 0)

        _DissolveMaskTex ("遮罩溶解", 2D) = "white" {}
        _DissolveMaskTex_Channel ("遮罩溶解 - 通道", Vector) = (1, 0, 0, 0)
        _DissolveMaskReverse ("遮罩取反", Float) = 0

        // 抖动
        _DitherIntensity ("抖动强度", Range(0, 1)) = 0
        _DitherSize ("抖动尺寸", Float) = 1
        _DitherAlpha ("抖动 Alpha", Range(0, 1)) = 1
        
        _DitherWithMatrix ("抖动矩阵", Int) = 0
        _DitherTexture ("抖动图", 2D) = "black" {}
        
        // 菲涅尔边缘光（整体）
        [MaterialToggle] _RGOn ("开启菲涅尔边缘光（整体）", Range(0, 1)) = 0
        
        [HDR] _RGColor ("颜色", Color) = (1, 1, 1, 1)
        
        _RGScale ("整体调节 - 强度", Range(0, 1)) = 1
        _RGBias ("整体调节 - 偏移", Range(-1, 1)) = 0
        _RGShininess ("整体调节 - 锐度/集中度", Range(1, 20)) = 5
        _RGFeather ("整体调节 - 羽化", Range(0, 1)) = 1
        _RGMixVertexNormal ("整体调节 - 混合顶点法线", Range(0, 1)) = 0
        
        _RGDiffuseBlend ("背光面调节 - 混合", Range(0, 1)) = 0
        _RGDiffuseStep ("背光面调节 - 阈值", Range(0, 1)) = 0.6
        _RGDiffuseFeather ("背光面调节 - 羽化", Range(0, 1)) = 0.15
        
        _RGSoftFresnelMix ("软菲涅尔 - 混合", Range(0, 1)) = 0
        _RGSoftFresnelParameters ("软菲涅尔 - 参数 - 软的范围倍数, 软的最小范围, 负偏移系数, 指数(使用 <1 的指数软化边缘)", Vector) = (2.5, 0.15, 0.5, 0.8)
        
        // 菲涅尔边缘光（局部）
        [MaterialToggle] _Local_RGOn ("开启菲涅尔边缘光（局部）", Range(0, 1)) = 0
        
        [HDR] _Local_RGColor ("颜色", Color) = (1, 1, 1, 1)
        
        _Local_RGScale ("整体调节 - 强度", Range(0, 1)) = 1
        _Local_RGBias ("整体调节 - 偏移", Range(-1, 1)) = 0
        _Local_RGShininess ("整体调节 - 锐度/集中度", Range(1, 20)) = 5
        _Local_RGFeather ("整体调节 - 羽化", Range(0, 1)) = 1
        _Local_RGMixVertexNormal ("整体调节 - 混合顶点法线", Range(0, 1)) = 0
        
        _Local_RGDiffuseBlend ("背光面调节 - 混合", Range(0, 1)) = 0
        _Local_RGDiffuseStep ("背光面调节 - 阈值", Range(0, 1)) = 0.6
        _Local_RGDiffuseFeather ("背光面调节 - 羽化", Range(0, 1)) = 0.15
        
        _Local_RGSoftFresnelMix ("软菲涅尔 - 混合", Range(0, 1)) = 0
        _Local_RGSoftFresnelParameters ("软菲涅尔 - 参数 - 软的范围倍数, 软的最小范围, 负偏移系数, 指数(使用 <1 的指数软化边缘)", Vector) = (2.5, 0.15, 0.5, 0.8)
        
        // Eye
        _PupilSize ("Pupil Size", Range(0.0, 1.0)) = 0.3
        _PupilSunken ("Pupil Sunken", Range(-0.2, 0.2)) = -0.1
        _PupilMatcap ("Pupil Matcap", 2D) = "white" {}
        _PupilMatcapIntensity ("Pupil Matcap Intensity", Range(0.0, 2.0)) = 1.0

        // 顶点拉扯
        [ToggleOff] _EnableVertexPull ("启用顶点拉扯", Range(0, 1)) = 0
        _VertexPullNoiseTexture ("拉扯噪声", 2D) = "black" {}
        _VertexPullDirection ("拉扯方向", Vector) = (0,0,-1,0)
        _VertexPullIntensity ("拉扯强度", Float) = 1
        
        // FOV
        [HideInInspector] _FOV_PivotWS ("角色枢轴世界坐标", Vector) = (0,0,0,0)
        [HideInInspector] _FOV_Parameters ("透视压扁参数 (x=脚本开关, y=压扁目标, z=形体补偿, w=1)", Vector) = (0,0,0,0)

        _TimelineMainLightIntensity ("用于大招时压暗角色亮度", Range(0, 5)) = 1.0

        // 角色 buff 叠加效果的 GUI 控制，测试属性
        [ToggleOff] _EnablePass_buff_frost ("启动霜冻 pass", Range(0, 1)) = 0
        [ToggleOff] _EnablePass_buff_frost_big ("启动霜冻 pass （大怪用）", Range(0, 1)) = 0

        // 霜冻效果
        [Toggle(_FROST_ON)] _FrostOn ("启用霜冻", float) = 0

        _FrostTint ("霜冻调色", Color) = (1, 1, 1, 0)
        _FrostTexture ("霜冻纹理", 2D) = "white" {}

        _FrostBumpMap ("霜冻法线", 2D) = "bump" {}
        _FrostBumpScale ("霜冻法线强度", Range( 0, 10 )) = 1

        _IcicleMask ("冰柱遮罩", 2D) = "white" {}
        _IcicleMaskTile ("冰柱遮罩的 Tiling 值", Range( 0, 1 )) = 0.5

        [Toggle(_ICE_OVERLAY_MASK_ON)] _IceOverlayMaskOn ("启用冰的覆盖遮罩", float) = 0
        _IceOverlayMask ("冰的覆盖遮罩", 2D) = "white" {}

        _IceSlider ("冰的总强度", Range( 0, 1 )) = 1
        _IceAmount ("冰量", Range( 0, 1 )) = 0
        _YMaskTop ("y轴遮罩 - 上半部分系数", Range( 0, 0.5 )) = 0.03
        _YMaskDown ("y轴遮罩 - 下半部分系数", Range( -0.5, 0 )) = -0.3
        _IcicleLength ("冰柱长度", Range( 0, 1 )) = 0
        _yIceMultiplier ("y轴冰柱倍增器", float) = 8

        _FrostEmissionFresnelIntensity ("霜冻自发光菲涅尔效应的强度", float) = 3
        _FrostEmissionFresnelPow ("霜冻自发光菲涅尔效果的幂值", float) = 2.5

        // 光传输
        [Toggle(_TRANSMISSION_LIGHT_ON)] _TransmissionLightOn ("启用光传输", float) = 0
        _TransmissionShadow ("传输阴影", Range( 0, 1 )) = 0.5

        // SSS半透明
        _TransStrength ("SSS - 强度", Range( 0, 50 )) = 1
        _TransNormal ("SSS - 法线失真", Range( 0, 1 )) = 0.5
        _TransScattering ("SSS - 散射", Range( 1, 50 )) = 2
        _TransDirect ("SSS - 直接的", Range( 0, 1 )) = 0.9
        _TransAmbient ("SSS - 环境", Range( 0, 1 )) = 0.1
        _TransShadow ("SSS - 阴影", Range( 0, 1 )) = 0.5

        // 镶嵌
        _TessValue ("最大镶嵌", Range( 1, 32 ) ) = 4
        _TessMin ("镶嵌的最小距离", Float ) = 1
        _TessMax ("镶嵌的最大距离", Float ) = 10

        // Advanced Options
        [ToggleOff] _EnvironmentReflections ("Environment Reflections （这个暂时应该是废的）", Range(0, 1)) = 0.0
        _EnvReflectStrength ("烘焙 GI 比例（漫反射 + 镜面高光）", Range(0, 1)) = 1.0

        _BloomFactor ("Bloom系数", Range( 0, 1 )) = 0.0
        _WaterColorOn ("水彩开关", Range(0, 1)) = 0.0
        _SceneSpaceOutlineOn ("屏幕空间描边开关", Range(0, 1)) = 1.0
        
        _WriteDepthNormals_On ("写入深度法线", Float) = 0.0

        // 模板缓冲
        [Toggle] _StencilActive ("启用Stencil", float) = 0
        [IntRange] _StencilWriteMask ("Stencil Write Mask", Range(0, 255)) = 255
        [IntRange] _StencilReadMask ("Stencil Read Mask", Range(0, 255)) = 255
        [IntRange] _Stencil ("Stencil ID", Range(0, 255)) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Stencil Pass", Float) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilFail ("Stencil Fail", Float) = 0
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilZFail ("Stencil ZFail", Float) = 0

        //_Character_LUT_Map ("角色 LUT 图", 2D) = "white" {}
        //_Character_LUT_Params ("角色 LUT 参数", Vector) = (0, 0, 0, 0)

        // 屏幕效果ID：0=关闭，1..8=效果
        _ScreenEffMaskID ("Screen Effect Mask ID", Float) = 0

        // 战斗描边配套的菲涅尔边缘光
        [Toggle(_COMBAT_SURFACE_GLOW_ON)] _CombatSurfaceGlowOn ("战斗fernel开关", float) = 0
        [HDR] _CombatSurfaceGlowColorInner ("战斗fernel内部颜色", Color) = (1.1, 0.55, 0.18, 1)
        [HDR] _CombatSurfaceGlowColorMid ("战斗fernel中间颜色", Color) = (2.6, 1.1, 0.32, 1)
        [HDR] _CombatSurfaceGlowColorOuter ("战斗fernel外部颜色", Color) = (4.0, 1.8, 0.55, 1)
        _CombatSurfaceGlowIntensity ("战斗fernel强度", Range(0, 8)) = 0.57
        // (bandStart, bandEnd, bandPower, preserveShading)
        _CombatSurfaceGlowBand ("Combat Surface Glow Band", Vector) = (0.0, 0.85, 1.6, 0.35)
        _CombatSurfaceGlowBreakupTex ("战斗fernel噪声贴图", 2D) = "gray" {}
        _CombatSurfaceGlowUseBreakupTex ("战斗fernel噪声贴图是否使用", float) = 1
        // (worldScale, amount, threshold, feather) 
        _CombatSurfaceGlowBreakupParams ("战斗fernel噪声参数", Vector) = (1, 0.41, 0.88, 0.16)
        // (fillWeight, softStart(1-NdotV), softEnd(1-NdotV), softPower)
        _CombatSurfaceGlowFill ("Combat Surface Glow Fill", Vector) = (0.2, 0.0, 1.0, 0.45)
    }
    
    // LOD 510
    /*
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "RenderType" = "Opaque"
        }
        LOD 510

        // ForwardLit
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Pass [_StencilPass]
                Fail [_StencilFail]
                ZFail [_StencilZFail]
            }
            
            Cull [_Cull]
            
            ColorMask RGBA
            Blend 0 [_SrcBlend] [_DstBlend]
            Blend 1 One Zero
            Blend 2 One Zero

            HLSLPROGRAM
            // #pragma enable_d3d11_debug_symbols
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 4.6
            #pragma vertex ToonForwardPassVertex
            #pragma fragment ToonForwardPassFragment

            // -------------------------------------
            // Pipeline keywords
            //#pragma multi_compile _ _TIMELINE_FULL_LIGHT_ON
            #pragma shader_feature _ _DITHER_ON

            #pragma multi_compile _ _GLOBAL_OVERRIDE_CHARACTER_LIGHTIING_ON
            #pragma multi_compile _ _HAS_CAMERA_CHARACTER_DEPTH_TEXTURE

            // #pragma multi_compile _ _KAJIYAHAIR // 卡吉亚各向异性头发高光

            #pragma multi_compile_fragment _ _MRT_BUFFER

            // 雾效
            #pragma multi_compile _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY
            
            // -------------------------------------
            // Material Keywords
            // #pragma shader_feature_local_vertex _ _VERTEX_PULL_ON
            #pragma shader_feature_local_fragment _ _SDFSHADOWMAP _RAMP_MODE_ON _ILM_SHADOW_MASK_ON _DIFFUSE_OFFSET
            #pragma shader_feature_local_fragment _ _EMISSION
            #pragma shader_feature_local_fragment _ _EYE_MODE
            #pragma shader_feature_local_fragment _ _RIM_ON
            // #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS

            // for vfx
            #pragma shader_feature_local_fragment _ _COMBAT_SURFACE_GLOW_ON
            #pragma shader_feature_local_fragment _ _RG_EFFECT_ON
            // 用法是默认在材质球勾选，所以可以用feature方式
            #pragma shader_feature_local _ _RANDOM_DISSOLVE_ON _DIRECTION_DISSOLVE_ON _MASK_DISSOLVE_ON
            #pragma shader_feature_local_fragment _ _SDF_MESH_RENDER_MODE

            // 因固定工作流而设死的开关宏
            #define _SPECULAR_SETUP 1 // 高光工作流
            // 卡通头发阴影
            //#define _HAS_CEL_HAIR_SHADOW_V1 1

            // 冰霜效果
            //#pragma shader_feature_local _ _FROST_ON
            //#pragma shader_feature_local _ _ICE_OVERLAY_MASK_ON
            //#pragma shader_feature_local _ _TRANSMISSION_LIGHT_ON

            // 镶嵌
            /*
            #pragma require tessellation tessHW
            #pragma hull HullFunction
            #pragma domain DomainFunction
            #define ASE_TESSELLATION 1 // 镶嵌开关
            #define ASE_DISTANCE_TESSELLATION // 距离镶嵌
            */

            #include "../ShaderLibrary/Lighting.hlsl"
            
            #include "./Include/ToonPBR_Input.hlsl"
            
            #include "./Include/ToonPBR_VertexPull.hlsl"
            #include "./Include/ToonPBR_Core.hlsl"
            #include "./Include/ToonPBR_Specular.hlsl"
            #include "./Include/ToonPBR_Diffuse.hlsl"
            #include "./Include/ToonPBR_Rim.hlsl"
            #include "./Include/ToonPBR_Fresnel.hlsl"
            #include "./Include/ToonPBR_Eye.hlsl"
            #include "./Include/ToonPBR_Lighting.hlsl"

            #include "./Include/ToonPBR_Forward.hlsl"
            ENDHLSL
        }

        UsePass "XKnight/Character/ToonPBR_Character/OUTLINE"
        UsePass "XKnight/Character/ToonPBR_Character/OUTLINEMOTIONVECTORS"
        UsePass "XKnight/Character/ToonPBR_Character/SHADOWCASTER"
        UsePass "XKnight/Character/ToonPBR_Character/DEPTHONLY"
        
        UsePass "XKnight/Character/ToonPBR_Character/BUFF_FROST"
        UsePass "XKnight/Character/ToonPBR_Character/BUFF_FROST_BIG"

        UsePass "XKnight/Character/ToonPBR_Character/HAIRSHADOWV2MARK"
        UsePass "XKnight/Character/ToonPBR_Character/HAIRSHADOWV2DRAW"
    }
    */
    
    // LOD 500
    /*
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "RenderType" = "Opaque"
        }
        LOD 500

        // ForwardLit
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Pass [_StencilPass]
                Fail [_StencilFail]
                ZFail [_StencilZFail]
            }
            
            Cull [_Cull]

            ColorMask RGBA
            Blend 0 [_SrcBlend] [_DstBlend]
            Blend 1 One Zero
            Blend 2 One Zero

            HLSLPROGRAM
            // #pragma enable_d3d11_debug_symbols
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma vertex ToonForwardPassVertex
            #pragma fragment ToonForwardPassFragment

            // -------------------------------------
            // Pipeline keywords
            //#pragma multi_compile _ _TIMELINE_FULL_LIGHT_ON
            #pragma shader_feature _ _DITHER_ON

            #pragma multi_compile _ _GLOBAL_OVERRIDE_CHARACTER_LIGHTIING_ON
            #pragma multi_compile _ _HAS_CAMERA_CHARACTER_DEPTH_TEXTURE

            // #pragma multi_compile _ _KAJIYAHAIR // 卡吉亚各向异性头发高光

            #pragma multi_compile_fragment _ _MRT_BUFFER

            // 雾效
            #pragma multi_compile _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY
            
            // -------------------------------------
            // Material Keywords
            // #pragma shader_feature_local_vertex _ _VERTEX_PULL_ON
            #pragma shader_feature_local_fragment _ _SDFSHADOWMAP _RAMP_MODE_ON _ILM_SHADOW_MASK_ON _DIFFUSE_OFFSET
            #pragma shader_feature_local_fragment _ _EMISSION
            #pragma shader_feature_local_fragment _ _EYE_MODE
            #pragma shader_feature_local_fragment _ _RIM_ON
            // #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS

            // for vfx
            #pragma shader_feature_local_fragment _ _COMBAT_SURFACE_GLOW_ON
            #pragma shader_feature_local_fragment _ _RG_EFFECT_ON
            // 用法是默认在材质球勾选，所以可以用feature方式
            #pragma shader_feature_local _ _RANDOM_DISSOLVE_ON _DIRECTION_DISSOLVE_ON _MASK_DISSOLVE_ON
            #pragma shader_feature_local_fragment _ _SDF_MESH_RENDER_MODE

            // 因固定工作流而设死的开关宏
            #define _SPECULAR_SETUP 1 // 高光工作流
            // 卡通头发阴影
            //#define _HAS_CEL_HAIR_SHADOW_V1 1

            // 冰霜效果
            //#pragma shader_feature_local _ _FROST_ON
            //#pragma shader_feature_local _ _ICE_OVERLAY_MASK_ON
            //#pragma shader_feature_local _ _TRANSMISSION_LIGHT_ON

            #include "../ShaderLibrary/Lighting.hlsl"
            
            #include "./Include/ToonPBR_Input.hlsl"
            
            #include "./Include/ToonPBR_VertexPull.hlsl"
            #include "./Include/ToonPBR_Core.hlsl"
            #include "./Include/ToonPBR_Specular.hlsl"
            #include "./Include/ToonPBR_Diffuse.hlsl"
            #include "./Include/ToonPBR_Rim.hlsl"
            #include "./Include/ToonPBR_Fresnel.hlsl"
            #include "./Include/ToonPBR_Eye.hlsl"
            #include "./Include/ToonPBR_Lighting.hlsl"

            #include "./Include/ToonPBR_Forward.hlsl"
            ENDHLSL
        }

        UsePass "XKnight/Character/ToonPBR_Character/OUTLINE"
        UsePass "XKnight/Character/ToonPBR_Character/OUTLINEMOTIONVECTORS"
        UsePass "XKnight/Character/ToonPBR_Character/SHADOWCASTER"
        UsePass "XKnight/Character/ToonPBR_Character/DEPTHONLY"
        
        UsePass "XKnight/Character/ToonPBR_Character/BUFF_FROST"
        UsePass "XKnight/Character/ToonPBR_Character/BUFF_FROST_BIG"

        UsePass "XKnight/Character/ToonPBR_Character/HAIRSHADOWV2MARK"
        UsePass "XKnight/Character/ToonPBR_Character/HAIRSHADOWV2DRAW"
    }
    */
    
    // LOD 410
    /*
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "RenderType" = "Opaque"
        }
        LOD 410

        // ForwardLit
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Pass [_StencilPass]
                Fail [_StencilFail]
                ZFail [_StencilZFail]
            }

            Cull [_Cull]
            
            ColorMask RGBA
            Blend [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            // #pragma enable_d3d11_debug_symbols
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 4.6
            #pragma vertex ToonForwardPassVertex
            #pragma fragment ToonForwardPassFragment

            // -------------------------------------
            // Pipeline keywords
            //#pragma multi_compile _ _TIMELINE_FULL_LIGHT_ON
            #pragma shader_feature _ _DITHER_ON

            #pragma multi_compile _ _GLOBAL_OVERRIDE_CHARACTER_LIGHTIING_ON
            #pragma multi_compile _ _HAS_CAMERA_CHARACTER_DEPTH_TEXTURE

            // #pragma multi_compile _ _KAJIYAHAIR // 卡吉亚各向异性头发高光

            // 雾效
            #pragma multi_compile _ _HEIGHT_FOG
            #pragma shader_feature _RECORDING_QUALITY
            
            // -------------------------------------
            // Material Keywords
            // #pragma shader_feature_local_vertex _ _VERTEX_PULL_ON
            #pragma shader_feature_local_fragment _ _SDFSHADOWMAP _RAMP_MODE_ON _ILM_SHADOW_MASK_ON _DIFFUSE_OFFSET
            #pragma shader_feature_local_fragment _ _EMISSION
            #pragma shader_feature_local_fragment _ _EYE_MODE
            #pragma shader_feature_local_fragment _ _RIM_ON
            // #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS

            // for vfx
            #pragma shader_feature_local_fragment _ _COMBAT_SURFACE_GLOW_ON
            #pragma shader_feature_local_fragment _ _RG_EFFECT_ON
            // 用法是默认在材质球勾选，所以可以用feature方式
            #pragma shader_feature_local _ _RANDOM_DISSOLVE_ON _DIRECTION_DISSOLVE_ON _MASK_DISSOLVE_ON
            #pragma shader_feature_local_fragment _ _SDF_MESH_RENDER_MODE

            // 因固定工作流而设死的开关宏
            #define _SPECULAR_SETUP 1 // 高光工作流
            // 卡通头发阴影
            //#define _HAS_CEL_HAIR_SHADOW_V1 1

            // 冰霜效果
            //#pragma shader_feature_local _ _FROST_ON
            //#pragma shader_feature_local _ _ICE_OVERLAY_MASK_ON
            //#pragma shader_feature_local _ _TRANSMISSION_LIGHT_ON

            // 镶嵌
            /*
            #pragma require tessellation tessHW
            #pragma hull HullFunction
            #pragma domain DomainFunction
            #define ASE_TESSELLATION 1 // 镶嵌开关
            #define ASE_DISTANCE_TESSELLATION // 距离镶嵌
            */

            // 导入代码
            #include "../ShaderLibrary/Lighting.hlsl"
            
            #include "./Include/ToonPBR_Input.hlsl"
            
            #include "./Include/ToonPBR_VertexPull.hlsl"
            #include "./Include/ToonPBR_Core.hlsl"
            #include "./Include/ToonPBR_Specular.hlsl"
            #include "./Include/ToonPBR_Diffuse.hlsl"
            #include "./Include/ToonPBR_Rim.hlsl"
            #include "./Include/ToonPBR_Fresnel.hlsl"
            #include "./Include/ToonPBR_Eye.hlsl"
            #include "./Include/ToonPBR_Lighting.hlsl"

            #include "./Include/ToonPBR_Forward.hlsl"
            ENDHLSL
        }

        UsePass "XKnight/Character/ToonPBR_Character/OUTLINE"
        UsePass "XKnight/Character/ToonPBR_Character/OUTLINEMOTIONVECTORS"
        UsePass "XKnight/Character/ToonPBR_Character/SHADOWCASTER"
        UsePass "XKnight/Character/ToonPBR_Character/DEPTHONLY"
        
        UsePass "XKnight/Character/ToonPBR_Character/DEPTHNORMALS"
        UsePass "XKnight/Character/ToonPBR_Character/DEPTHMASK"
        UsePass "XKnight/Character/ToonPBR_Character/VIEWSPACENORMALS"
        
        UsePass "XKnight/Character/ToonPBR_Character/BUFF_FROST"
        UsePass "XKnight/Character/ToonPBR_Character/BUFF_FROST_BIG"

        UsePass "XKnight/Character/ToonPBR_Character/HAIRSHADOWV2MARK"
        UsePass "XKnight/Character/ToonPBR_Character/HAIRSHADOWV2DRAW"
    }
    */

    // LOD 400
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "RenderType" = "Opaque"
        }
        LOD 400

        // ForwardLit
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Pass [_StencilPass]
                Fail [_StencilFail]
                ZFail [_StencilZFail]
            }

            Cull [_Cull]
            
            ColorMask RGBA
            Blend [_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]

            HLSLPROGRAM
            // #pragma enable_d3d11_debug_symbols
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma vertex ToonForwardPassVertex
            #pragma fragment ToonForwardPassFragment

            // -------------------------------------
            // Unity defined keywords
            
            // -------------------------------------
            // Pipeline keywords
            //#pragma multi_compile _ _TIMELINE_FULL_LIGHT_ON
            #pragma multi_compile _ _GLOBAL_OVERRIDE_CHARACTER_LIGHTIING_ON
            #pragma multi_compile _ _HAS_CAMERA_CHARACTER_DEPTH_TEXTURE

            // #pragma multi_compile _ _KAJIYAHAIR // 卡吉亚各向异性头发高光
            
            #pragma multi_compile _ _HEIGHT_FOG // 雾效
            #pragma shader_feature _ _RECORDING_QUALITY // 录制画质

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_vertex _ _VERTEX_PULL_ON
            #pragma shader_feature_local_fragment _ _SDFSHADOWMAP _RAMP_MODE_ON _ILM_SHADOW_MASK_ON _DIFFUSE_OFFSET
            #pragma shader_feature_local_fragment _ _EMISSION
            #pragma shader_feature_local_fragment _ _EYE_MODE
            #pragma shader_feature_local_fragment _ _RIM_ON
            
            #pragma shader_feature_local_fragment _ _COMBAT_SURFACE_GLOW_ON
            #pragma shader_feature_local_fragment _ _RG_EFFECT_ON
            #pragma shader_feature_local_fragment _ _RG_EFFECT_LOCAL_ON
            // 用法是默认在材质球勾选，所以可以用feature方式
            #pragma shader_feature_local _ _RANDOM_DISSOLVE_ON _DIRECTION_DISSOLVE_ON _MASK_DISSOLVE_ON
            #pragma shader_feature_local_fragment _ _SDF_MESH_RENDER_MODE

            #pragma multi_compile_local_fragment _ _DITHER_ON

            // 因固定工作流而设死的开关宏
            #define _SPECULAR_SETUP 1 // 高光工作流
            // 卡通头发阴影
            //#define _HAS_CEL_HAIR_SHADOW_V1 1

            // 冰霜效果
            //#pragma shader_feature_local _ _FROST_ON
            //#pragma shader_feature_local _ _ICE_OVERLAY_MASK_ON
            //#pragma shader_feature_local _ _TRANSMISSION_LIGHT_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            // 导入 hlsl
            #include "./Include/Generated/ToonPBR_AutoDefines.hlsl"
            
            #include "../ShaderLibrary/Lighting.hlsl"
            
            #include "./Include/ToonPBR_Input.hlsl"
            
            #include "./Include/ToonPBR_VertexPull.hlsl"
            #include "./Include/ToonPBR_Core.hlsl"
            #include "./Include/ToonPBR_Specular.hlsl"
            #include "./Include/ToonPBR_Diffuse.hlsl"
            #include "./Include/ToonPBR_Rim.hlsl"
            #include "./Include/ToonPBR_Fresnel.hlsl"
            #include "./Include/ToonPBR_Eye.hlsl"
            #include "./Include/ToonPBR_Lighting.hlsl"
            #include "./Include/ToonPBR_Forward.hlsl"
            ENDHLSL
        }
        
        UsePass "XKnight/Character/ToonPBR_Character/OUTLINE"
        UsePass "XKnight/Character/ToonPBR_Character/MOTIONVECTORS"
        UsePass "XKnight/Character/ToonPBR_Character/MOTIONVECTORSOUTLINE"
        UsePass "XKnight/Character/ToonPBR_Character/SHADOWCASTER"
        UsePass "XKnight/Character/ToonPBR_Character/DEPTHONLY"
        
        UsePass "XKnight/Character/ToonPBR_Character/DEPTHNORMALS"
        UsePass "XKnight/Character/ToonPBR_Character/DEPTHMASK"
        UsePass "XKnight/Character/ToonPBR_Character/VIEWSPACENORMALS"
        
        UsePass "XKnight/Character/ToonPBR_Character/BUFF_FROST"
        UsePass "XKnight/Character/ToonPBR_Character/BUFF_FROST_BIG"
        
        UsePass "XKnight/Character/ToonPBR_Character/HAIRSHADOWV2MARK"
        UsePass "XKnight/Character/ToonPBR_Character/HAIRSHADOWV2DRAW"
        UsePass "XKnight/Character/ToonPBR_Character/CHARACTERMASK"
        UsePass "XKnight/Character/ToonPBR_Character/SCREENEFFMASK"
        UsePass "Hidden/XKNight/SubsurfaceDiffuse/SUBSURFACEDIFFUSE"
    }

    // LOD 300
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "RenderType" = "Opaque"
        }
        LOD 300
        
        // ForwardLit
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Pass [_StencilPass]
                Fail [_StencilFail]
                ZFail [_StencilZFail]
            }
            
            Cull [_Cull]
            
            ColorMask RGBA
            Blend [_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]

            HLSLPROGRAM
            // #pragma enable_d3d11_debug_symbols
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma vertex ToonForwardPassVertex
            #pragma fragment ToonForwardPassFragment
            
            // -------------------------------------
            // Unity defined keywords

            // -------------------------------------
            // Pipeline keywords
            //#pragma multi_compile _ _TIMELINE_FULL_LIGHT_ON
            #pragma multi_compile _ _GLOBAL_OVERRIDE_CHARACTER_LIGHTIING_ON
            #pragma multi_compile _ _HAS_CAMERA_CHARACTER_DEPTH_TEXTURE
            
            #pragma multi_compile _ _HEIGHT_FOG // 雾效
            #pragma shader_feature _ _RECORDING_QUALITY // 录制画质

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_vertex _ _VERTEX_PULL_ON
            #pragma shader_feature_local_fragment _ _SDFSHADOWMAP _RAMP_MODE_ON _ILM_SHADOW_MASK_ON _DIFFUSE_OFFSET
            #pragma shader_feature_local_fragment _ _EMISSION
            #pragma shader_feature_local_fragment _ _EYE_MODE
            #pragma shader_feature_local_fragment _ _RIM_ON
            
            #pragma shader_feature_local_fragment _ _COMBAT_SURFACE_GLOW_ON
            #pragma shader_feature_local_fragment _ _RG_EFFECT_ON
            #pragma shader_feature_local_fragment _ _RG_EFFECT_LOCAL_ON
            // 用法是默认在材质球勾选，所以可以用feature方式
            #pragma shader_feature_local _ _RANDOM_DISSOLVE_ON _DIRECTION_DISSOLVE_ON _MASK_DISSOLVE_ON
            #pragma shader_feature_local_fragment _ _SDF_MESH_RENDER_MODE

            #pragma multi_compile_local_fragment _ _DITHER_ON

            // 因固定工作流而设死的开关宏
            #define _SPECULAR_SETUP 1 // 高光工作流
            // 卡通头发阴影
            //#define _HAS_CEL_HAIR_SHADOW_V1 1

            // 冰霜效果
            //#pragma shader_feature_local _ _FROST_ON
            //#pragma shader_feature_local _ _ICE_OVERLAY_MASK_ON
            //#pragma shader_feature_local _ _TRANSMISSION_LIGHT_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            
            // 导入 hlsl
            #include "./Include/Generated/ToonPBR_AutoDefines.hlsl"
            
            #include "../ShaderLibrary/Lighting.hlsl"
            
            #include "./Include/ToonPBR_Input.hlsl"
            
            #include "./Include/ToonPBR_VertexPull.hlsl"
            #include "./Include/ToonPBR_Core.hlsl"
            #include "./Include/ToonPBR_Specular.hlsl"
            #include "./Include/ToonPBR_Diffuse.hlsl"
            #include "./Include/ToonPBR_Rim.hlsl"
            #include "./Include/ToonPBR_Fresnel.hlsl"
            #include "./Include/ToonPBR_Eye.hlsl"
            #include "./Include/ToonPBR_Lighting.hlsl"

            #include "./Include/ToonPBR_Forward_LOD1.hlsl"
            ENDHLSL
        }

        // Outline
        Pass
        {
            Name "Outline"
            Tags
            {
                "LightMode" = "Outline"
            }
            
            Cull Front

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma vertex Vertex
            #pragma fragment Fragment
            
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_vertex _MESH_PREVIEW_MODE
            #pragma shader_feature_local _ _RANDOM_DISSOLVE_ON _DIRECTION_DISSOLVE_ON _MASK_DISSOLVE_ON

            #pragma multi_compile_local_fragment _ _DITHER_ON
            #pragma shader_feature_local _ _OUTLINELOCALCOLOR
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
           
            #include "./Include/ToonPBR_Input.hlsl"
            #include "./Include/ToonPBR_Outline.hlsl"
            ENDHLSL
        }

        // MotionVectors
        Pass
        {
            Name "MotionVectors"
            Tags
            {
                "LightMode" = "MotionVectors"
            }
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.5
            #pragma vertex Vertex
            #pragma fragment Fragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ _RANDOM_DISSOLVE_ON _DIRECTION_DISSOLVE_ON _MASK_DISSOLVE_ON
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "./Include/ToonPBR_Input.hlsl"
            #include_with_pragmas "./Include/ToonPBR_MotionVectors.hlsl"
            ENDHLSL
        }

        // MotionVectorsOutline
        Pass
        {
            Name "MotionVectorsOutline"
            Tags
            {
                "LightMode" = "MotionVectorsOutline"
            }
            
            Cull Front

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.5
            #pragma vertex Vertex
            #pragma fragment Fragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ _RANDOM_DISSOLVE_ON _DIRECTION_DISSOLVE_ON _MASK_DISSOLVE_ON
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "./Include/ToonPBR_Input.hlsl"
            #include_with_pragmas "./Include/ToonPBR_MotionVectorsOutline.hlsl"
            ENDHLSL
        }
        UsePass "Hidden/XKNight/SubsurfaceDiffuse/SUBSURFACEDIFFUSE"
        // ShadowCaster
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
            
            Cull [_Cull]
            ColorMask 0

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            // -------------------------------------
            // Pipeline keywords

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ _RANDOM_DISSOLVE_ON _DIRECTION_DISSOLVE_ON _MASK_DISSOLVE_ON
            
            #pragma multi_compile_local_fragment _ _DITHER_ON
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "./Include/ToonPBR_Input.hlsl"
            #include "./Include/ToonPBR_ShadowCaster.hlsl"
            ENDHLSL
        }

        // DepthOnly
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            Cull [_Cull]
            ColorMask R

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Pipeline keywords

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ _RANDOM_DISSOLVE_ON _DIRECTION_DISSOLVE_ON _MASK_DISSOLVE_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "./Include/ToonPBR_Input.hlsl"
            #include "./Include/ToonPBR_DepthOnlyPass.hlsl"
            ENDHLSL
        }

        // DepthNormals
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }
            
            Cull [_Cull]

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Pipeline keywords

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ _RANDOM_DISSOLVE_ON _DIRECTION_DISSOLVE_ON _MASK_DISSOLVE_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "./Include/ToonPBR_Input.hlsl"
            #include "./Include/ToonPBR_DepthNormalsPass.hlsl"
            ENDHLSL
        }

        // DepthMask
        Pass
        {
            Name "DepthMask"
            Tags
            {
                "LightMode" = "DepthMask"
            }

            Cull [_Cull]

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma vertex DepthMaskVertex
            #pragma fragment DepthMaskFragment

            // -------------------------------------
            // Pipeline keywords
            // #pragma multi_compile_fragment _BLOOMFACTORMASK

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ _RANDOM_DISSOLVE_ON _DIRECTION_DISSOLVE_ON _MASK_DISSOLVE_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "./Include/ToonPBR_Input.hlsl"
            #include "./Include/ToonPBR_DepthMask.hlsl"
            ENDHLSL
        }

        // CharacterMask
        Pass
        {
            Name "CharacterMask"
            Tags
            {
                "LightMode" = "CharacterMask"
            }

            Cull [_Cull]

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma vertex CharacterMaskVertex
            #pragma fragment CharacterMaskFragment
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "./Include/ToonPBR_Input.hlsl"
            #include "./Include/ToonPBR_CharacterMask.hlsl"
            ENDHLSL
        }

        // Screen effect mask -- R = occupancy, G = effect id
        Pass
        {
            Name "ScreenEffMask"
            Tags { "LightMode" = "ScreenEffMask" }
            Cull [_Cull]
            ColorMask RG
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma vertex ScreenEffectMaskVertex
            #pragma fragment ScreenEffectMaskFragment
            #pragma multi_compile_instancing

            #include "./Include/ToonPBR_Input.hlsl"
            #include "./Include/ToonPBR_ScreenEffectMask.hlsl"
            ENDHLSL
        }

        // ViewSpaceNormals
        Pass
        {
            Name "ViewSpaceNormals"
            Tags
            {
                "LightMode" = "ViewSpaceNormals"
            }
            
            Cull [_Cull]

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma vertex ViewSpaceNormalsVertex
            #pragma fragment ViewSpaceNormalsFragment

            // -------------------------------------
            // Pipeline keywords
            // #pragma multi_compile_fragment _BLOOMFACTORMASK

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _ _RANDOM_DISSOLVE_ON _DIRECTION_DISSOLVE_ON _MASK_DISSOLVE_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "./Include/ToonPBR_Input.hlsl"
            #include "./Include/ToonPBR_ViewSpaceNormals.hlsl"
            ENDHLSL
        }

        // buff 标记 - 霜冻
        Pass
        {
            Name "buff_frost"
            Tags
            {
                "LightMode" = "buff_frost"
            }

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma vertex BuffPassVertex
            #pragma fragment BuffPassFragment
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            
            #include "./Include/ToonPBR_Input.hlsl"
            #include "./Include/ToonPBR_Buff.hlsl"
            ENDHLSL
        }

        // buff 标记 - 霜冻 大体型怪物用的版本
        Pass
        {
            Name "buff_frost_big"
            Tags
            {
                "LightMode" = "buff_frost_big"
            }
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma vertex BuffPassVertex
            #pragma fragment BuffPassFragment
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            
            #include "./Include/ToonPBR_Input.hlsl"
            #include "./Include/ToonPBR_Buff.hlsl"
            ENDHLSL
        }

        // HairShadowV2Mark
        Pass
        {
            Name "HairShadowV2Mark"
            Tags
            {
                "LightMode" = "HairShadowV2Mark"
            }
            
            Stencil
            {
                // 通过和脸上的模板值进行比较，确定头发的位置
                Ref 128
                Comp Equal
                // 将头帘的模板值设置为 128+1
                Pass IncrSat
            }
            
            ZTest LEqual
            ZWrite Off
            ColorMask 0 // 不输出颜色
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0
            #pragma vertex HairShadowVertex
            #pragma fragment HairShadowFragment
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            
            #include "./Include/ToonPBR_Input.hlsl"
            
            #include "./Include/ToonPBR_HairShadowV2Mark.hlsl"
            ENDHLSL
        }

        // HairShadowV2Draw
        Pass
        {
            Name "HairShadowV2Draw"
            Tags
            {
                "LightMode" = "HairShadowV2Draw"
            }
            
            Stencil
            {
                // 129 的位置，就是有头帘的地方，让我们来给它上色
                Ref 129
                Comp Equal
                Pass keep
            }
            
            ZTest LEqual
            ZWrite Off
            
            Cull [_Cull]
            Blend [_SrcBlend][_DstBlend], [_SrcBlendAlpha][_DstBlendAlpha]
            
            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            #pragma vertex ToonForwardPassVertex
            #pragma fragment ToonForwardPassFragment

            // -------------------------------------
            // Unity defined keywords
            
            // -------------------------------------
            // Pipeline keywords
            //#pragma multi_compile _ _TIMELINE_FULL_LIGHT_ON
            #pragma multi_compile _ _GLOBAL_OVERRIDE_CHARACTER_LIGHTIING_ON
            // #pragma shader_feature_vertex _ _VERTEX_PULL_ON
            #pragma multi_compile _ _HAS_CAMERA_CHARACTER_DEPTH_TEXTURE
            
            #pragma multi_compile _ _HEIGHT_FOG // 雾效
            #pragma shader_feature _ _RECORDING_QUALITY // 录制画质

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ _SDFSHADOWMAP _RAMP_MODE_ON _ILM_SHADOW_MASK_ON _DIFFUSE_OFFSET
            #pragma shader_feature_local_fragment _ _EMISSION
            #pragma shader_feature_local_fragment _ _EYE_MODE
            #pragma shader_feature_local_fragment _ _RIM_ON
            
            #pragma shader_feature_local_fragment _ _COMBAT_SURFACE_GLOW_ON
            #pragma shader_feature_local_fragment _ _RG_EFFECT_ON
            #pragma shader_feature_local_fragment _ _RG_EFFECT_LOCAL_ON
            // 用法是默认在材质球勾选，所以可以用feature方式
            #pragma shader_feature_local _ _RANDOM_DISSOLVE_ON _DIRECTION_DISSOLVE_ON _MASK_DISSOLVE_ON
            #pragma shader_feature_local_fragment _ _SDF_MESH_RENDER_MODE

            #pragma multi_compile_local_fragment _ _DITHER_ON

            // 因固定工作流而设死的开关宏
            #define _SPECULAR_SETUP 1 // 高光工作流
            // 卡通头发阴影
            #define _HAS_CEL_HAIR_SHADOW_V2 1

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            // 导入 hlsl
            #include "./Include/Generated/ToonPBR_AutoDefines.hlsl"
            
            #include "../ShaderLibrary/Lighting.hlsl"
            
            #include "./Include/ToonPBR_Input.hlsl"
            
            #include "./Include/ToonPBR_VertexPull.hlsl"
            #include "./Include/ToonPBR_Core.hlsl"
            #include "./Include/ToonPBR_Specular.hlsl"
            #include "./Include/ToonPBR_Diffuse.hlsl"
            #include "./Include/ToonPBR_Rim.hlsl"
            #include "./Include/ToonPBR_Fresnel.hlsl"
            #include "./Include/ToonPBR_Eye.hlsl"
            #include "./Include/ToonPBR_Lighting.hlsl"

            #include "./Include/ToonPBR_Forward_LOD1.hlsl"
            ENDHLSL
        }
    }
    
    CustomEditor "XKnight.ShaderGUI.ToonPBRShaderGUINew"
}
