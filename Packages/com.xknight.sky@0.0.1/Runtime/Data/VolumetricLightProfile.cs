namespace UnityEngine.Rendering.Universal
{
    public class VolumetricLightProfile : ScriptableObject
    {
        public Texture DitheringTex;
        [Range(0.0f, 1.0f)] public float VolumetricLightRange = 0.5f;
        public Color VolumetricLightColor = new Color(0.2f, 0.2f, 0.2f, 1.0f);
    }
}