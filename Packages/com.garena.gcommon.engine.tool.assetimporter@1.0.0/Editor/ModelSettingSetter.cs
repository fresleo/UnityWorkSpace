// Created by: WangYu   Date: 2025-09-26

using System;
using UnityEditor;
using UnityEngine;

namespace com.garena.gcommon.engine.tool.assetimporter
{
    internal class ModelSettingSetter
    {
        private const string c_Prop_materialImportMode = "materialImportMode";
        private const string c_Prop_importAnimation = "importAnimation";
        private const string c_Prop_animationType = "animationType";
        private const string c_Prop_meshCompression = "meshCompression";
        private const string c_Prop_isReadable = "isReadable";
        private const string c_Prop_animationRotationError = "animationRotationError";
        private const string c_Prop_animationPositionError = "animationPositionError";
        private const string c_Prop_animationCompression = "animationCompression";
        private const string c_Prop_removeConstantScaleCurves = "removeConstantScaleCurves";
        private const string c_Prop_importBlendShapeNormals = "importBlendShapeNormals";
        
        public static string SetModelSingleProp(ModelImporter mi, string name, string value, bool isApplyChange)
        {
            string dirtyValue = "";

            switch (name)
            {
                case c_Prop_materialImportMode:
                    SetMaterialImportMode(ref dirtyValue, mi, value, isApplyChange);
                    break;
                case c_Prop_importAnimation:
                    SetImportAnimation(ref dirtyValue, mi, value, isApplyChange);
                    break;
                case c_Prop_animationType:
                    SetAnimationType(ref dirtyValue, mi, value, isApplyChange);
                    break;
                case c_Prop_meshCompression:
                    SetMeshCompression(ref dirtyValue, mi, value, isApplyChange);
                    break;
                case c_Prop_isReadable:
                    SetIsReadable(ref dirtyValue, mi, value, isApplyChange);
                    break;
                case c_Prop_animationRotationError:
                    SetAnimationRotationError(ref dirtyValue, mi, value, isApplyChange);
                    break;
                case c_Prop_animationPositionError:
                    SetAnimationPositionError(ref dirtyValue, mi, value, isApplyChange);
                    break;
                case c_Prop_animationCompression:
                    SetAnimationCompression(ref dirtyValue, mi, value, isApplyChange);
                    break;
                case c_Prop_removeConstantScaleCurves:
                    SetRemoveConstantScaleCurves(ref dirtyValue, mi, value, isApplyChange);
                    break;
                case c_Prop_importBlendShapeNormals:
                    SetImportBlendShapeNormals(ref dirtyValue, mi, value, isApplyChange);
                    break;
            }

            return dirtyValue;
        }

        
        private static void SetMaterialImportMode(ref string dirtyValue, ModelImporter mi, string value, bool isApplyChange)
        {
            if (Enum.TryParse(value, out ModelImporterMaterialImportMode importMode))
            {
                if (mi.materialImportMode != importMode)
                {
                    dirtyValue = mi.materialImportMode.ToString();
                    if (isApplyChange)
                    {
                        mi.materialImportMode = importMode;
                    }
                }
            }
        }

        private static void SetImportAnimation(ref string dirtyValue, ModelImporter mi, string value, bool isApplyChange)
        {
            if (bool.TryParse(value, out bool flag))
            {
                if (mi.importAnimation != flag)
                {
                    dirtyValue = mi.importAnimation.ToString();
                    if (isApplyChange)
                    {
                        mi.importAnimation = flag;
                    }
                }
            }
        }

        private static void SetAnimationType(ref string dirtyValue, ModelImporter mi, string value, bool isApplyChange)
        {
            if (Enum.TryParse(value, out ModelImporterAnimationType animationType))
            {
                if (mi.animationType != animationType)
                {
                    dirtyValue = mi.animationType.ToString();
                    if (isApplyChange)
                    {
                        mi.animationType = animationType;
                    }
                }
            }
        }

        private static void SetMeshCompression(ref string dirtyValue, ModelImporter mi, string value, bool isApplyChange)
        {
            if (Enum.TryParse(value, out ModelImporterMeshCompression meshCompression))
            {
                if (mi.meshCompression != meshCompression)
                {
                    dirtyValue = mi.meshCompression.ToString();
                    if (isApplyChange)
                    {
                        mi.meshCompression = meshCompression;
                    }
                }
            }
        }

        private static void SetIsReadable(ref string dirtyValue, ModelImporter mi, string value, bool isApplyChange)
        {
            if (bool.TryParse(value, out bool flag))
            {
                if (mi.isReadable != flag)
                {
                    dirtyValue = mi.isReadable.ToString();
                    if (isApplyChange)
                    {
                        mi.isReadable = flag;
                    }
                }
            }
        }

        private static void SetAnimationRotationError(ref string dirtyValue, ModelImporter mi, string value, bool isApplyChange)
        {
            if (float.TryParse(value, out float animationRotationError))
            {
                if (!Mathf.Approximately(mi.animationRotationError, animationRotationError))
                {
                    dirtyValue = mi.animationRotationError.ToString();
                    if (isApplyChange)
                    {
                        mi.animationRotationError = animationRotationError;
                    }
                }
            }
        }

        private static void SetAnimationPositionError(ref string dirtyValue, ModelImporter mi, string value, bool isApplyChange)
        {
            if (float.TryParse(value, out float animationPositionError))
            {
                if (!Mathf.Approximately(mi.animationPositionError, animationPositionError))
                {
                    dirtyValue = mi.animationPositionError.ToString();
                    if (isApplyChange)
                    {
                        mi.animationPositionError = animationPositionError;
                    }
                }
            }
        }

        private static void SetAnimationCompression(ref string dirtyValue, ModelImporter mi, string value, bool isApplyChange)
        {
            if (Enum.TryParse(value, out ModelImporterAnimationCompression animCompression))
            {
                if (mi.animationCompression != animCompression)
                {
                    dirtyValue = mi.animationCompression.ToString();
                    if (isApplyChange)
                    {
                        mi.animationCompression = animCompression;
                    }
                }
            }
        }

        private static void SetRemoveConstantScaleCurves(ref string dirtyValue, ModelImporter mi, string value, bool isApplyChange)
        {
            if (bool.TryParse(value, out bool flag))
            {
                if (mi.removeConstantScaleCurves != flag)
                {
                    dirtyValue = mi.removeConstantScaleCurves.ToString();
                    if (isApplyChange)
                    {
                        mi.removeConstantScaleCurves = flag;
                    }
                }
            }
        }

        private static void SetImportBlendShapeNormals(ref string dirtyValue, ModelImporter mi, string value, bool isApplyChange)
        {
            if (Enum.TryParse(value, out ModelImporterNormals modelImporterNormals))
            {
                if (mi.importBlendShapeNormals != modelImporterNormals)
                {
                    dirtyValue = mi.importBlendShapeNormals.ToString();
                    if (isApplyChange)
                    {
                        mi.importBlendShapeNormals = modelImporterNormals;
                    }
                }
            }
        }
        
    }
}