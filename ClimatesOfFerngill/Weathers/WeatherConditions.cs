using EnumsNET;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using System.Collections.Generic;
using System.Linq;
using TwilightShards.Common;
using static ClimatesOfFerngillRebuild.Sprites;
using System;
using ClimatesOfFerngillRebuild.Weathers;
using TwilightShards.Stardew.Common;
using StardewModdingAPI.Events;

namespace ClimatesOfFerngillRebuild
{
    /// <summary>
    /// This class tracks the current the current weather of the game
    /// </summary>
    public class WeatherConditions
    {
        /// <summary>This Dictionary tracks elements associated with weathers </summary>
        public Dictionary<int, WeatherData> Weathers;

        /// <summary>Track today's temperature</summary>
        private RangePair TodayTemps;

        /// <summary>Track tomorrow's temperature</summary>
        private RangePair TomorrowTemps;

        /// <summary>Track current conditions</summary>
        private CurrentWeather CurrentConditionsN { get; set; }

        /// <summary>The list of custom weathers </summary>
        internal List<ISDVWeather> CurrentWeathers { get; set; }

        //evening fog details
        private bool HasSetEveningFog {get; set;}
        public bool GenerateEveningFog { get; set; }

        /// ******************************************************************************
        /// CONSTRUCTORS
        /// ******************************************************************************

        /// <summary>
        /// Default Constructor
        /// </summary>
        public WeatherConditions()
        {
            Weathers = PopulateWeathers();

            CurrentConditionsN = CurrentWeather.Unset;
            CurrentWeathers = new List<ISDVWeather>
            {
                new FerngillFog(SDVTimePeriods.Morning),
                new FerngillWhiteOut(),
                new FerngillBlizzard(),
                new FerngillThunderFrenzy(),
                new FerngillSandstorm()
            };

            foreach (ISDVWeather weather in CurrentWeathers)
                weather.OnUpdateStatus += ProcessWeatherChanges;
        }

        public WeatherIcon CurrentWeatherIcon 
        {
            get
            {
                if (ClimatesOfFerngill.MoonAPI != null)
                {
                    if (ClimatesOfFerngill.MoonAPI.GetCurrentMoonPhase() == "Blood Moon")
                        return WeatherIcon.IconBloodMoon;
                }

                if (Weathers.ContainsKey((int)CurrentConditionsN))
                    return Weathers[(int)CurrentConditionsN].Icon;
                else
                    return WeatherIcon.IconError;
            }
        }

        public WeatherIcon CurrentWeatherIconBasic
        {
            get
            {
                if (ClimatesOfFerngill.MoonAPI != null)
                {
                    if (ClimatesOfFerngill.MoonAPI.GetCurrentMoonPhase() == "Blood Moon")
                        return WeatherIcon.IconBloodMoon;
                }

                if (Weathers.ContainsKey((int)CurrentConditionsN))
                    return Weathers[(int)CurrentConditionsN].IconBasic;
                else
                    return WeatherIcon.IconError;
            }
        }

