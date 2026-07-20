using UnityEditor.ProjectWindowCallback;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    public class SkyProfileAssetCreator : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var skyProfile = CreateInstance<AtmosphericScatteringProfile>();
            AssetDatabase.CreateAsset(skyProfile, !string.IsNullOrEmpty(pathName) ? pathName : "Assets/SkyProfile.asset");
            ProjectWindowUtil.ShowCreatedAsset(skyProfile);
            AssetDatabase.SaveAssets();
        }
    }
}