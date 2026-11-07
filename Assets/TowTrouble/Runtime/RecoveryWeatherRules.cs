using UnityEngine;

namespace TowTrouble
{
    public static class RecoveryWeatherRules
    {
        public const float DaySeconds=1920,WeatherSeconds=300,TransitionSeconds=30,StartDay=.4f;
        public static float Day(float seconds)=>Mathf.Repeat(StartDay+Mathf.Max(0,seconds)/DaySeconds,1);
        public static float Night(float day)
        {
            float hour=Mathf.Repeat(day,1)*24;
            return Mathf.Max(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(5.5f,7.25f,hour)),Mathf.SmoothStep(0,1,Mathf.InverseLerp(18.25f,20,hour)));
        }
        public static int Forecast(int segment,uint seed)
        {
            if(segment<=0)return 1;
            unchecked{uint h=(uint)segment*747796405u+seed*2891336453u;h=(h^(h>>16))*2246822519u;return (int)((h^(h>>13))%4);}
        }
        public static void Weather(float seconds,uint seed,out int from,out int to,out float blend)
        {
            seconds=Mathf.Max(0,seconds);int segment=Mathf.FloorToInt(seconds/WeatherSeconds);
            from=Forecast(segment,seed);to=Forecast(segment+1,seed);
            blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(WeatherSeconds-TransitionSeconds,WeatherSeconds,seconds%WeatherSeconds));
        }
        public static float Wet(int from,int to,float blend)=>Mathf.Lerp(from==3?1:0,to==3?1:0,blend);
        public static void Precipitation(float wet,float snowWeight,out float rain,out float snow)
        {rain=Mathf.Clamp01(wet)*(1-Mathf.Clamp01(snowWeight))*.7f;snow=Mathf.Clamp01(snowWeight)*Mathf.Lerp(.4f,.75f,Mathf.Clamp01(wet));}
    }
}