        private Dictionary<int, WeatherData> PopulateWeathers()
        {
            return new Dictionary<int, WeatherData>{ 
                { (int)CurrentWeather.Sunny, new WeatherData(WeatherIcon.IconSunny, WeatherIcon.IconSunny, "sunny", ClimatesOfFerngill.Translator.Get("weather_sunny_daytime"), CondDescNight: ClimatesOfFerngill.Translator.Get("weather_sunny_nighttime"))},

                { (int)(CurrentWeather.Sunny | CurrentWeather.Lightning), new WeatherData(WeatherIcon.IconDryLightning, WeatherIcon.IconDryLightning, "drylightning", ClimatesOfFerngill.Translator.Get("weather_drylightning"))},

                { (int)(CurrentWeather.Sunny | CurrentWeather.Frost), new WeatherData(WeatherIcon.IconSunny, WeatherIcon.IconSunny, CondName: "sunnyfrost", CondDesc: ClimatesOfFerngill.Translator.Get("weather_frost", new { condition = ClimatesOfFerngill.Translator.Get("weather_sunny") }), CondDescNight: ClimatesOfFerngill.Translator.Get("weather_frost_night")) },

                { (int)(CurrentWeather.Sunny | CurrentWeather.Heatwave), new WeatherData(WeatherIcon.IconSunny, WeatherIcon.IconSunny, CondName: "sunnyheatwave", CondDesc: ClimatesOfFerngill.Translator.Get("weather_heatwave")) },

                { (int)(CurrentWeather.Sunny | CurrentWeather.Fog), new WeatherData(WeatherIcon.IconSunnyFog, WeatherIcon.IconSunny, CondName: "fog", CondDesc: ClimatesOfFerngill.Translator.Get("weather_fog", new { condition = ClimatesOfFerngill.Translator.Get("weather_sunny") }), CondDescNight: ClimatesOfFerngill.Translator.Get("weather_frost_night")) },

                { (int)(CurrentWeather.Sunny | CurrentWeather.Fog | CurrentWeather.Frost), new WeatherData(WeatherIcon.IconSunnyFog, WeatherIcon.IconSunny, CondName: "sunnyfrostfog", CondDesc: ClimatesOfFerngill.Translator.Get("weather_frost", new { condition = ClimatesOfFerngill.Translator.Get("weather_fog_basic") }))},

                { (int)(CurrentWeather.Sunny | CurrentWeather.Frost | CurrentWeather.Lightning), new WeatherData(WeatherIcon.IconDryLightning, WeatherIcon.IconDryLightning, "drylightningfrost", ClimatesOfFerngill.Translator.Get("weather_frost", new { condition = ClimatesOfFerngill.Translator.Get("weather_drylightning") }))},

                { (int)(CurrentWeather.Sunny | CurrentWeather.Fog | CurrentWeather.Lightning), new WeatherData(WeatherIcon.IconDryLightningFog, WeatherIcon.IconSunny, CondName: "drylightningfog", CondDesc: ClimatesOfFerngill.Translator.Get("weather_fog", new { condition = ClimatesOfFerngill.Translator.Get("weather_drylightning") }))},

                { (int)(CurrentWeather.Sunny | CurrentWeather.Heatwave | CurrentWeather.Lightning), new WeatherData(WeatherIcon.IconDryLightning, WeatherIcon.IconDryLightning, "drylightningheatwave", ClimatesOfFerngill.Translator.Get("weather_drylightningheatwave"))},

                { (int)(CurrentWeather.Frost | CurrentWeather.Lightning), new WeatherData(WeatherIcon.IconDryLightning, WeatherIcon.IconDryLightning, "drylightningfrost", ClimatesOfFerngill.Translator.Get("weather_frost", new { condition = ClimatesOfFerngill.Translator.Get("weather_drylightning") }))},

                { (int)(CurrentWeather.Heatwave | CurrentWeather.Lightning), new WeatherData(WeatherIcon.IconDryLightning, WeatherIcon.IconDryLightning, "drylightningheatwave", ClimatesOfFerngill.Translator.Get("weather_drylightningheatwave"))},

                { (int)(CurrentWeather.Lightning), new WeatherData(WeatherIcon.IconDryLightning, WeatherIcon.IconDryLightning, "drylightning", ClimatesOfFerngill.Translator.Get("weather_drylightning"))},

                { (int)(CurrentWeather.Unset), new WeatherData(WeatherIcon.IconError, WeatherIcon.IconError, "unset", ClimatesOfFerngill.Translator.Get("weather_unset"))},

                { (int)(CurrentWeather.Rain), new WeatherData(WeatherIcon.IconRain, WeatherIcon.IconRain, "rainy", ClimatesOfFerngill.Translator.Get("weather_rainy")) },

                { (int)(CurrentWeather.Rain | CurrentWeather.Lightning), new WeatherData(WeatherIcon.IconStorm, WeatherIcon.IconStorm, "stormy", ClimatesOfFerngill.Translator.Get("weather_stormy")) },

                { (int)(CurrentWeather.Rain | CurrentWeather.Lightning | CurrentWeather.Frost), new WeatherData(WeatherIcon.IconStorm, WeatherIcon.IconStorm, "stormyfrost", ClimatesOfFerngill.Translator.Get("weather_frost", new { conditions = ClimatesOfFerngill.Translator.Get("weather_stormy") })) },

                { (int)(CurrentWeather.Rain | CurrentWeather.Lightning | CurrentWeather.Heatwave), new WeatherData(WeatherIcon.IconStorm, WeatherIcon.IconStorm, "stormyheatwave", ClimatesOfFerngill.Translator.Get("weather_heatwaveCond", new { conditions = ClimatesOfFerngill.Translator.Get("weather_stormy") })) },

                { (int)(CurrentWeather.Rain | CurrentWeather.Fog), new WeatherData(WeatherIcon.IconRainFog, WeatherIcon.IconRain, "rainyfog", ClimatesOfFerngill.Translator.Get("weather_fog", new { condition = ClimatesOfFerngill.Translator.Get("weather_rainy") })) },

                { (int)(CurrentWeather.Rain | CurrentWeather.Lightning | CurrentWeather.Fog), new WeatherData(WeatherIcon.IconStorm, WeatherIcon.IconStorm, "stormyfog", ClimatesOfFerngill.Translator.Get("weather_fog", new { condition = ClimatesOfFerngill.Translator.Get("weather_stormy") })) },

                { (int)(CurrentWeather.Rain | CurrentWeather.Lightning | CurrentWeather.ThunderFrenzy), new WeatherData(WeatherIcon.IconThunderFrenzy, WeatherIcon.IconStorm, "lightfrenzyfog", ClimatesOfFerngill.Translator.Get("weather_frenzy")) },

                { (int)(CurrentWeather.Rain | CurrentWeather.Lightning | CurrentWeather.Frost | CurrentWeather.ThunderFrenzy), new WeatherData(WeatherIcon.IconThunderFrenzy, WeatherIcon.IconStorm, "lightfrenzyfrost", ClimatesOfFerngill.Translator.Get("weather_frost", new { condition = ClimatesOfFerngill.Translator.Get("weather_frenzy")})) },

                { (int)(CurrentWeather.Rain | CurrentWeather.Lightning | CurrentWeather.Heatwave | CurrentWeather.ThunderFrenzy), new WeatherData(WeatherIcon.IconThunderFrenzy, WeatherIcon.IconStorm, "lightfrenzyheatwave", ClimatesOfFerngill.Translator.Get("weather_heatwave", new { condition = ClimatesOfFerngill.Translator.Get("weather_frenzy") })) },

                { (int)(CurrentWeather.Rain | CurrentWeather.Lightning | CurrentWeather.Fog | CurrentWeather.ThunderFrenzy), new WeatherData(WeatherIcon.IconThunderFrenzy, WeatherIcon.IconStorm, "lightfrenzyfog", ClimatesOfFerngill.Translator.Get("weather_fog", new { condition = ClimatesOfFerngill.Translator.Get("weather_frenzy") })) },

                { (int)(CurrentWeather.Rain | CurrentWeather.Lightning | CurrentWeather.Fog | CurrentWeather.Frost | CurrentWeather.ThunderFrenzy), new WeatherData(WeatherIcon.IconThunderFrenzy, WeatherIcon.IconStorm, "lightfrenzyfogfrost", ClimatesOfFerngill.Translator.Get("weather_frostTwo", new { condition = ClimatesOfFerngill.Translator.Get("weather_frenzy"), condtitionB = ClimatesOfFerngill.Translator.Get("weather_fog_basic") })) },

                { (int)(CurrentWeather.Rain | CurrentWeather.Lightning | CurrentWeather.Fog | CurrentWeather.Heatwave | CurrentWeather.ThunderFrenzy), new WeatherData(WeatherIcon.IconThunderFrenzy, WeatherIcon.IconStorm, "lightfrenzyfogheatwave", ClimatesOfFerngill.Translator.Get("weather_heatwaveTwo", new { condition = ClimatesOfFerngill.Translator.Get("weather_frenzy"), condtitionB = ClimatesOfFerngill.Translator.Get("weather_fog_basic") })) },

                { (int)(CurrentWeather.Rain | CurrentWeather.Fog | CurrentWeather.Frost), new WeatherData(WeatherIcon.IconRainFog, WeatherIcon.IconRain, "rainyfrostfog", ClimatesOfFerngill.Translator.Get("weather_frostTwo", new { condition = ClimatesOfFerngill.Translator.Get("weather_rainy"), condtitionB = ClimatesOfFerngill.Translator.Get("weather_fog_basic") })) },

                { (int)(CurrentWeather.Rain | CurrentWeather.Lightning | CurrentWeather.Fog | CurrentWeather.Frost), new WeatherData(WeatherIcon.IconStorm, WeatherIcon.IconStorm, "stormyfrostfog", ClimatesOfFerngill.Translator.Get("weather_frost", new { condition = ClimatesOfFerngill.Translator.Get("weather_stormy"), conditionB = ClimatesOfFerngill.Translator.Get("weather_fog_basic") })) },

                { (int)(CurrentWeather.Rain | CurrentWeather.Fog | CurrentWeather.Heatwave), new WeatherData(WeatherIcon.IconRainFog, WeatherIcon.IconRain, "rainyheatwavefog", ClimatesOfFerngill.Translator.Get("weather_heatwaveTwo", new { condition = ClimatesOfFerngill.Translator.Get("weather_rainy"), conditionB = ClimatesOfFerngill.Translator.Get("weather_fog_basic") })) },

                { (int)(CurrentWeather.Rain | CurrentWeather.Lightning | CurrentWeather.Fog | CurrentWeather.Heatwave), new WeatherData(WeatherIcon.IconStormFog, WeatherIcon.IconStorm, "stormyheatwavefog", ClimatesOfFerngill.Translator.Get("weather_heatwaveTwo", new { condition = ClimatesOfFerngill.Translator.Get("weather_stormy"), conditionB = ClimatesOfFerngill.Translator.Get("weather_fog_basic") })) },

                { (int)(CurrentWeather.Fog | CurrentWeather.Heatwave), new WeatherData(WeatherIcon.IconSunnyFog, WeatherIcon.IconSunny, CondName: "fogheatwave", CondDesc: ClimatesOfFerngill.Translator.Get("weather_fog", new { condition = ClimatesOfFerngill.Translator.Get("weather_heatwave") })) },

                { (int)(CurrentWeather.Fog | CurrentWeather.Frost),  new WeatherData(WeatherIcon.IconSunnyFog, WeatherIcon.IconSunny, CondName: "fogfrost", CondDesc: ClimatesOfFerngill.Translator.Get("weather_frost", new { condition = ClimatesOfFerngill.Translator.Get("weather_fog_basic") }))  },

                { (int)(CurrentWeather.Rain | CurrentWeather.Heatwave), new WeatherData(WeatherIcon.IconRain, WeatherIcon.IconRain, "rainHeatwave", ClimatesOfFerngill.Translator.Get("weather_heatwaveCond", new { condition = ClimatesOfFerngill.Translator.Get("weather_rainy") })) },

                { (int)(CurrentWeather.Rain | CurrentWeather.Frost), new WeatherData(WeatherIcon.IconRain, WeatherIcon.IconRain, "rainFrost", ClimatesOfFerngill.Translator.Get("weather_frost", new { condition = ClimatesOfFerngill.Translator.Get("weather_rainy") })) },

                { (int)(CurrentWeather.Snow), new WeatherData(WeatherIcon.IconSnow, WeatherIcon.IconSnow, "snowy", ClimatesOfFerngill.Translator.Get("weather_snow")) },

                { (int)(CurrentWeather.Snow | CurrentWeather.Frost), new WeatherData(WeatherIcon.IconSnow, WeatherIcon.IconSnow, "snowyFrost", ClimatesOfFerngill.Translator.Get("weather_frost", new { condition = ClimatesOfFerngill.Translator.Get("weather_snow") })) },

                { (int)(CurrentWeather.Snow | CurrentWeather.Fog), new WeatherData(WeatherIcon.IconSnowFog, WeatherIcon.IconSnow, "snowyFog", ClimatesOfFerngill.Translator.Get("weather_fog", new { condition = ClimatesOfFerngill.Translator.Get("weather_snow") })) },

                { (int)(CurrentWeather.Snow | CurrentWeather.Fog | CurrentWeather.Frost), new WeatherData(WeatherIcon.IconSnowFog, WeatherIcon.IconSnow, "snowyFrostFog", ClimatesOfFerngill.Translator.Get("weather_frostTwo", new { condition = ClimatesOfFerngill.Translator.Get("weather_snow"), conditionB = ClimatesOfFerngill.Translator.Get("weather_fog_basic") })) },

                { (int)(CurrentWeather.Snow | CurrentWeather.Lightning), new WeatherData(WeatherIcon.IconThunderSnow, WeatherIcon.IconSnow, "thunderSnow",ClimatesOfFerngill.Translator.Get("weather_thundersnow")) },

                { (int)(CurrentWeather.Snow | CurrentWeather.Blizzard | CurrentWeather.Lightning | CurrentWeather.Frost), new WeatherData(WeatherIcon.IconThunderSnow, WeatherIcon.IconSnow, "blizzardThunderSnowFrost",ClimatesOfFerngill.Translator.Get("weather_thundersnow")) },
                
                { (int)(CurrentWeather.Snow | CurrentWeather.Blizzard | CurrentWeather.WhiteOut | CurrentWeather.Frost | CurrentWeather.Lightning), new WeatherData(WeatherIcon.IconThunderSnow, WeatherIcon.IconSnow, "whiteOutBlizzardFrostThunderSnow",ClimatesOfFerngill.Translator.Get("weather_thundersnow")) },
                
                { (int)(CurrentWeather.Snow | CurrentWeather.Blizzard | CurrentWeather.Lightning), new WeatherData(WeatherIcon.IconThunderSnow, WeatherIcon.IconSnow, "blizThunderSnow",ClimatesOfFerngill.Translator.Get("weather_thundersnow")) },
                
                { (int)(CurrentWeather.Snow | CurrentWeather.Blizzard | CurrentWeather.WhiteOut | CurrentWeather.Lightning), new WeatherData(WeatherIcon.IconThunderSnow, WeatherIcon.IconSnow, "whiteOutThunderSnow",ClimatesOfFerngill.Translator.Get("weather_thundersnow")) },
                
                { (int)(CurrentWeather.Snow | CurrentWeather.Lightning | CurrentWeather.Fog), new WeatherData(WeatherIcon.IconThunderSnowFog, WeatherIcon.IconSnow, "thunderSnowFog", ClimatesOfFerngill.Translator.Get("weather_fog", new { condition = ClimatesOfFerngill.Translator.Get("weather_thundersnow") })) },

                { (int)(CurrentWeather.Snow | CurrentWeather.Lightning | CurrentWeather.Fog | CurrentWeather.Frost), new WeatherData(WeatherIcon.IconThunderSnowFog, WeatherIcon.IconSnow, "thunderSnowFrostFog", ClimatesOfFerngill.Translator.Get("weather_frostTwo", new { condition = ClimatesOfFerngill.Translator.Get("weather_thundersnow"), conditionB = ClimatesOfFerngill.Translator.Get("weather_fog_basic") })) },

                { (int)(CurrentWeather.Snow | CurrentWeather.Lightning | CurrentWeather.Frost), new WeatherData(WeatherIcon.IconThunderSnowFog, WeatherIcon.IconSnow, "thunderSnowFrost", ClimatesOfFerngill.Translator.Get("weather_frost", new { condition = ClimatesOfFerngill.Translator.Get("weather_thundersnow") })) },

                { (int)(CurrentWeather.Festival), new WeatherData(WeatherIcon.IconFestival, WeatherIcon.IconFestival, "festival", ClimatesOfFerngill.Translator.Get("weather_festival")) },

                { (int)(CurrentWeather.Wedding), new WeatherData(WeatherIcon.IconWedding, WeatherIcon.IconWedding, "wedding", ClimatesOfFerngill.Translator.Get("weather_wedding")) }, 

                { (int)(CurrentWeather.Wind), new WeatherData(WeatherIcon.IconSpringDebris, WeatherIcon.IconSpringDebris, "debris", ClimatesOfFerngill.Translator.Get("weather_wind")) },

                { (int)(CurrentWeather.Wind | CurrentWeather.Frost), new WeatherData(WeatherIcon.IconSpringDebris, WeatherIcon.IconSpringDebris, "debrisfrost", ClimatesOfFerngill.Translator.Get("weather_frost", new { condition = ClimatesOfFerngill.Translator.Get("weather_wind") })) },

                { (int)(CurrentWeather.Wind | CurrentWeather.Heatwave), new WeatherData(WeatherIcon.IconSpringDebris, WeatherIcon.IconSpringDebris, "debrisheatwave", ClimatesOfFerngill.Translator.Get("weather_heatwaveCond", new { condition = ClimatesOfFerngill.Translator.Get("weather_wind") })) },

                { (int)(CurrentWeather.Wind | CurrentWeather.Lightning), new WeatherData(WeatherIcon.IconDryLightningWind, WeatherIcon.IconSpringDebris, "drylightningwindy", ClimatesOfFerngill.Translator.Get("weather_drylightningwindy")) },

                { (int)(CurrentWeather.Wind | CurrentWeather.Heatwave | CurrentWeather.Lightning), new WeatherData(WeatherIcon.IconDryLightningWind, WeatherIcon.IconSpringDebris, "drylightningheatwave", ClimatesOfFerngill.Translator.Get("weather_drylightningheatwavewindy")) },

                { (int)(CurrentWeather.Snow | CurrentWeather.Blizzard), new WeatherData(WeatherIcon.IconBlizzard, WeatherIcon.IconBlizzard, "blizzard", ClimatesOfFerngill.Translator.Get("weather_blizzard")) },

                { (int)(CurrentWeather.Snow | CurrentWeather.Blizzard | CurrentWeather.WhiteOut), new WeatherData(WeatherIcon.IconWhiteOut, WeatherIcon.IconWhiteOut, "whiteout", ClimatesOfFerngill.Translator.Get("weather_whiteOut")) },

                { (int)(CurrentWeather.Snow | CurrentWeather.Blizzard | CurrentWeather.Fog), new WeatherData(WeatherIcon.IconBlizzardFog, WeatherIcon.IconBlizzard, "blizzardFog", ClimatesOfFerngill.Translator.Get("weather_fog", new { condition = ClimatesOfFerngill.Translator.Get("weather_blizzard") })) },

                { (int)(CurrentWeather.Snow | CurrentWeather.Blizzard | CurrentWeather.WhiteOut | CurrentWeather.Fog), new WeatherData(WeatherIcon.IconWhiteOutFog, WeatherIcon.IconWhiteOut, "whiteoutFog", ClimatesOfFerngill.Translator.Get("weather_fog", new { condition = ClimatesOfFerngill.Translator.Get("weather_whiteOut") })) },

                { (int)(CurrentWeather.Snow | CurrentWeather.Blizzard | CurrentWeather.Frost), new WeatherData(WeatherIcon.IconBlizzard, WeatherIcon.IconBlizzard, "blizzardFog", ClimatesOfFerngill.Translator.Get("weather_frost", new { condition = ClimatesOfFerngill.Translator.Get("weather_blizzard") })) },

                { (int)(CurrentWeather.Snow | CurrentWeather.Blizzard | CurrentWeather.WhiteOut | CurrentWeather.Frost), new WeatherData(WeatherIcon.IconWhiteOut, WeatherIcon.IconWhiteOut, "whiteoutFog", ClimatesOfFerngill.Translator.Get("weather_frost", new { condition = ClimatesOfFerngill.Translator.Get("weather_whiteOut") })) },

                { (int)(CurrentWeather.BloodMoon | CurrentWeather.Frost | CurrentWeather.Fog), new WeatherData(WeatherIcon.IconBloodMoon, WeatherIcon.IconBloodMoon, "bloodMoonFrostFog", ClimatesOfFerngill.Translator.Get("weather_fog", new {condition = ClimatesOfFerngill.Translator.Get("weather_bloodmoon")}) )},

                { (int)(CurrentWeather.BloodMoon |CurrentWeather.Heatwave | CurrentWeather.Fog), new WeatherData(WeatherIcon.IconBloodMoon, WeatherIcon.IconBloodMoon, "bloodMoonHWFog", ClimatesOfFerngill.Translator.Get("weather_fog", new {condition = ClimatesOfFerngill.Translator.Get("weather_bloodmoon")}) )},

                { (int)(CurrentWeather.BloodMoon | CurrentWeather.Fog), new WeatherData(WeatherIcon.IconBloodMoon, WeatherIcon.IconBloodMoon, "bloodMoonFog", ClimatesOfFerngill.Translator.Get("weather_fog", new {condition = ClimatesOfFerngill.Translator.Get("weather_bloodmoon")}) )},
                { (int)(CurrentWeather.BloodMoon), new WeatherData(WeatherIcon.IconBloodMoon, WeatherIcon.IconBloodMoon, "bloodMoon", ClimatesOfFerngill.Translator.Get("weather_bloodmoon"))},

                { (int)(CurrentWeather.BloodMoon| CurrentWeather.Heatwave), new WeatherData(WeatherIcon.IconBloodMoon, WeatherIcon.IconBloodMoon, "bloodMoonHeatwave", ClimatesOfFerngill.Translator.Get("weather_bloodmoon"))},

                { (int)(CurrentWeather.BloodMoon| CurrentWeather.Frost), new WeatherData(WeatherIcon.IconBloodMoon, WeatherIcon.IconBloodMoon, "bloodMoonFrost", ClimatesOfFerngill.Translator.Get("weather_bloodmoon"))},

                { (int)(CurrentWeather.Sandstorm), new WeatherData(WeatherIcon.IconSandstorm, WeatherIcon.IconSandstorm, "sandstorm", ClimatesOfFerngill.Translator.Get("weather_sandstorm"))},
                { (int)(CurrentWeather.Sandstorm | CurrentWeather.Wind), new WeatherData(WeatherIcon.IconSandstorm, WeatherIcon.IconSandstorm, "sandstormWind", ClimatesOfFerngill.Translator.Get("weather_sandstorm"))},
                { (int)(CurrentWeather.Sandstorm | CurrentWeather.Wind | CurrentWeather.Heatwave), new WeatherData(WeatherIcon.IconSandstorm, WeatherIcon.IconSandstorm, "sandstormWindHW", ClimatesOfFerngill.Translator.Get("weather_sandstorm"))},
                { (int)(CurrentWeather.Sandstorm | CurrentWeather.Wind | CurrentWeather.Frost), new WeatherData(WeatherIcon.IconSandstorm, WeatherIcon.IconSandstorm, "sandstormWindFrost", ClimatesOfFerngill.Translator.Get("weather_sandstorm"))},
                { (int)(CurrentWeather.Sandstorm | CurrentWeather.Sunny), new WeatherData(WeatherIcon.IconSandstorm, WeatherIcon.IconSandstorm, "sandstorm", ClimatesOfFerngill.Translator.Get("weather_sandstorm"))},
                { (int)(CurrentWeather.Sandstorm | CurrentWeather.Wind | CurrentWeather.Sunny), new WeatherData(WeatherIcon.IconSandstorm, WeatherIcon.IconSandstorm, "sandstormWind", ClimatesOfFerngill.Translator.Get("weather_sandstorm"))},
                { (int)(CurrentWeather.Sandstorm | CurrentWeather.Wind | CurrentWeather.Heatwave| CurrentWeather.Sunny), new WeatherData(WeatherIcon.IconSandstorm, WeatherIcon.IconSandstorm, "sandstormWindHW", ClimatesOfFerngill.Translator.Get("weather_sandstorm"))},
                { (int)(CurrentWeather.Sandstorm | CurrentWeather.Wind | CurrentWeather.Frost| CurrentWeather.Sunny), new WeatherData(WeatherIcon.IconSandstorm, WeatherIcon.IconSandstorm, "sandstormWindFrost", ClimatesOfFerngill.Translator.Get("weather_sandstorm"))},
                { (int)(CurrentWeather.Rain | CurrentWeather.Overcast), new WeatherData(WeatherIcon.IconOvercast, WeatherIcon.IconOvercast, "overcast", ClimatesOfFerngill.Translator.Get("weather_overcast"))},
                { (int)(CurrentWeather.Rain | CurrentWeather.Overcast | CurrentWeather.Heatwave), new WeatherData(WeatherIcon.IconOvercast, WeatherIcon.IconOvercast, "overcastHeatwave", ClimatesOfFerngill.Translator.Get("weather_overcast"))},
                { (int)(CurrentWeather.Rain | CurrentWeather.Overcast | CurrentWeather.Frost), new WeatherData(WeatherIcon.IconOvercast, WeatherIcon.IconOvercast, "overcastFrost", ClimatesOfFerngill.Translator.Get("weather_overcast"))},
                { (int)(CurrentWeather.Rain | CurrentWeather.Overcast | CurrentWeather.Fog | CurrentWeather.Heatwave), new WeatherData(WeatherIcon.IconOvercast, WeatherIcon.IconOvercast, "overcastHeatwaveFog", ClimatesOfFerngill.Translator.Get("weather_overcast"))},
                { (int)(CurrentWeather.Rain | CurrentWeather.Overcast | CurrentWeather.Frost | CurrentWeather.Fog), new WeatherData(WeatherIcon.IconOvercast, WeatherIcon.IconOvercast, "overcastFrostFog", ClimatesOfFerngill.Translator.Get("weather_overcast"))},

    };
        }

