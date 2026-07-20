using UnityEngine;
using UnityEditor;

namespace XKnight.XLOD
{
    [CustomEditor(typeof(EffectHost))]
    public class EffectHostEditor : Editor
    {
        EffectHost script;

        int[] enableCounts = new int[3];

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            script = target as EffectHost;

            EditorGUILayout.LabelField("质量切换只在运行状态下可预览效果");
            if (GUILayout.Button("高质量"))
            {
                script.SetLOD((int)EffectQuality.HIGH);
                script.PlayEffect();
            }
            if (GUILayout.Button("中质量"))
            {
                script.SetLOD((int)EffectQuality.MEDIUM);
                script.PlayEffect();
            }
            if (GUILayout.Button("低质量"))
            {
                script.SetLOD((int)EffectQuality.LOW);
                script.PlayEffect();
            }
            if (GUILayout.Button("添加所有子物体的LOD"))
            {
                script.AddEffectHolderForAllChildren();
            }
            if (GUILayout.Button("移除所有子物体的LOD"))
            {
                if (EditorUtility.DisplayDialog("注意", "所有子物体中添加的特效LOD将被移除！", "不要怂，就是干", "容我再想想"))
                {
                    script.RemoveEffectHolderInAllChildren();
                }
            }

            if (script.CheckHolder(enableCounts))
            {
                EditorGUILayout.LabelField("高中低质量可见数量依次为: " + enableCounts[0] + "、" +
                    enableCounts[1] + "、" + enableCounts[2]);
                if (enableCounts[1] > (int)(enableCounts[0] * 0.7) || enableCounts[2] > (int)(enableCounts[0] * 0.3))
                {
                    GUI.contentColor = Color.red;
                    EditorGUILayout.LabelField("警告: 高中低质量显示比例不满足100%、70%、30%的比例要求！");
                    GUI.contentColor = Color.white;
                }
            }
            else
            {
                EditorGUILayout.LabelField("请点击\"添加所有子物体的LOD\"");
            }
        }

        [MenuItem("GameObject/添加特效LOD组件")]
        static void CreateForwardRenderData()
        {
            GameObject obj = Selection.activeGameObject;
            if (obj.GetComponent<EffectHost>() == null)
            {
                obj.AddComponent<EffectHost>();
            }
        }
    }
}
