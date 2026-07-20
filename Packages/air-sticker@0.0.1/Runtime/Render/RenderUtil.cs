// Created By: WangYu  Date: 2025-04-29

using UnityEngine;

namespace AirSticker.Runtime.Render
{
    public static class RenderUtil
    {
        public static bool SetFloat(Material material, int propertyId, float floatValue)
        {
            if (material == null) return false;
            if (!material.HasProperty(propertyId)) return false;

            material.SetFloat(propertyId, floatValue);
            return true;
        }
        
        public static bool SetVector(Material material, int propertyId, Vector4 vector4Value)
        {
            if (material == null) return false;
            if (!material.HasProperty(propertyId)) return false;
            
            material.SetVector(propertyId, vector4Value);
            return true;
        }
        
        public static bool SetColor(Material material, int propertyId, Color colorValue)
        {
            if (material == null) return false;
            if (!material.HasProperty(propertyId)) return false;

            material.SetColor(propertyId, colorValue);
            return true;
        }
        
    }
}