        /// *************************************************************************
        /// ACCESS METHODS
        /// *************************************************************************
        public CurrentWeather GetCurrentConditions()
        {
            return CurrentConditionsN;
        }

        public void ClearAllSpecialWeather()
        {
            foreach (ISDVWeather weather in CurrentWeathers)
                weather.EndWeather();
        }

        public void DrawWeathers()
        {
            foreach (ISDVWeather weather in CurrentWeathers)
                weather.DrawWeather();

            //if it's a blood moon out..
            if (ClimatesOfFerngill.MoonAPI != null && ClimatesOfFerngill.MoonAPI.GetCurrentMoonPhase() == "Blood Moon")
            {
                if (this.GetWeatherMatchingType("Fog").First().IsWeatherVisible)
                {
                    //Get fog instance
                    FerngillFog ourFog = (FerngillFog)this.GetWeatherMatchingType("Fog").First();
                    ourFog.BloodMoon = true;
                }

                if (this.GetWeatherMatchingType("Blizzard").First().IsWeatherVisible)
                {
                    //Get Blizzard instance
                    FerngillBlizzard blizzard = (FerngillBlizzard)this.GetWeatherMatchingType("Blizzard").First();
                    blizzard.IsBloodMoon = true;
                }
            }
        }

        public void MoveWeathers()
        {
            foreach (ISDVWeather weather in CurrentWeathers)
                weather.MoveWeather();
        }
        
