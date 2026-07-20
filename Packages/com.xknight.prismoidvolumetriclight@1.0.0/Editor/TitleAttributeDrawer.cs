#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using UnityEngine.Rendering.Universal;

namespace  knightTA.FakeSpotLightTool
    {
    [CustomPropertyDrawer(typeof(TitleAttribute))]
    [CanEditMultipleObjects]
    public class TitleAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI( Rect position, SerializedProperty property, GUIContent label )
        {
            EditorGUI.PropertyField(position, property, new GUIContent( (attribute as TitleAttribute).newTitle ));
        }
    }
}
#endif
