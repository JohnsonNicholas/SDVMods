﻿using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

//3P
using NPack;
using StardewModdingAPI;
using StardewModdingAPI.Events;

using StardewValley;
using StardewValley.Objects;
using StardewValley.Monsters;
using StardewValley.Locations;
using StardewValley.Menus;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

//DAMN YOU 1.2
using SFarmer = StardewValley.Farmer;



namespace ClimateOfFerngill
{
    public class ClimatesOfFerngill : Mod
    {
        /// <summary>
        /// This function
        /// </summary>
        public ClimateConfig Config { get; private set; }
        internal FerngillWeather CurrWeather { get; set; }
        public SDVMoon Luna { get; set; }
        private Sprites.Icons OurIcons { get; set; }
        private static Item RememberItemToEat; 

        //trackers
        private bool GameLoaded;
        private bool RainTotemUsedToday;
        private bool HaveIEatenYet;
        private SDVWeather EndWeather;

        //event fields
        private List<Vector2> ThreatenedCrops { get; set; }
        private int DeathTime { get; set; }

        public MersenneTwister Dice;
        private IClickableMenu PreviousMenu;
        private HazardousWeatherEvents BadEvents;

        //chances of specific weathers
        public FerngillClimate WeatherModel;

        //tv overloading
        private static FieldInfo Field = typeof(GameLocation).GetField("afterQuestion", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo TVChannel = typeof(TV).GetField("currentChannel", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo TVScreen = typeof(TV).GetField("screen", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo TVScreenOverlay = typeof(TV).GetField("screenOverlay", BindingFlags.Instance | BindingFlags.NonPublic);
        private static MethodInfo TVMethod = typeof(TV).GetMethod("getWeatherChannelOpening", BindingFlags.Instance | BindingFlags.NonPublic);
        private static MethodInfo TVMethodOverlay = typeof(TV).GetMethod("setWeatherOverlay", BindingFlags.Instance | BindingFlags.NonPublic);
        private static GameLocation.afterQuestionBehavior Callback;
        private static TV Target;

        /// <summary>Initialise the mod.</summary>
        /// <param name="helper">Provides methods for interacting with the mod directory, such as read/writing a config file or custom JSON files.</param>
        public override void Entry(IModHelper helper)
        {
            Dice = new MersenneTwister();
            Config = helper.ReadConfig<ClimateConfig>();
            CurrWeather = new FerngillWeather(Config);
            ThreatenedCrops = new List<Vector2>();

            Luna = new SDVMoon(Monitor, Config, Dice);
            BadEvents = new HazardousWeatherEvents(Monitor, Config, Dice);

            //set flags
            RainTotemUsedToday = false;
            HaveIEatenYet = false;

            //register event handlers
            TimeEvents.DayOfMonthChanged += TimeEvents_DayOfMonthChanged;
            MenuEvents.MenuChanged += MenuEvents_MenuChanged;
            SaveEvents.AfterLoad += SaveEvents_AfterLoad;
            TimeEvents.TimeOfDayChanged += TimeEvents_TimeOfDayChanged;
            SaveEvents.BeforeSave += SaveEvents_BeforeSave;
            GameEvents.QuarterSecondTick += GameEvents_QuarterSecondTick;
            GameEvents.UpdateTick += GameEvents_UpdateTick;
            SaveEvents.AfterReturnToTitle += SaveEvents_AfterReturnToTitle;

            //siiigh.
            helper.ConsoleCommands
                    .Add("world_changetmrweather", "Changes tomorrow's weather.\"rain,storm,snow,debris,festival,wedding,sun\" ", TmrwWeatherChangeFromConsole)
                    .Add("world_changeweather", "Changes CURRENT weather. \"rain,storm,snow,debris,sun\"", WeatherChangeFromConsole)
                    .Add("player_removecold", "Removes the cold from the player.", RemovePlayerCode);


            //register keyboard handlers and other menu events
            ControlEvents.KeyPressed += (sender, e) => this.ReceiveKeyPress(e.KeyPressed, this.Config.Keyboard);
            MenuEvents.MenuClosed += (sender, e) => this.ReceiveMenuClosed(e.PriorMenu);

            VerifyBundledWeatherFiles();

            /*
            //load in custom weather object
            if (Config.UseCustomWeather && File.Exists(Path.Combine(Helper.DirectoryPath, "userClimate.json"))){
                //if the user object exists
                if (Config.TooMuchInfo)
                    Monitor.Log("Detected custom weather and enabled. Reading in now");

                UserClimate = Helper.ReadJsonFile<CustomClimate>(Path.Combine(Helper.DirectoryPath, "userClimate.json"));
            }
            else if (!File.Exists(Path.Combine(Helper.DirectoryPath, "userClimate.json"))){
                UserClimate = new CustomClimate();
                Helper.WriteJsonFile<CustomClimate>(Path.Combine(Helper.DirectoryPath, "userClimate.json"), UserClimate);
            }
            */
        }

        private void VerifyBundledWeatherFiles()
        {
            if (!File.Exists(Path.Combine(Helper.DirectoryPath, "weather/normal.json")))
            {
                FerngillClimate NormalClimate = new FerngillClimate();
                NormalClimate.ClimateSequences.Add(
                    new FerngillClimateTimeSpan(Season: "spring", BeginDay: 1, EndDay: 9, FrozenPrecip: false,
                        LowTempBase: 4, LowTempChange: .725, LowTempVariable: .5, HighTempBase: 8.5, HighTempChange: .745,
                        HighTempVariable: .65, BaseRainChance: .55, RainChange: -.024, RainVariability: .001, BaseStormChance: .145, StormChange: 0, 
                        StormVariability: .01, BaseDebrisChance: .25, DebrisChange: 0, AllowDebris: true)
                    
                        );
                Helper.WriteJsonFile<FerngillClimate>(Path.Combine(Helper.DirectoryPath, "weather/normal.json"), NormalClimate);
            }
        }

        private void RemovePlayerCode(string arg1, string[] arg2)
        {
            BadEvents.RemoveCold();
        }

        private void WeatherChangeFromConsole(string arg1, string[] arg2)
        {
            if (arg2.Length < 1)
                return;

            if (Config.TooMuchInfo)
                Monitor.Log($"The arguments passed are {arg1} and {InternalUtility.PrintStringArray(arg2)}");

            string ChosenWeather = arg2[0];

            switch (ChosenWeather)
            {
                case "rain":
                    Game1.isSnowing = Game1.isLightning = Game1.isDebrisWeather = false;
                    Game1.isRaining = true;
                    Monitor.Log("The weather is now rain",LogLevel.Info);
                    break;
                case "storm":
                    Game1.isSnowing = Game1.isDebrisWeather = false;
                    Game1.isLightning = Game1.isRaining = true;
                    Monitor.Log("The weather is now storm", LogLevel.Info);
                    break;
                case "snow":
                    Game1.isRaining = Game1.isLightning = Game1.isDebrisWeather = false;
                    Game1.isSnowing = true;
                    Monitor.Log("The weather is now snow", LogLevel.Info);
                    break;
                case "debris":
                    Game1.isSnowing = Game1.isLightning = Game1.isRaining = false;
                    Game1.isDebrisWeather = true;
                    Monitor.Log("The weather is now debris", LogLevel.Info);
                    break;
                case "sunny":
                    Game1.isSnowing = Game1.isLightning = Game1.isRaining = Game1.isRaining = false;
                    Monitor.Log("The weather is now sunny", LogLevel.Info);
                    break;
            }

            Game1.updateWeatherIcon();
            if (Config.TooMuchInfo)
                Monitor.Log(InternalUtility.PrintCurrentWeatherStatus());

        }

        private void TmrwWeatherChangeFromConsole(string arg1, string[] arg2)
        {
            if (arg2.Length < 1)
                return;

            if (Config.TooMuchInfo)
                Monitor.Log($"The arguments passed are {arg1} and {arg2.ToString()}");

            string ChosenWeather = arg2[0];
            switch(ChosenWeather)
                    {
                        case "rain":
                            Game1.weatherForTomorrow = Game1.weather_rain;
                            Monitor.Log("The weather tommorow is now rain", LogLevel.Info);
                            break;
                        case "storm":
                            Game1.weatherForTomorrow = Game1.weather_lightning;
                            Monitor.Log("The weather tommorow is now storm", LogLevel.Info);
                            break;
                        case "snow":
                            Game1.weatherForTomorrow = Game1.weather_snow;
                            Monitor.Log("The weather tommorow is now snow", LogLevel.Info);
                            break;
                        case "debris":
                            Game1.weatherForTomorrow = Game1.weather_debris;
                            Monitor.Log("The weather tommorow is now debris", LogLevel.Info);
                            break;
                        case "festival":
                            Game1.weatherForTomorrow = Game1.weather_festival;
                            Monitor.Log("The weather tommorow is now festival", LogLevel.Info);
                            break;
                        case "sun":
                            Game1.weatherForTomorrow = Game1.weather_sunny;
                            Monitor.Log("The weather tommorow is now sun", LogLevel.Info);
                            break;
                        case "wedding":
                            Game1.weatherForTomorrow = Game1.weather_wedding;
                            Monitor.Log("The weather tommorow is now wedding", LogLevel.Info);
                            break;
            }
        }

        private void GameEvents_UpdateTick(object sender, EventArgs e)
        {
            if (Game1.player != null && Game1.player.itemToEat != RememberItemToEat && !HaveIEatenYet)
            {
                RememberItemToEat = Game1.player.itemToEat;
                if (Game1.player.itemToEat != null)
                {
                    if (Config.TooMuchInfo)
                        Monitor.Log($"Detecting someone is eating something! This something is {Game1.player.itemToEat.parentSheetIndex}");

                    HaveIEatenYet = true;
                    if (RememberItemToEat.parentSheetIndex == 351 && Game1.isEating)
                    {
                        if (BadEvents.HasACold())
                        {
                            if (Config.TooMuchInfo)
                                Monitor.Log("Removing the cold after having drunk a muscle relaxant");

                            BadEvents.RemoveCold();
                        }

                    }
                }
                else
                {
                    //clear the flag
                    HaveIEatenYet = false;
                }
            }
        }

        private void GameEvents_QuarterSecondTick(object sender, EventArgs e)
        {

            if (Config.StormTotem)
            {
                if (Game1.weatherForTomorrow != (int)EndWeather && !RainTotemUsedToday)
                {
                    RainTotemUsedToday = true;
                    if (Config.TooMuchInfo)
                        Monitor.Log("Storm totem code enabled and running");

                    //use flat storm chances
                    if (Game1.currentSeason == "spring")
                    {
                        if (Dice.NextDoublePositive() < .25)
                        {
                            Game1.weatherForTomorrow = Game1.weather_lightning;
                            Game1.addHUDMessage(new HUDMessage("You hear a roll of thunder..."));
                            if (Config.TooMuchInfo)
                                Monitor.Log($"Setting the rain totem to stormy, based on a roll of under .6");
                        }
                    }

                    //use flat storm chances
                    if (Game1.currentSeason == "summer")
                    {
                        if (Dice.NextDoublePositive() < .4)
                        {
                            Game1.weatherForTomorrow = Game1.weather_lightning;
                            Game1.addHUDMessage(new HUDMessage("You hear a roll of thunder..."));
                            if (Config.TooMuchInfo)
                                Monitor.Log($"Setting the rain totem to stormy, based on a roll of under .6");
                        }
                    }

                    //use flat storm chances
                    if (Game1.currentSeason == "autumn")
                    {
                        if (Dice.NextDoublePositive() < .6)
                        {
                            Game1.weatherForTomorrow = Game1.weather_lightning;
                            Game1.addHUDMessage(new HUDMessage("You hear a roll of thunder..."));
                            if (Config.TooMuchInfo)
                                Monitor.Log($"Setting the rain totem to stormy, based on a roll of under .6");
                        }
                    }
                }
            }
        }

        
        private void SaveEvents_AfterReturnToTitle(object sender, EventArgs e)
        {
            if (Config.TooMuchInfo)
                Monitor.Log("Resetting the game state for returning to title");

            BadEvents.UpdateForNewDay();
            CurrWeather.UpdateForNewDay();
            OurIcons = null;
            Luna.Reset();
            DeathTime = 0;
            ThreatenedCrops.Clear();
            GameLoaded = false;
            RainTotemUsedToday = false;
        }

        private void ReceiveKeyPress(Keys key, Keys config)
        {
            if (config != key)  //sanity force this to exit!
                return;

            if (!GameLoaded)
                return;

            // perform bound action
            this.ToggleMenu();
        }

        private void ReceiveMenuClosed(IClickableMenu closedMenu)
        {
            // restore the previous menu if it was hidden to show the lookup UI
            if (closedMenu is WeatherMenu && this.PreviousMenu != null)
            {
                 Game1.activeClickableMenu = this.PreviousMenu;
                 this.PreviousMenu = null;
            }
        }

        private void SaveEvents_BeforeSave(object sender, EventArgs e)
        {
            //run all night time processing. Events do their own checking if enabled
            if (Config.HarshWeather)
            {
                BadEvents.EarlyFrost(CurrWeather);
            }

            Luna.HandleMoonBeforeSleep(Game1.getFarm()); //run lunar events           
        }
       
        private void TimeEvents_TimeOfDayChanged(object sender, EventArgsIntChanged e)
        {
            if (Config.StormyPenalty)
                BadEvents.CatchACold();

            //specific time stuff
            if (e.NewInt == 610)
            {
                CurrWeather.MessageForDangerousWeather();
            }

            //heatwave event
            if (e.NewInt == 1700)
            {
                //the heatwave can't happen if it's a festval day, and if it's rainy or lightening.
                if (CurrWeather.GetTodayHigh() > (int)Config.HeatwaveWarning && 
                    !Utility.isFestivalDay(Game1.dayOfMonth, Game1.currentSeason) && (!Game1.isRaining || !Game1.isLightning))
                {
                    DeathTime = InternalUtility.GetNewValidTime(e.NewInt, 300, InternalUtility.TIMEADD); //3 hours.
                    ThreatenedCrops = BadEvents.ProcessHeatwave(Game1.getFarm(), CurrWeather);

                    if (ThreatenedCrops.Count > 0)
                    {
                        if (!Config.AllowCropHeatDeath)
                            InternalUtility.ShowMessage("The extreme heat has caused some of your crops to become dry....!");
                        else
                        {
                            InternalUtility.ShowMessage("The extreme heat has caused some of your crops to dry out. If you don't water them, they'll die!");
                        }
                    }
                    
                }
            }

            //killer heatwave crop death time
            if (Game1.timeOfDay == DeathTime && Config.AllowCropHeatDeath)
            {
                BadEvents.WiltHeatwave(ThreatenedCrops);
            }

            /* /////////////////////////////////////////////////
             * night time events
             * //////////////////////////////////////////////// */

            if (Luna.CheckForGhostSpawn()) SpawnGhostOffScreen();

            // sanity check if the player hits 0 Stamina ( the game doesn't track this )
            if (Game1.player.Stamina <= 0f)
            {
                InternalUtility.FaintPlayer();
            }
        }

        public void SpawnGhostOffScreen()
        {
            Vector2 zero = Vector2.Zero;

            if (Game1.getFarm() is Farm ourFarm)
            {
                switch (Game1.random.Next(4))
                {
                    case 0:
                        zero.X = (float)Dice.Next(ourFarm.map.Layers[0].LayerWidth);
                        break;
                    case 1:
                        zero.X = (float)(ourFarm.map.Layers[0].LayerWidth - 1);
                        zero.Y = (float)Dice.Next(ourFarm.map.Layers[0].LayerHeight);
                        break;
                    case 2:
                        zero.Y = (float)(ourFarm.map.Layers[0].LayerHeight - 1);
                        zero.X = (float)Dice.Next(ourFarm.map.Layers[0].LayerWidth);
                        break;
                    case 3:
                        zero.Y = (float)Game1.random.Next(ourFarm.map.Layers[0].LayerHeight);
                        break;
                }

                if (Utility.isOnScreen(zero * (float)Game1.tileSize, Game1.tileSize))
                    zero.X -= (float)Game1.viewport.Width;

                List<NPC> characters = ourFarm.characters;
                Ghost bat = new Ghost(zero * Game1.tileSize)
                {
                    focusedOnFarmers = true,
                    wildernessFarmMonster = true
                };
                characters.Add((NPC)bat);

                if (!Game1.currentLocation.Equals((object)this))
                    return;
            }
        }

        private void SaveEvents_AfterLoad(object sender, EventArgs e)
        {
            GameLoaded = true;
            OurIcons = new Sprites.Icons(Helper.DirectoryPath);
            //UpdateWeather(CurrWeather);
            Luna.UpdateForNewDay();
            BadEvents.UpdateForNewDay();
            RainTotemUsedToday = false;
        }

        private void MenuEvents_MenuChanged(object sender, EventArgsClickableMenuChanged e)
        {
            TryHookTelevision();
        }

        #region TVOverride
        public void TryHookTelevision()
        {
            if (Game1.currentLocation != null && Game1.currentLocation is DecoratableLocation && Game1.activeClickableMenu != null && Game1.activeClickableMenu is DialogueBox)
            {
                Callback = (GameLocation.afterQuestionBehavior)Field.GetValue(Game1.currentLocation);
                if (Callback != null && Callback.Target.GetType() == typeof(TV))
                {
                    Field.SetValue(Game1.currentLocation, new GameLocation.afterQuestionBehavior(InterceptCallback));
                    Target = (TV)Callback.Target;
                }
            }
        }
    
        public void InterceptCallback(SFarmer who, string answer)
        {
            if (answer != "Weather")
            {
                Callback(who, answer);
                return;
            }
            TVChannel.SetValue(Target, 2);
            TVScreen.SetValue(Target, new TemporaryAnimatedSprite(Game1.mouseCursors, new Rectangle(413, 305, 42, 28), 150f, 2, 999999, Target.getScreenPosition(), false, false, (float)((double)(Target.boundingBox.Bottom - 1) / 10000.0 + 9.99999974737875E-06), 0.0f, Color.White, Target.getScreenSizeModifier(), 0.0f, 0.0f, 0.0f, false));
            Game1.drawObjectDialogue(Game1.parseText((string)TVMethod.Invoke(Target, null)));
            Game1.afterDialogues = NextScene;
        }

        public void NextScene()
        {
            TVScreen.SetValue(Target, new TemporaryAnimatedSprite(Game1.mouseCursors, new Rectangle(497, 305, 42, 28), 9999f, 1, 999999, Target.getScreenPosition(), false, false, (float)((double)(Target.boundingBox.Bottom - 1) / 10000.0 + 9.99999974737875E-06), 0.0f, Color.White, Target.getScreenSizeModifier(), 0.0f, 0.0f, 0.0f, false));
            Game1.drawObjectDialogue(Game1.parseText(GetWeatherForecast()));
            TVMethodOverlay.Invoke(Target, null);
            Game1.afterDialogues = Target.proceedToNextScene;
        }
        #endregion

        public string GetWeatherForecast()
        {
            if (Config.TooMuchInfo)
                Monitor.Log(
                      "This is a long debug message.\n"
                      + $"Wedding Today: {Game1.weddingToday}\n"
                      + $"Spouse Status: {Game1.player.spouse}\n"
                      + $"Countdown info: {Game1.countdownToWedding}");

            if (Game1.weddingToday) // sanity check
            {
                if (Config.TooMuchInfo)
                    Monitor.Log("There was a wedding today. Regenerating the weather.");

                UpdateWeather(CurrWeather, weddingOverride: true);
            }

            string tvText = " ";

            //The TV should display: Alerts, today's weather, tommorow's weather, alerts.

            // Something such as "Today, the high is 12C, with low 8C. It'll be a very windy day. Tommorow, it'll be rainy."
            // since we don't predict weather in advance yet. (I don't want to rearchitecture it yet.)
            // That said, the TV channel starts with Tommorow, so we need to keep that in mind.


            // Alerts for frost/cold snap display all day. Alerts for heatwave last until 1830. 
            tvText = "The forecast for the Valley is: ";

            if (CurrWeather.GetTodayHigh() > Config.HeatwaveWarning && Game1.timeOfDay < 1830)
                tvText = tvText + "That it will be unusually hot outside. Stay hydrated and be careful not to stay too long in the sun. ";
            if (CurrWeather.GetTodayHigh() < -5)
                tvText = tvText + "There's an extreme cold snap passing through the valley. Stay warm. ";
            if (CurrWeather.GetTodayLow() < 2 && Config.HarshWeather)
                tvText = tvText + "Warning. We're getting frost tonight! Be careful what you plant! ";


            if (Game1.timeOfDay < 1800) //don't display today's weather 
            {
                tvText += "The high for today is ";
                if (!Config.DisplaySecondScale)
                    tvText += WeatherHelper.DisplayTemperature(CurrWeather.GetTodayHigh(), Config.TempGauge) + ", with the low being " + WeatherHelper.DisplayTemperature(CurrWeather.GetTodayLow(), Config.TempGauge) + ". ";
                else //derp.
                    tvText += WeatherHelper.DisplayTemperature(CurrWeather.GetTodayHigh(), Config.TempGauge) + " (" + WeatherHelper.DisplayTemperature(CurrWeather.GetTodayHigh(), Config.SecondScaleGauge) + ") , with the low being " + WeatherHelper.DisplayTemperature(CurrWeather.GetTodayLow(), Config.TempGauge) + " (" + WeatherHelper.DisplayTemperature(CurrWeather.GetTodayHigh(), Config.SecondScaleGauge) + ") . ";

                if (Config.TooMuchInfo) Monitor.Log(tvText);

                //today weather
                tvText = tvText + WeatherHelper.GetWeatherDesc(Dice, WeatherHelper.GetTodayWeather(), true, Monitor, Config.TooMuchInfo);

                //get WeatherForTommorow and set text
                tvText = tvText + "#Tommorow, ";
            }

            //tommorow weather
            tvText = tvText + WeatherHelper.GetWeatherDesc(Dice, (SDVWeather)Game1.weatherForTomorrow, false, Monitor, Config.TooMuchInfo);

            return tvText;
        }
       
        public void TimeEvents_DayOfMonthChanged(object sender, EventArgsIntChanged e)
        {
            if (!GameLoaded) //sanity check
                return;

            //update objects for new day.
            BadEvents.UpdateForNewDay();
            CurrWeather.UpdateForNewDay();
            RainTotemUsedToday = false;
            Luna.UpdateForNewDay();
            DeathTime = 0;
            ThreatenedCrops.Clear();
            Luna.HandleMoonAfterWake(InternalUtility.GetBeach());

            //update the weather
            UpdateWeather(CurrWeather);    
        }


        void UpdateWeather(FerngillWeather weatherOutput, bool weddingOverride = false)
        {
            //get start values
            SDVSeasons CurSeason = InternalUtility.GetSeason(Game1.currentSeason);
            SDVWeather TmrwWeather = (SDVWeather)Game1.weatherForTomorrow;
            bool forceSet = false;

            if (Config.TooMuchInfo)
                Monitor.Log($"The weather tommorow at start is: {WeatherHelper.DescWeather(TmrwWeather, Game1.currentSeason)}");

            // The mod executes after the main loop and should only execute at the beginning of the
            //  day. This really means we have to make sure it runs or we'll have an issue with the tv
            //  description.

            // So, essentially, if it's already set to wedding or festival, we can go ahead and 
            //  just not run. If you use a rain totem, that should run after this, and before the 
            //  game's own weather processing.
            if (!weddingOverride)
            {

                if (Config.TooMuchInfo && Game1.player.spouse != null)
                    Monitor.Log($"Wedding flags: {Game1.countdownToWedding == 1} and {Game1.player.spouse.Contains("engaged")} with" +
                        $"count down to wedding being {Game1.countdownToWedding}");
                else if (Config.TooMuchInfo)
                    Monitor.Log($"Spouse is null. No wedding.");

                if (Game1.countdownToWedding == 1 && Game1.player.spouse != null && Game1.player.spouse.Contains("engaged"))
                {
                    if (Config.TooMuchInfo)
                        Monitor.Log("Wedding tommorrow");
                    forceSet = true;
                    Game1.weatherForTomorrow = Game1.weather_wedding;
                }        
            }

            if (TmrwWeather == SDVWeather.Festival)
            {
                if (Config.TooMuchInfo)
                    Monitor.Log("The weather tommorow is a festival.", LogLevel.Warn);
                forceSet = true;
            }

            //handle calcs here for odds.
            double chance = Dice.NextDouble();

            //global change - if it rains, drop the temps (and if it's stormy, drop the temps)
            if (Game1.isRaining)
            {
                if (Config.TooMuchInfo) Monitor.Log($"Dropping temp by 4 from {CurrWeather.GetTodayHigh()}");
                CurrWeather.SetTodayHigh(CurrWeather.GetTodayHigh() - 4);
                CurrWeather.SetTodayLow(CurrWeather.GetTodayLow() - 2);
            }

            //handle forced weather from the game - this function will actually set the weather itself, if true.
            if (GameWillForceTomorrow(GetTommorowInGame()))
            {
                forceSet = true;
                if (Config.TooMuchInfo) Monitor.Log("The game is forcing weather tommorow. Setting flag.");

            }

            if (Game1.currentSeason == "winter" && (Game1.weatherForTomorrow == Game1.weather_rain || Game1.weatherForTomorrow == Game1.weather_lightning ) && !Config.AllowRainInWinter)
            {
                if (Config.TooMuchInfo)
                    Monitor.Log($"Fixing {WeatherHelper.WeatherToString(Game1.weatherForTomorrow)} in winter. Force setting to snow");

                Game1.weatherForTomorrow = Game1.weather_snow;
            }


            if (forceSet)
            {
                if (Config.TooMuchInfo) Monitor.Log("Detecting Force Set. Exiting.");
                return;
            }
 
            
            //Snow fall on Fall 28, if the flag is set.
            if (Game1.dayOfMonth == 28 && Game1.currentSeason == "fall" && Config.AllowSnowOnFall28)
            {
                CurrWeather.SetTodayHigh(2);
                CurrWeather.SetTodayLow(-1);
                TmrwWeather = (SDVWeather)Game1.weather_snow; //it now snows on Fall 28.
            }
            
            if (Config.TooMuchInfo)
                Monitor.Log($"We've set the weather for tommorow. It is: {WeatherHelper.DescWeather(TmrwWeather, Game1.currentSeason)}");

            //set trackers
            EndWeather = TmrwWeather;
            // Game1.chanceToRainTomorrow = rainChance; //set for various events.
            Game1.weatherForTomorrow = (int)TmrwWeather;

            if (Config.TooMuchInfo)
                Monitor.Log($"Checking if set. Generated Weather: {WeatherHelper.DescWeather(TmrwWeather, Game1.currentSeason)} and set weather is: {WeatherHelper.DescWeather(Game1.weatherForTomorrow, Game1.currentSeason)}");
        }

        private bool GameWillForceTomorrow(SDVDate Tomorrow)
        {
            switch (Tomorrow.Season)
            {
                case "spring":
                    if (Game1.year == 1 && Tomorrow.Day == 2 || Tomorrow.Day == 4)
                    {
                        Game1.weatherForTomorrow = Game1.weather_sunny;
                        return true;
                    }

                    else if (Game1.year == 1 && Tomorrow.Day == 3)
                    {
                        Game1.weatherForTomorrow = Game1.weather_rain;
                        return true;
                    }

                    else if (Tomorrow.Day == 1)
                    {
                        Game1.weatherForTomorrow = Game1.weather_sunny;
                        return true;
                    }
                    else if (Tomorrow.Day == 13 || Tomorrow.Day == 24)
                    {
                        Game1.weatherForTomorrow = Game1.weather_festival;
                        return true;
                    }
                    break;
                case "summer":
                    if (Tomorrow.Day == 1)
                    {
                        Game1.weatherForTomorrow = Game1.weather_sunny;
                        return true;
                    }
                    else if (Tomorrow.Day == 11 || Tomorrow.Day == 28)
                    {
                        Game1.weatherForTomorrow = Game1.weather_festival;
                        return true;
                    }

                    else if (Tomorrow.Day == 13 || Tomorrow.Day == 25 || Tomorrow.Day == 26)
                    {
                        Game1.weatherForTomorrow = Game1.weather_lightning;
                        return true;
                    }
                    break;
                case "fall":
                    if (Tomorrow.Day == 1)
                    {
                        return true;
                    }
                    else if (Tomorrow.Day == 16 || Tomorrow.Day == 27)
                    {
                        Game1.weatherForTomorrow = Game1.weather_festival;
                        return true;
                    }
                    break;
                case "winter":
                    if (Tomorrow.Day == 1)
                    {
                        Game1.weatherForTomorrow = Game1.weather_sunny;
                        return true;
                    }
                    else if (Tomorrow.Day == 8 || Tomorrow.Day == 25)
                    {
                        Game1.weatherForTomorrow = Game1.weather_festival;
                        return true;
                    }
                    break;
                default:
                    return false;
            }

            return false;
        }

        private SDVDate GetTommorowInGame()
        {
            int day = 1;
            string season = "spring";

            if (Game1.dayOfMonth == 28)
            {
                day = 1;
                season = GetNextSeason(Game1.currentSeason);
            }
            else
            {
                season = Game1.currentSeason;
                day = Game1.dayOfMonth + 1;
            }

            return new SDVDate(season, day);
            
        }

        private string GetNextSeason(string currentSeason)
        {
            if (currentSeason == "spring")
                return "summer";
            if (currentSeason == "summer")
                return "fall";
            if (currentSeason == "fall")
                return "winter";
            if (currentSeason == "winter")
                return "spring";

            return "error";
        }


        #region Menu
        private void ToggleMenu()
        {
            if (Game1.activeClickableMenu is WeatherMenu)
                this.HideMenu();
            else
                this.ShowMenu();
        }

        private void ShowMenu()
        {
            // show menu
            this.PreviousMenu = Game1.activeClickableMenu;
            Game1.activeClickableMenu = new WeatherMenu(Monitor, this.Helper.Reflection, OurIcons, CurrWeather, Luna, Config);
        }

        private void HideMenu()
        {
            if (Game1.activeClickableMenu is WeatherMenu)
            {
                Game1.playSound("bigDeSelect"); // match default behaviour when closing a menu
                Game1.activeClickableMenu = null;
            }
        }
        #endregion
    }    
}