        public string FogDescription(double fogRoll, double fogChance)
        {
            string desc = "";
            foreach (ISDVWeather weather in CurrentWeathers)
            {
                if (weather.WeatherType == "Fog")
                {
                    FerngillFog fog = (FerngillFog)weather;
                    desc += fog.FogDescription(fogRoll, fogChance);
                }
            }

            return desc;
        }

        public void CreateWeather(string Type)
        {
            foreach (ISDVWeather weather in CurrentWeathers)
            {
                if (weather.WeatherType == Type)
                    weather.CreateWeather();
            }
        }

        public void TenMinuteUpdate()
        {
            foreach (ISDVWeather weather in CurrentWeathers)
            {
                weather.UpdateWeather();
            }

            //update fog for the evening
            if (SDVTime.CurrentTimePeriod == SDVTimePeriods.Afternoon && GenerateEveningFog && !HasSetEveningFog && (!IsFestivalToday && !IsWeddingToday))
            {
                //Get fog instance
                FerngillFog ourFog = (FerngillFog)this.GetWeatherMatchingType("Fog").First();
                if (!ourFog.WeatherInProgress)
                {
                    ourFog.SetEveningFog();
                    this.GenerateWeatherSync();
                    HasSetEveningFog = true;
                }
            }
        }

        public void SecondUpdate()
        {
            foreach (ISDVWeather weather in CurrentWeathers)
            {
                weather.SecondUpdate();
            }
        }

        internal List<ISDVWeather> GetWeatherMatchingType(string type)
        {
            List<ISDVWeather> Weathers = new List<ISDVWeather>();
            foreach (ISDVWeather weather in CurrentWeathers)
            {
                if (weather.WeatherType == type)
                    Weathers.Add(weather);
            }

            return Weathers;
        }

        /// <summary>Rather than track the weather seprately, always get it from the game.</summary>
        public CurrentWeather TommorowForecast => ConvertToCurrentWeather(Game1.weatherForTomorrow);

