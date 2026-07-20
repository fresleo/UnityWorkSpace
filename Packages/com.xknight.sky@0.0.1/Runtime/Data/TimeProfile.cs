namespace UnityEngine.Rendering.Universal
{
    public class TimeProfile : ScriptableObject
    {
        public bool TimeOfDay = false;
        [Range(0.0f, 23.999f)] public float Timeline = 10f;

        [Range(-90f, 90f)] public float Latitude = 0f;
        [Range(-180f, 180f)] public float Longitude = 0f;
        [Range(-12f, 12f)] public float UTC = 0f;

        public Color SunLightColor = new Color(1.0f, 0.9647059f, 0.9294118f, 1.0f);
        public AnimationCurve SunLightIntensity = AnimationCurve.Constant(0, 1, 2f);
        public Color MoonLightColor = new Color(0.901f, 0.951f, 1.0f, 1.0f);
        public AnimationCurve MoonLightIntensity = AnimationCurve.Constant(0, 1, 0.5f);

        public bool PlayTime = false;   
        public float DayLengthInMinutes = 15f;
    }
}