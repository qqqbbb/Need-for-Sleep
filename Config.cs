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
        public static ConfigEntry<bool> sleepAnytime;


        public static void Bind()
        {
            hoursNeedToSleep = Main.config.Bind("", "Hours you need to sleep", 6, "Number of hours you need to sleep every 24 hours.");
            calorieBurnMultSleep = Main.config.Bind("", "Calorie burn rate multiplier when sleeping", 0f, "");
            sleepAnytime = Main.config.Bind("", "Can go to sleep anytime", false, "By default you can fall sleep only when you are tired. If this is on, you can fall sleep anytime you want.");
        }

    }
}
