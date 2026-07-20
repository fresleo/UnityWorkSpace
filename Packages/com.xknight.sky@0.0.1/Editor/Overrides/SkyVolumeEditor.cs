using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [VolumeComponentEditor(typeof(SkyVolume))]
    public class SkyVolumeEditor : VolumeComponentEditor
    {
        SerializedDataParameter skySetting;
        SerializedDataParameter renderClouds;
        public override void OnEnable()
        {
            var o = new PropertyFetcher<SkyVolume>(serializedObject);

            skySetting = Unpack(o.Find(x => x.SkySetting));
            renderClouds = Unpack(o.Find(x => x.RenderClouds));
        }

        public override void OnInspectorGUI()
        {
            var rootObject = new SerializedObject(target);
            PropertyField(skySetting, EditorGUIUtility.TrTextContent("天气配置"));
            PropertyField(renderClouds, EditorGUIUtility.TrTextContent("是否渲染云"));

            rootObject.ApplyModifiedProperties();
        }
    }
}
