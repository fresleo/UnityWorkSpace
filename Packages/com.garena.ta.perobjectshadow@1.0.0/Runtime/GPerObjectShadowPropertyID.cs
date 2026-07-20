using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Garena.TA
{
    public class GPerObjectShadowPropertyID
    {
        public static int PID_Color = Shader.PropertyToID("_Color");
        public static int PID_SrcBlend = Shader.PropertyToID("_SrcBlend");
        public static int PID_DstBlend = Shader.PropertyToID("_DstBlend");

        public static int PID_GPerObjectShadowCount = Shader.PropertyToID("_GPerObjectShadowCount");
        public static int PID_GPerObjectWorldToShadow = Shader.PropertyToID("_GPerObjectWorldToShadow");
        public static int PID_GPerObjectShadowUVRect = Shader.PropertyToID("_GPerObjectShadowUVRect");
        public static int PID_GPerObjectShadowIntensity = Shader.PropertyToID("_GPerObjectShadowIntensity");
        public static int PID_GPerObjectShadowmapTextureSize = Shader.PropertyToID("_GPerObjectShadowMapSize");

        public static int PID_CharacterMaskTexture = Shader.PropertyToID("_CharacterMaskTexture");

        public static int PID_GPerObjectShadowEnable = Shader.PropertyToID("_GPerObjectShadowEnable");
        /// <summary>
        /// 逐物体阴影纹理名
        /// </summary>
        public const string GPerObjectShadowMapName = "_GPerObjectShadowMap";

        public const string GEmptyPerObjectShadowMapName = "_GEmptyPerObjectShadowMap";


        /// <summary>
        /// 逐物体阴影纹理shader id
        /// </summary>
        public static int PID_GPerObjectShadowMap = Shader.PropertyToID(GPerObjectShadowMapName);

        public const string ResolvePostShaderName = "GarenaTA/GPerObjectShadow/ResolvePost";
        public const string ResolveShaderName = "GarenaTA/GPerObjectShadow/Resolve";
        public const string ApplyShaderName = "GarenaTA/GPerObjectShadow/Apply";

        public const string GPerObjectScreenSpaceShadowMapName = "_GPerObjectScreenSpaceShadowMap";
        public static int PID_GPerObjectScreenSpaceShadowMap = Shader.PropertyToID(GPerObjectScreenSpaceShadowMapName);

        public const string UnityScreenSpaceShadowMapName = "_ScreenSpaceShadowmapTexture";



        // 以下为角色自投影的相关信息
        public static string PID_GPO_EmptyCharacterShadowMapName = "_GEmptyPerObjectCharacterShadowMap";
        public static readonly int PID_GPO_CharacterShadowEnable = Shader.PropertyToID("_GPerObjectCharacterShadowEnable");
        public const string PID_GPO_CharacterShadowMapName = "_GPerObjectCharacterShadowMap";
        public static readonly int PID_GPO_CharacterShadowMap = Shader.PropertyToID(PID_GPO_CharacterShadowMapName);
        public static readonly int PID_GPO_CharacterShadowMapSize = Shader.PropertyToID("_GPerObjectCharacterShadowMapSize");
        public static readonly int PID_GPO_CharacterCount = Shader.PropertyToID("_GPerObjectCharacterShadowCount");
        public static readonly int PID_GPO_CharacterWorldToShadow = Shader.PropertyToID("_GPerObjectCharacterWorldToShadow");
        public static readonly int PID_GPO_CharacterShadowUVRect = Shader.PropertyToID("_GPerObjectCharacterShadowUVRect");

    }
}
