﻿namespace ClimatesOfFerngillRebuild
{
    public class WeatherSync
    {
        public int weatherType;
        public bool isFoggy;
        public bool isWhiteOut;
        public bool isThunderFrenzy;
        public bool isBlizzard;
        public bool isSandstorm;
        public bool isOvercast;
        public bool isVariableRain;
        public int rainAmt;
        public int fogWeatherBeginTime;
        public int fogWeatherEndTime;
        public int blizzWeatherBeginTime;
        public int blizzWeatherEndTime;
        public int whiteWeatherBeginTime;
        public int whiteWeatherEndTime;
        public int thunWeatherBeginTime;
        public int thunWeatherEndTime;
        public int sandstormWeatherBeginTime;
        public int sandstormWeatherEndTime;
        public double todayLow;
        public double todayHigh;
        public double tommorowLow;
        public double tommorowHigh;
        public string RainStatus;
        public ClimateTracker currTracker;

        //default constructor
        public WeatherSync()
        {

        }
    }
}
