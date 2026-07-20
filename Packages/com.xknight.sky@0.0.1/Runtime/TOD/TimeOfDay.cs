using System;

namespace UnityEngine.Rendering.Universal
{
    public class TimeOfDay
    {
        const float k_TimelineLength = 24f;

        public float Timeline;

        bool m_IsValid = false;
        float m_DurationCycle;

        int m_Day = -1;
        int m_Month = -1;
        int m_Year = -1;

        CelestialsCalculations m_CelestialsCalculations;

        public TimeOfDay()
        {
            m_CelestialsCalculations = new CelestialsCalculations();
        }

        public void Update(TimeProfile profile)
        {
            UpdateProfile(profile);
            UpdateTime(profile);
        }

        void UpdateProfile(TimeProfile profile)
        {
            bool valid = profile.TimeOfDay;            
            if (!m_IsValid && valid) // 开启TOD
            {
                Timeline = profile.Timeline;
            }

            m_IsValid = valid;
            m_DurationCycle = 60 * profile.DayLengthInMinutes;
            m_CelestialsCalculations.Latitude = profile.Latitude;
            m_CelestialsCalculations.Longitude = profile.Longitude;
            m_CelestialsCalculations.UTC = profile.UTC;
        }

        void UpdateTime(TimeProfile profile)
        {
            if (!m_IsValid)
            {
                return;
            }

            if (profile.PlayTime && Application.isPlaying)
            {
                Timeline += m_DurationCycle != 0 ? (Time.deltaTime / m_DurationCycle) * k_TimelineLength : 0.0f;
            }
            else
            {
                Timeline = profile.Timeline;
            }
        }

        public bool ComputeDirection(out Vector3 SunDirection, out Vector3 MoonDirection)
        {
            if (!m_IsValid)
            {
                SunDirection = Vector3.zero;
                MoonDirection = Vector3.zero;
                return false;
            }

            m_CelestialsCalculations.DateTime = GetTateTime();

            // Sun
            Vector3 sunCoords = m_CelestialsCalculations.GetSunCoords();
            float SunPI = sunCoords.x - Mathf.PI;
            float SunTheta = sunCoords.z;

            // Moon
            Vector3 moonCoords = m_CelestialsCalculations.GetMoonCoords();
            float MoonPI = moonCoords.x - Mathf.PI;
            float MoonTheta = moonCoords.z;

            SunDirection = SphericalToCartesian(SunTheta, SunPI);
            MoonDirection = SphericalToCartesian(MoonTheta, MoonPI);

            return true;
        }

        float TimeToFloat(int hour, int minute, int second, int millisecond)
        {
            return (float)hour + (float)minute / 60f + (float)second / 3600f + (float)millisecond / 3600000f;
        }

        DateTime GetTateTime()
        {
            if (m_Year == -1)
            {
                DateTime dataTimeNow = DateTime.Now;
                m_Year = dataTimeNow.Year;
                m_Month = dataTimeNow.Month;
                m_Day = dataTimeNow.Day;
            }

            // Create new DateTime.
            DateTime dateTime = new DateTime(0, System.DateTimeKind.Utc);          

            // Add date and time in DateTime.
            dateTime = dateTime.AddYears(m_Year - 1).
                AddMonths(m_Month - 1).
                AddDays(m_Day - 1).
                AddHours(Timeline);

            // Set date.
            m_Year = dateTime.Year;
            m_Month = dateTime.Month;
            m_Day = dateTime.Day;

            // Set timeline.
            Timeline = TimeToFloat(dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond);

            return dateTime;
        }

        Vector3 SphericalToCartesian(float theta, float pi)
        {
            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);
            float sinPI = Mathf.Sin(pi);
            float cosPI = Mathf.Cos(pi);

            return new Vector3()
            {
                x = sinTheta * sinPI,
                y = cosTheta,
                z = sinTheta * cosPI
            };
        }
    }
}