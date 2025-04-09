using BepInEx.Configuration;
using Nautilus.Options.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Need_for_Sleep
{
    internal class Config
    {
        public static ConfigEntry<int> hoursNeedToSleep;
        public static ConfigEntry<float> calorieBurnMultSleep;
        public static ConfigEntry<SleepWarning> sleepWarning;


        public static void Bind()
        {
            hoursNeedToSleep = Main.config.Bind("", "Hours you need to sleep", 6, "Number of hours you need to sleep every 24 hours.");
            calorieBurnMultSleep = Main.config.Bind("", "Calorie burn rate multiplier when sleeping", 0f, "");
            sleepWarning = Main.config.Bind("", "Show warning when you should go to sleep", SleepWarning.Always, "");
        }

        public enum SleepWarning { Always, Once, Never }
    }
}
