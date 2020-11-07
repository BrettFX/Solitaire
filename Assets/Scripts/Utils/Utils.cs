using UnityEngine;

namespace Solitaire
{
    public class Utils
    {
        public static string GetTimestamp(float milliseconds)
        {
            float t = milliseconds / 1000.0f;
            float remainderMillis = (Mathf.Floor(t * 100) % 100); // calculate the milliseconds for the timer

            int seconds = (int)(t % 60); // return the remainder of the seconds divide by 60 as an int
            t /= 60; // divide current time y 60 to get minutes
            int minutes = (int)(t % 60); //return the remainder of the minutes divide by 60 as an int
            t /= 60; // divide by 60 to get hours
            int hours = (int)(t % 24); // return the remainder of the hours divided by 60 as an int

            return string.Format("{0}:{1}:{2}.{3}", hours.ToString("00"), minutes.ToString("00"), seconds.ToString("00"), remainderMillis.ToString("00"));
        }
    }
}