        public bool IsTodayTempSet => TodayTemps != null;
        public bool IsTomorrowTempSet => TomorrowTemps != null;
        public bool IsFestivalToday => CurrentConditionsN.HasFlag(CurrentWeather.Festival);
        public bool IsWeddingToday => CurrentConditionsN.HasFlag(CurrentWeather.Wedding);

        /// <summary> This returns the high for today </summary>
        public double TodayHigh => TodayTemps.HigherBound;

        /// <summary> This returns the high for tomorrow </summary>
        public double TomorrowHigh => TomorrowTemps.HigherBound;

        /// <summary> This returns the low for today </summary>
        public double TodayLow => TodayTemps.LowerBound;

        /// <summary> This returns the low for tomorrow </summary>
        public double TomorrowLow => TomorrowTemps.LowerBound;

        public void AddWeather(CurrentWeather newWeather)
        {
            //sanity remove these once weather is set.
            CurrentConditionsN = CurrentConditionsN.RemoveFlags(CurrentWeather.Unset);

            //Some flags are contradictory. Fix that here.
            if (newWeather == CurrentWeather.Rain)
            {
                //unset debris, sunny, snow and blizzard, if it's raining.
                CurrentConditionsN = CurrentConditionsN.RemoveFlags(CurrentWeather.Sunny);
                CurrentConditionsN = CurrentConditionsN.RemoveFlags(CurrentWeather.Snow);
                CurrentConditionsN = CurrentConditionsN.RemoveFlags(CurrentWeather.Blizzard);
                CurrentConditionsN = CurrentConditionsN.RemoveFlags(CurrentWeather.Wind);

                CurrentConditionsN |= newWeather;
            }

            else if (newWeather == CurrentWeather.Sunny)
            {
                //unset debris, rain, snow and blizzard, if it's sunny.
                CurrentConditionsN = CurrentConditionsN.RemoveFlags(CurrentWeather.Rain);
                CurrentConditionsN = CurrentConditionsN.RemoveFlags(CurrentWeather.Snow);
                CurrentConditionsN = CurrentConditionsN.RemoveFlags(CurrentWeather.Blizzard);
                CurrentConditionsN = CurrentConditionsN.RemoveFlags(CurrentWeather.Wind);

                CurrentConditionsN |= newWeather;
            }

            else if (newWeather == CurrentWeather.Wind)
            {
                //unset sunny, rain, snow and blizzard, if it's debris.
                CurrentConditionsN = CurrentConditionsN.RemoveFlags(CurrentWeather.Rain);
                CurrentConditionsN = CurrentConditionsN.RemoveFlags(CurrentWeather.Snow);
                CurrentConditionsN = CurrentConditionsN.RemoveFlags(CurrentWeather.Blizzard);
                CurrentConditionsN = CurrentConditionsN.RemoveFlags(CurrentWeather.Sunny);

                CurrentConditionsN |= newWeather;
            }

            else if (newWeather == CurrentWeather.Snow)
            {
                //unset debris, sunny, snow and blizzard, if it's raining.
                CurrentConditionsN = CurrentConditionsN.RemoveFlags(CurrentWeather.Sunny);
                CurrentConditionsN = CurrentConditionsN.RemoveFlags(CurrentWeather.Rain);
                CurrentConditionsN = CurrentConditionsN.RemoveFlags(CurrentWeather.Wind);

                CurrentConditionsN |= newWeather;
            }

            else if (newWeather == CurrentWeather.Frost)
            {
                CurrentConditionsN.RemoveFlags(CurrentWeather.Heatwave);

                CurrentConditionsN |= newWeather;
            }

            else if (newWeather == CurrentWeather.Heatwave)
            {
                CurrentConditionsN = CurrentConditionsN.RemoveFlags(CurrentWeather.Frost);

                CurrentConditionsN |= newWeather;
            }

            else if (newWeather == CurrentWeather.Wedding || newWeather == CurrentWeather.Festival)
            {
                CurrentConditionsN = newWeather; //Clear *everything else* if it's a wedding or festival.
            }

            else
                CurrentConditionsN |= newWeather;
        }

        internal void ForceEveningFog()
        {
            //Get fog instance
            List<ISDVWeather> fogWeather = this.GetWeatherMatchingType("Fog");
            foreach (ISDVWeather weat in fogWeather)
            {
                SDVTime BeginTime, ExpirTime;
                BeginTime = new SDVTime(Game1.getStartingToGetDarkTime());
                BeginTime.AddTime(ClimatesOfFerngill.Dice.Next(-15, 90));

                ExpirTime = new SDVTime(BeginTime);
                ExpirTime.AddTime(ClimatesOfFerngill.Dice.Next(120, 310));

                BeginTime.ClampToTenMinutes();
                ExpirTime.ClampToTenMinutes();
                weat.SetWeatherTime(BeginTime, ExpirTime);
            }
        }

        /// <summary> Syntatic Sugar for Enum.HasFlag(). Done so if I choose to rewrite how it's accessed, less rewriting of invoking functions is needed. </summary>
        /// <param name="checkWeather">The weather being checked.</param>
        /// <returns>If the weather is present</returns>
        public bool HasWeather(CurrentWeather checkWeather)
        {
            return CurrentConditionsN.HasFlag(checkWeather);
        }

        public bool HasPrecip()
        {
            if (CurrentConditionsN.HasAnyFlags(CurrentWeather.Snow | CurrentWeather.Rain | CurrentWeather.Blizzard))
                return true;

            return false;
        }       
  
        private void ProcessWeatherChanges(object sender, WeatherNotificationArgs e)
        {
            if (e.Weather == "WhiteOut")
            {
                if (e.Present)
                {
                    CurrentConditionsN |= CurrentWeather.WhiteOut;
                    this.GenerateWeatherSync();
                }
                else
                {
                    CurrentConditionsN = CurrentConditionsN.RemoveFlags(CurrentWeather.WhiteOut);
                    this.GenerateWeatherSync();
                }
            }

            if (e.Weather == "ThunderFrenzy")
            {
                if (e.Present)
                {
                    CurrentConditionsN |= CurrentWeather.ThunderFrenzy;
                    this.GenerateWeatherSync();
                }
                else
                {
                    CurrentConditionsN = CurrentConditionsN.RemoveFlags(CurrentWeather.ThunderFrenzy);
                    this.GenerateWeatherSync();
                }
            }

            if (e.Weather == "Sandstorm")
            {
                if (e.Present)
                { 
                    CurrentConditionsN |= CurrentWeather.Sandstorm;
                    this.GenerateWeatherSync();
                }
                else
                {
                    CurrentConditionsN = CurrentConditionsN.RemoveFlags(CurrentWeather.Sandstorm);
                    this.GenerateWeatherSync();
                }
            }

            if (e.Weather == "Fog")
            {
                if (e.Present)
                {
                    CurrentConditionsN |= CurrentWeather.Fog;
                    this.GenerateWeatherSync();
                }
                else
                {
                    CurrentConditionsN = CurrentConditionsN.RemoveFlags(CurrentWeather.Fog);
                    this.GenerateWeatherSync();
                }
            }

            if (e.Weather == "Blizzard")
            {
  
                if (e.Present) { 
                    CurrentConditionsN |= CurrentWeather.Blizzard;
                    this.GenerateWeatherSync();
                }

                else { 
                    CurrentConditionsN = CurrentConditionsN.RemoveFlags(CurrentWeather.Blizzard);
                    this.GenerateWeatherSync();
                }
            }
        }

        internal bool GetWeatherStatus(string weather)
        {
            foreach (ISDVWeather w in this.CurrentWeathers)
            {
                if (w.WeatherType == weather)
                    return w.WeatherInProgress;
            }
            return false;
        }

        internal SDVTime GetWeatherBeginTime(string weather)
        {
            foreach (ISDVWeather w in this.CurrentWeathers)
            {
                if (w.WeatherType == weather)
                    return w.WeatherBeginTime;
            }

            return new SDVTime(0600);
        }

        internal SDVTime GetWeatherEndTime(string weather)
        {
            foreach (ISDVWeather w in this.CurrentWeathers)
            {
                if (w.WeatherType == weather)
                    return w.WeatherExpirationTime;
            }

            return new SDVTime(0600);
        }

        internal void SetWeatherBeginTime(string weather, int weatherTime)
        {
            foreach (ISDVWeather w in this.CurrentWeathers)
            {
                if (w.WeatherType == weather)
                    w.SetWeatherBeginTime(new SDVTime(weatherTime));
            }
        }

