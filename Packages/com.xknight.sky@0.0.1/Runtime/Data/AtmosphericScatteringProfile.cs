namespace UnityEngine.Rendering.Universal
{
    public class AtmosphericScatteringProfile : ScriptableObject
    {
        public CloudProfile cloudProfile;
        public TimeProfile timeProfile;
        public AtmosphereProfile atmosphereProfile;
        public VolumetricLightProfile volumetricLightProfile;

        #region TODO
        public CloudProfileMap cloudProfileMap;
        #endregion
    }
}
