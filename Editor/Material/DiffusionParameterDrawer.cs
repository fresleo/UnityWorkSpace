//date: 2026/6/30
//author:calvin
//describe:接管SSS材质属性的显示

using UnityEngine;
using UnityEditor;
using System;

namespace Garena.TA.SSS
{
    public static class DP_GUID
    {
        public static float AsFloat(uint val) => BitConverter.Int32BitsToSingle((int)val);

        public static uint AsUInt(float val) => (uint)BitConverter.SingleToInt32Bits(val);

        internal static Vector4 ConvertGUIDToVector4(string guid)
        {
            byte[] bytes = new byte[16];

            for (int i = 0; i < 16; i++)
                bytes[i] = byte.Parse(guid.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);

            return new Vector4(
                BitConverter.ToSingle(bytes, 0),
                BitConverter.ToSingle(bytes, 4),
                BitConverter.ToSingle(bytes, 8),
                BitConverter.ToSingle(bytes, 12)
            );
        }

        internal static string ConvertVector4ToGUID(Vector4 vector)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(32);
            byte[] xBytes = BitConverter.GetBytes(vector.x);
            byte[] yBytes = BitConverter.GetBytes(vector.y);
            byte[] zBytes = BitConverter.GetBytes(vector.z);
            byte[] wBytes = BitConverter.GetBytes(vector.w);

            for (int i = 0; i < 4; i++) sb.Append(xBytes[i].ToString("x2"));
            for (int i = 0; i < 4; i++) sb.Append(yBytes[i].ToString("x2"));
            for (int i = 0; i < 4; i++) sb.Append(zBytes[i].ToString("x2"));
            for (int i = 0; i < 4; i++) sb.Append(wBytes[i].ToString("x2"));

            return sb.ToString();
        }
    }

    public class DiffusionParameterDrawer : MaterialPropertyDrawer //接管材质属性的显示
    {
        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor) =>
            0; //决定该属性在材质面板中占用的垂直高度(profile的id值不占用高度)

        //实际的UI
        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            //Reflection:  [DiffusionParameter] + Drawer
            var assetProperty = MaterialEditor.GetMaterialProperty(editor.targets, prop.name + "_Asset");
            KnightDiffusionProfileMaterialUI.OnGUI(assetProperty, prop, prop.displayName);
        }
    }

    public static class KnightDiffusionProfileMaterialUI //具体的逻辑处理
    {
        private const string DiffusionParametersNotAssigned = "材质未定义Diffusion Prameters资产\n" +
                                                           "该材质需要一个默认的Diffusion Prameters资产.";

        public static void OnGUI(MaterialProperty diffusionProfileAsset, MaterialProperty diffusionProfileHash,
            string displayName = "Diffusion Parameters")
        {
            MaterialEditor.BeginProperty(diffusionProfileAsset);
            MaterialEditor.BeginProperty(diffusionProfileHash);
            string guid = DP_GUID.ConvertVector4ToGUID(diffusionProfileAsset.vectorValue);//将vector转化成16位的guid
            DiffusionParameter diffusionParameters = AssetDatabase.LoadAssetAtPath<DiffusionParameter>(AssetDatabase.GUIDToAssetPath(guid));//通过guid获取到DiffusionParameter资产
            
            EditorGUI.BeginChangeCheck();
            diffusionParameters = (DiffusionParameter)EditorGUILayout.ObjectField(displayName, diffusionParameters, typeof(DiffusionParameter), false);
            if (EditorGUI.EndChangeCheck())
            {
                Vector4 newGuid = Vector4.zero;
                float hash = 0;

                if (diffusionParameters != null)
                {
                    // 将 Assets存入 vector
                    // bufferid存入 float
                    guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(diffusionParameters));
                    newGuid = DP_GUID.ConvertGUIDToVector4(guid);
                    hash = DP_GUID.AsFloat(diffusionParameters.hash);
                }
                diffusionProfileAsset.vectorValue = newGuid;
                diffusionProfileHash.floatValue = hash;
                
            }
            MaterialEditor.EndProperty();
            MaterialEditor.EndProperty();
            DrawDiffusionProfileWarning(diffusionParameters);
        }
        private static void DrawDiffusionProfileWarning(DiffusionParameter materialProfile)
        {
            if (materialProfile == null)
            {
                EditorGUILayout.HelpBox(DiffusionParametersNotAssigned, MessageType.Warning);
            }
        }
    }
}