        internal void SetWeatherEndTime(string weather, int weatherTime)
        {
            foreach (ISDVWeather w in this.CurrentWeathers)
            {
                if (w.WeatherType == weather)
                    w.SetWeatherExpirationTime(new SDVTime(weatherTime));
            }
        }

        public void GenerateWeatherSync()
        {
            if (!Context.IsMainPlayer)
                return;

            WeatherSync Message = new WeatherSync
            {
                weatherType = WeatherUtilities.GetWeatherCode(),
                isFoggy = GetWeatherStatus("Fog"),
                isThunderFrenzy = GetWeatherStatus("ThunderFrenzy"),
                isWhiteOut = GetWeatherStatus("WhiteOut"),
                isBlizzard = GetWeatherStatus("Blizzard"),
                isSandstorm = GetWeatherStatus("Sandstorm"),
                isVariableRain = ClimatesOfFerngill.IsVariableRain,
                isOvercast = ((CurrentConditionsN & CurrentWeather.Overcast) != 0),
                rainAmt = ClimatesOfFerngill.AmtOfRainDrops,
                fogWeatherBeginTime = GetWeatherBeginTime("Fog").ReturnIntTime(),
                thunWeatherBeginTime = GetWeatherBeginTime("ThunderFrenzy").ReturnIntTime(),
                blizzWeatherBeginTime = GetWeatherBeginTime("Blizzard").ReturnIntTime(),
                whiteWeatherBeginTime = GetWeatherBeginTime("WhiteOut").ReturnIntTime(),
                sandstormWeatherBeginTime = GetWeatherBeginTime("Sandstorm").ReturnIntTime(),
                sandstormWeatherEndTime = GetWeatherEndTime("Sandstorm").ReturnIntTime(),
                fogWeatherEndTime = GetWeatherEndTime("Fog").ReturnIntTime(),
                thunWeatherEndTime = GetWeatherEndTime("ThunderFrenzy").ReturnIntTime(),
                blizzWeatherEndTime = GetWeatherEndTime("Blizzard").ReturnIntTime(),
                whiteWeatherEndTime = GetWeatherEndTime("WhiteOut").ReturnIntTime(),
                todayHigh = TodayTemps.HigherBound,
                todayLow = TodayTemps.LowerBound
            };    

            ClimatesOfFerngill.MPHandler.SendMessage<WeatherSync>(Message, "WeatherSync", new [] { "KoihimeNakamura.ClimatesOfFerngill" });    
        }

        public WeatherSync GenerateWeatherSyncMessage()
        {
            WeatherSync Message = new WeatherSync
            {
                weatherType = WeatherUtilities.GetWeatherCode(),
                isFoggy = GetWeatherStatus("Fog"),
                isThunderFrenzy = GetWeatherStatus("ThunderFrenzy"),
                isWhiteOut = GetWeatherStatus("WhiteOut"),
                isBlizzard = GetWeatherStatus("Blizzard"),
                isSandstorm = GetWeatherStatus("Sandstorm"),
                isOvercast = ((CurrentConditionsN & CurrentWeather.Overcast) != 0),
                isVariableRain = ClimatesOfFerngill.IsVariableRain,
                rainAmt = ClimatesOfFerngill.AmtOfRainDrops,
                fogWeatherBeginTime = GetWeatherBeginTime("Fog").ReturnIntTime(),
                thunWeatherBeginTime = GetWeatherBeginTime("ThunderFrenzy").ReturnIntTime(),
                blizzWeatherBeginTime = GetWeatherBeginTime("Blizzard").ReturnIntTime(),
                whiteWeatherBeginTime = GetWeatherBeginTime("WhiteOut").ReturnIntTime(),
                sandstormWeatherBeginTime = GetWeatherBeginTime("Sandstorm").ReturnIntTime(),
                sandstormWeatherEndTime = GetWeatherEndTime("Sandstorm").ReturnIntTime(),
                fogWeatherEndTime = GetWeatherEndTime("Fog").ReturnIntTime(),
                thunWeatherEndTime = GetWeatherEndTime("ThunderFrenzy").ReturnIntTime(),
                blizzWeatherEndTime = GetWeatherEndTime("Blizzard").ReturnIntTime(),
                whiteWeatherEndTime = GetWeatherEndTime("WhiteOut").ReturnIntTime(),
                todayHigh = TodayTemps.HigherBound,
                todayLow = TodayTemps.LowerBound,
                tommorowHigh = TomorrowHigh,
                tommorowLow = TomorrowLow
            };

            return Message;
        }

        public void ForceWeatherStart(string s)
        {
            foreach (ISDVWeather w in this.CurrentWeathers)
            {
                if (w.WeatherType == s)
                    w.ForceWeatherStart();
            }
        }

        public void SetSync(WeatherSync ws)
        {
            //set general weather first, then the specialized weathers
            switch (ws.weatherType)
            {
                case Game1.weather_sunny:
                    WeatherUtilities.SetWeatherSunny();
                    break;
                case Game1.weather_debris:
                    WeatherUtilities.SetWeatherDebris();
                    break;
                case Game1.weather_snow:
                    WeatherUtilities.SetWeatherSnow();
                    break;
                case Game1.weather_rain:
                    WeatherUtilities.SetWeatherRain();
                    break;
                case Game1.weather_lightning:
                    WeatherUtilities.SetWeatherStorm();
                    break;
                default:
                    WeatherUtilities.SetWeatherSunny();
                    break;
            }

            Game1.updateWeatherIcon();

            if (ws.isFoggy && ws.fogWeatherEndTime == Game1.timeOfDay)
                ForceWeatherEnd("Fog");


            if (ws.isBlizzard && ws.blizzWeatherEndTime == Game1.timeOfDay)
                ForceWeatherEnd("Blizzard");

            //yay, force set weathers!
            if (ws.isFoggy && (ws.fogWeatherBeginTime >= Game1.timeOfDay && ws.fogWeatherEndTime < Game1.timeOfDay))
            {
                ForceWeatherStart("Fog");
                SetWeatherBeginTime("Fog", ws.fogWeatherBeginTime);
                SetWeatherEndTime("Fog", ws.fogWeatherEndTime);
            }

            if (ws.isBlizzard && (ws.blizzWeatherBeginTime >= Game1.timeOfDay && ws.blizzWeatherEndTime < Game1.timeOfDay))
            {
                ForceWeatherStart("Blizzard");
                SetWeatherBeginTime("Blizzard", ws.blizzWeatherBeginTime);
                SetWeatherEndTime("Blizzard", ws.blizzWeatherEndTime);
            }

            if (ws.isWhiteOut && (ws.whiteWeatherBeginTime >= Game1.timeOfDay && ws.whiteWeatherEndTime < Game1.timeOfDay))
            {
                ForceWeatherStart("WhiteOut");
                SetWeatherBeginTime("WhiteOut", ws.whiteWeatherBeginTime);
                SetWeatherEndTime("WhiteOut", ws.whiteWeatherEndTime);
            }

            if (ws.isThunderFrenzy && (ws.thunWeatherBeginTime >= Game1.timeOfDay && ws.thunWeatherEndTime < Game1.timeOfDay)))
            {
                SetWeatherBeginTime("ThunderFrenzy", ws.thunWeatherBeginTime);
                SetWeatherEndTime("ThunderFrenzy", ws.thunWeatherEndTime);
            }

