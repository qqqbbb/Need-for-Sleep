using BepInEx.Configuration;
using Nautilus.Options.Attributes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Need_for_Sleep
{
    internal class Config
    {
        public static ConfigEntry<int> hoursNeedToSleep;
        public static ConfigEntry<int> coffeeHours;
        public static ConfigEntry<float> calorieBurnMultSleep;
        public static ConfigEntry<bool> sleepAnytime;
        public static ConfigEntry<bool> showTimeTillTired;
        public static ConfigEntry<bool> showTimeTillTireSleepButton;
        //public static ConfigEntry<SleepButton> sleepButton;
        public static ConfigEntry<bool> delayButtons;
        public static ConfigEntry<bool> turnSensivity;
        public static ConfigEntry<bool> blurryVision;
        public static ConfigEntry<bool> slowMovement;


        //public enum SleepButton { Left_hand, Right_hand, Jump, Deconstruct, Tool_alt_use, Reload, Sprint };

        public static void Bind()
        {
            hoursNeedToSleep = Main.config.Bind("", "NS_sleep_hours", 6, "NS_sleep_hours_desc");
            calorieBurnMultSleep = Main.config.Bind("", "NS_calorie_burn_rate", 0f, "");
            sleepAnytime = Main.config.Bind("", "NS_sleep_anytime", false, "NS_sleep_anytime_desc");
            showTimeTillTired = Main.config.Bind("", "NS_show_time_tired_bed", true, "");
            showTimeTillTireSleepButton = Main.config.Bind("", "NS_show_time_tired_button", false, "");
            //sleepButton = Main.config.Bind("", "Sleep button", SleepButton.Left_hand, "");
            delayButtons = Main.config.Bind("", "Actions are less responsive when sleep deprived", true, "");
            turnSensivity = Main.config.Bind("", "NS_turning", true, "");
            blurryVision = Main.config.Bind("", "NS_blurry_vision", true, "");
            slowMovement = Main.config.Bind("", "NS_movement", true, "");
            coffeeHours = Main.config.Bind("", "NS_coffee_hours", 1, "NS_coffee_hours_desc");

        }



    }
}