            if (ws.isSandstorm && (ws.sandstormWeatherBeginTime >= Game1.timeOfDay && ws.sandstormWeatherEndTime < Game1.timeOfDay))))
            {
                ForceWeatherStart("Sandstorm");
                SetWeatherBeginTime("Sandstorm", ws.sandstormWeatherBeginTime);
                SetWeatherEndTime("Sandstorm", ws.sandstormWeatherEndTime);
            }

            if (ws.isOvercast)
            {
                ClimatesOfFerngill.SetRainAmt(0);
                CurrentConditionsN |= CurrentWeather.Overcast;
                ClimatesOfFerngill.SetVariableRain(false);
            }
            
            if (ws.isVariableRain)
            {
                ClimatesOfFerngill.SetRainAmt(ws.rainAmt);
                ClimatesOfFerngill.SetVariableRain(ws.isVariableRain);
            }

            if (TodayTemps is null)
            {
                TodayTemps = new RangePair();
            }
            TodayTemps.HigherBound = ws.todayHigh;
            TodayTemps.LowerBound = ws.todayLow;
            SetTomorrowTemps(new RangePair(ws.tommorowLow,ws.tommorowHigh));
            //update tracker object

            ClimatesOfFerngill.Conditions.SetTodayWeather();
        }

        public string PrintWeather()
        {
            string s = "";
            foreach(ISDVWeather w in this.CurrentWeathers)
            {
                s += w.DebugWeatherOutput();
                s += Environment.NewLine;
            }

            return s;
        }

        public bool ContainsCondition(CurrentWeather cond)
        {
            if (CurrentConditionsN.HasFlag(cond))
            {
                return true;
            }

            return false;
        }

        /// ******************************************************************************
        /// PROCESSING
        /// ******************************************************************************
        internal void ForceTodayTemps(double high, double low)
        {
            if (TodayTemps is null)
                TodayTemps = new RangePair();

            TodayTemps.HigherBound = high;
            TodayTemps.LowerBound = low;
            this.GenerateWeatherSync();
        }

        /// <summary>This function resets the weather for a new day.</summary>
        public void OnNewDay()
        {
            foreach (ISDVWeather weather in CurrentWeathers)
                weather.OnNewDay();

            CurrentConditionsN = CurrentWeather.Unset;
            //Formerly, if tomorrow was null, we'd just allow nulls. Now we don't. 
            if (TomorrowTemps == null) { 
                ClimatesOfFerngill.GetTodayTemps();
                ClimatesOfFerngill.GetTomorrowTemps();
            }
            else { 
                TodayTemps = TomorrowTemps; //If Tomorrow is null, should just allow it to be null.
                ClimatesOfFerngill.GetTomorrowTemps();
            }

            if (Game1.currentSeason == "fall")
            {
                Weathers[(int)CurrentWeather.Wind] = new WeatherData(WeatherIcon.IconDebris, WeatherIcon.IconDebris, "debris", ClimatesOfFerngill.Translator.Get("weather_wind"));
                Weathers[(int)(CurrentWeather.Wind | CurrentWeather.Frost)] = new WeatherData(WeatherIcon.IconDebris, WeatherIcon.IconDebris, "debrisfrost", ClimatesOfFerngill.Translator.Get("weather_frost", new { condition = ClimatesOfFerngill.Translator.Get("weather_wind") }));
                Weathers[(int)(CurrentWeather.Wind | CurrentWeather.Heatwave)] = new WeatherData(WeatherIcon.IconDebris, WeatherIcon.IconDebris, "debrisheatwave", ClimatesOfFerngill.Translator.Get("weather_heatwaveCond", new { condition = ClimatesOfFerngill.Translator.Get("weather_wind") }));
                Weathers[(int)(CurrentWeather.Wind | CurrentWeather.Lightning)] = new WeatherData(WeatherIcon.IconDebris, WeatherIcon.IconDebris, "drylightningwindy", ClimatesOfFerngill.Translator.Get("weather_drylightningwindy"));
                Weathers[(int)(CurrentWeather.Wind | CurrentWeather.Heatwave | CurrentWeather.Lightning)] = new WeatherData(WeatherIcon.IconDebris, WeatherIcon.IconDebris, "drylightningheatwave", ClimatesOfFerngill.Translator.Get("weather_drylightningheatwavewindy"));
            }
            else
            {
                //reset back to spring. 
                Weathers[(int)CurrentWeather.Wind] = new WeatherData(WeatherIcon.IconSpringDebris, WeatherIcon.IconSpringDebris, "debris", ClimatesOfFerngill.Translator.Get("weather_wind"));
                Weathers[(int)(CurrentWeather.Wind | CurrentWeather.Frost)] = new WeatherData(WeatherIcon.IconSpringDebris, WeatherIcon.IconSpringDebris, "debrisfrost", ClimatesOfFerngill.Translator.Get("weather_frost", new { condition = ClimatesOfFerngill.Translator.Get("weather_wind") }));
                Weathers[(int)(CurrentWeather.Wind | CurrentWeather.Heatwave)] = new WeatherData(WeatherIcon.IconSpringDebris, WeatherIcon.IconSpringDebris, "debrisheatwave", ClimatesOfFerngill.Translator.Get("weather_heatwaveCond", new { condition = ClimatesOfFerngill.Translator.Get("weather_wind") }));
                Weathers[(int)(CurrentWeather.Wind | CurrentWeather.Lightning)] = new WeatherData(WeatherIcon.IconSpringDebris, WeatherIcon.IconSpringDebris, "drylightningwindy", ClimatesOfFerngill.Translator.Get("weather_drylightningwindy"));
                Weathers[(int)(CurrentWeather.Wind | CurrentWeather.Heatwave | CurrentWeather.Lightning)] = new WeatherData(WeatherIcon.IconSpringDebris, WeatherIcon.IconSpringDebris, "drylightningheatwave", ClimatesOfFerngill.Translator.Get("weather_drylightningheatwavewindy"));
            }

            this.GenerateWeatherSync();
        }

        /// <summary> This function resets the weather object to basic. </summary>
        public void Reset()
        {
            foreach (ISDVWeather weather in CurrentWeathers)
                weather.Reset();

            TodayTemps = null;
            TomorrowTemps = null;
            CurrentConditionsN = CurrentWeather.Unset;
        }

        public SDVTime GetFogTime()
        {
            foreach (ISDVWeather weather in CurrentWeathers)
            {
                if (weather is FerngillFog f)
                {
                    if (f.IsWeatherVisible)
                        return f.WeatherExpirationTime;
                    else
                        return new SDVTime(600);
                }
            }

            return new SDVTime(600);
        }

        /// <summary> This sets the temperatures from outside for today </summary>
        /// <param name="a">The RangePair that contains the generated temperatures</param>
        public void SetTodayTemps(RangePair a) => TodayTemps = new RangePair(a, EnforceHigherOverLower: true);

        /// <summary> This sets the temperatures from outside for tomorrow</summary>
        /// <param name="a">The RangePair that contains the generated temperatures</param>
        public void SetTomorrowTemps(RangePair a) => TomorrowTemps = new RangePair(a, EnforceHigherOverLower: true);

        /// ***************************************************************************
        /// Utility functions
        /// ***************************************************************************
        
        ///<summary> This function converts from the game weather back to the CurrentWeather enum. Intended primarily for use with tommorow's forecasted weather.</summary>
        internal static CurrentWeather ConvertToCurrentWeather(int weather)
        { 
            if (weather == Game1.weather_rain)
                return CurrentWeather.Rain;
            else if (weather == Game1.weather_festival)
                return CurrentWeather.Festival;
            else if (weather == Game1.weather_wedding)
                return CurrentWeather.Wedding;
            else if (weather == Game1.weather_debris)
                return CurrentWeather.Wind;
            else if (weather == Game1.weather_snow)
                return CurrentWeather.Snow;
            else if (weather == Game1.weather_lightning)
                return CurrentWeather.Rain | CurrentWeather.Lightning;

            //default return.
            return CurrentWeather.Sunny;
        }

        internal void SetTodayWeather()
        {      
            CurrentConditionsN = CurrentWeather.Unset; //reset the flag.

            if (!Game1.isDebrisWeather && !Game1.isRaining && !Game1.isSnowing)
            {
                AddWeather(CurrentWeather.Sunny);
            }

            if (Game1.isRaining)
                AddWeather(CurrentWeather.Rain);
            if (Game1.isDebrisWeather)
                AddWeather(CurrentWeather.Wind);
            if (Game1.isLightning)
                AddWeather(CurrentWeather.Lightning);
            if (Game1.isSnowing)
                AddWeather(CurrentWeather.Snow);

            if (Utility.isFestivalDay(SDate.Now().Day, SDate.Now().Season))
                AddWeather(CurrentWeather.Festival);

            if (Game1.weddingToday)
                AddWeather(CurrentWeather.Wedding);

            //check current weathers.
            foreach (ISDVWeather weat in CurrentWeathers)
            {
                if (weat.WeatherType == "Fog" && weat.IsWeatherVisible)
                    CurrentConditionsN |= CurrentWeather.Fog;
                if (weat.WeatherType == "Blizzard" && weat.IsWeatherVisible)
                    CurrentConditionsN |= CurrentWeather.Blizzard;
                if (weat.WeatherType == "WhiteOut" && weat.IsWeatherVisible)
                    CurrentConditionsN |= CurrentWeather.WhiteOut;
            }
        }

        /// <summary>
        /// This function returns a description of the object. A very important note that this is meant for debugging, and as such does not do localization.
        /// </summary>
        /// <returns>A string describing the object.</returns>
        public override string ToString()
        {
            string ret = "";
            ret += $"Low for today is {TodayTemps?.LowerBound:N3} with the high being {TodayTemps?.HigherBound:N3}. The current conditions are {Weathers[(int)CurrentConditionsN].ConditionName}.";

            foreach (ISDVWeather weather in CurrentWeathers)
                ret += weather.ToString() + Environment.NewLine;
            
            ret += $"Weather set for tommorow is {Weathers[(int)(WeatherConditions.ConvertToCurrentWeather(Game1.weatherForTomorrow))].ConditionName} with high {TomorrowTemps?.HigherBound:N3} and low {TomorrowTemps?.LowerBound:N3}. Evening fog generated {GenerateEveningFog} ";

            return ret;
        }
        
        internal bool TestForSpecialWeather(FerngillClimateTimeSpan ClimateForDay, ClimateTracker model)
        {
            bool specialWeatherTriggered = false;
            // Conditions: Blizzard - occurs in weather_snow in "winter"
            //             Dry Lightning - occurs if it's sunny in any season if temps exceed 25C.
            //             Frost and Heatwave check against the configuration.
            //             Thundersnow  - as Blizzard, but really rare. Will not happen in fog, may happen in Blizzard/WhiteOut
            //             Sandstorm - windy, with no precip for several days. Spring-Fall only, highest chance in summer.
            //             Fog - per climate, although night fog in winter is double normal chance
            GenerateEveningFog = (ClimatesOfFerngill.Dice.NextDouble() < ClimateForDay.EveningFogChance * ClimateForDay.RetrieveOdds(ClimatesOfFerngill.Dice,"fog",Game1.dayOfMonth)) && !this.GetCurrentConditions().HasFlag(CurrentWeather.Wind);
          
            bool blockFog = ClimatesOfFerngill.MoonAPI != null && ClimatesOfFerngill.MoonAPI.IsSolarEclipse();

            if (blockFog || ClimatesOfFerngill.WeatherOpt.DisableAllFog)
                GenerateEveningFog = false;
            
            //double fogRoll = (ClimatesOfFerngill.WeatherOpt.DisableAllFog ? 1.1 : ClimatesOfFerngill.Dice.NextDoublePositive());
            double fogRoll = 0;

            if (fogRoll < ClimateForDay.RetrieveOdds(ClimatesOfFerngill.Dice, "fog", Game1.dayOfMonth) && !this.GetCurrentConditions().HasFlag(CurrentWeather.Wind) && !blockFog)
            {
                this.CreateWeather("Fog");

                if (ClimatesOfFerngill.WeatherOpt.Verbose)
                    ClimatesOfFerngill.Logger.Log($"{FogDescription(fogRoll, ClimateForDay.RetrieveOdds(ClimatesOfFerngill.Dice, "fog", Game1.dayOfMonth))}");

                specialWeatherTriggered = true;
            }

            //do these here
            if (this.TodayLow < ClimatesOfFerngill.WeatherOpt.TooColdOutside && !Game1.IsWinter)
            {
                if (ClimatesOfFerngill.WeatherOpt.HazardousWeather)
                {
                    this.AddWeather(CurrentWeather.Frost);
                    specialWeatherTriggered = true;
                }
            }

            //test for spring conversion
            if (this.HasWeather(CurrentWeather.Rain) && this.HasWeather(CurrentWeather.Frost) && (Game1.currentSeason == "spring" || Game1.currentSeason == "fall")
                && ClimatesOfFerngill.Dice.NextDoublePositive() <= ClimatesOfFerngill.WeatherOpt.RainToSnowConversion)
            {
                CurrentConditionsN.RemoveFlags(CurrentWeather.Rain);
                CurrentConditionsN |= CurrentWeather.Snow;
                Game1.isRaining = false;
                Game1.isSnowing = true;
                specialWeatherTriggered = true;
            }


            if (this.HasWeather(CurrentWeather.Snow))
            {
                double blizRoll = 0;
                if (blizRoll <= ClimatesOfFerngill.WeatherOpt.BlizzardOdds)
                {
                    this.CreateWeather("Blizzard");
                    if (ClimatesOfFerngill.WeatherOpt.Verbose)
                        ClimatesOfFerngill.Logger.Log($"With roll {blizRoll:N3} against {ClimatesOfFerngill.WeatherOpt.BlizzardOdds}, there will be blizzards today");
                    if (ClimatesOfFerngill.Dice.NextDoublePositive() < .05 && ClimatesOfFerngill.WeatherOpt.HazardousWeather)
                    {
                        this.CreateWeather("WhiteOut");
                    }
                }

                specialWeatherTriggered = true;
            }

            //Dry Lightning is also here for such like the dry and arid climates 
            //  which have so low rain chances they may never storm.
            if (this.HasWeather(CurrentWeather.Snow))
            {
                double oddsRoll = ClimatesOfFerngill.Dice.NextDoublePositive();

                if (oddsRoll <= ClimatesOfFerngill.WeatherOpt.ThundersnowOdds && !this.HasWeather(CurrentWeather.Fog))
                {
                    this.AddWeather(CurrentWeather.Lightning);
                    if (ClimatesOfFerngill.WeatherOpt.Verbose)
                        ClimatesOfFerngill.Logger.Log($"With roll {oddsRoll:N3} against {ClimatesOfFerngill.WeatherOpt.ThundersnowOdds}, there will be thundersnow today");

                    specialWeatherTriggered = true;
                }
            }

            if (!(this.HasPrecip()))
            {
                double oddsRoll = ClimatesOfFerngill.Dice.NextDoublePositive();

                if (oddsRoll <= ClimatesOfFerngill.WeatherOpt.DryLightning && this.TodayHigh >= ClimatesOfFerngill.WeatherOpt.DryLightningMinTemp &&
                    !this.CurrentConditionsN.HasFlag(CurrentWeather.Frost))
                {
                    this.AddWeather(CurrentWeather.Lightning);
                    if (ClimatesOfFerngill.WeatherOpt.Verbose)
                        ClimatesOfFerngill.Logger.Log($"With roll {oddsRoll:N3} against {ClimatesOfFerngill.WeatherOpt.DryLightning}, there will be dry lightning today.");

                    specialWeatherTriggered = true;
                }

                if (this.TodayHigh > ClimatesOfFerngill.WeatherOpt.TooHotOutside && ClimatesOfFerngill.WeatherOpt.HazardousWeather)
                {
                    this.AddWeather(CurrentWeather.Heatwave);
                    specialWeatherTriggered = true;
                }

                double sandstormOdds = .18;
                if (Game1.currentSeason == "summer")
                    sandstormOdds *= 1.2;

                if (oddsRoll < sandstormOdds && ClimatesOfFerngill.WeatherOpt.HazardousWeather && Game1.isDebrisWeather)
                {
                    this.AddWeather(CurrentWeather.Sandstorm);
                    specialWeatherTriggered = true;
                    this.CreateWeather("Sandstorm");
                }
            }

            //and finally, test for thunder frenzy
            if (this.HasWeather(CurrentWeather.Lightning) && this.HasWeather(CurrentWeather.Rain) && ClimatesOfFerngill.WeatherOpt.HazardousWeather)
            {
                double oddsRoll = ClimatesOfFerngill.Dice.NextDouble();
                if (oddsRoll < ClimatesOfFerngill.WeatherOpt.ThunderFrenzyOdds)
                {
                    this.AddWeather(CurrentWeather.ThunderFrenzy);
                    specialWeatherTriggered = true;
                    if (ClimatesOfFerngill.WeatherOpt.Verbose)
                        ClimatesOfFerngill.Logger.Log($"With roll {oddsRoll:N3} against {ClimatesOfFerngill.WeatherOpt.ThunderFrenzyOdds}, there will be a thunder frenzy today");
                    this.CreateWeather("ThunderFrenzy");
                }
            }

            return specialWeatherTriggered;
        }
    }
}
