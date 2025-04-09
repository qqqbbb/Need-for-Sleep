using Nautilus.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Need_for_Sleep
{
    public class OptionsMenu : ModOptions
    {
        public OptionsMenu() : base("Need for Sleep")
        {
            ModSliderOption hoursNeedToSleepSlider = Config.hoursNeedToSleep.ToModSliderOption(3, 12, 1);
            ModSliderOption calorieBurnMultSleepSlider = Config.calorieBurnMultSleep.ToModSliderOption(0, 1f, .01f, "{0:0.0.#}");


            AddItem(hoursNeedToSleepSlider);
            AddItem(calorieBurnMultSleepSlider);
            AddItem(Config.sleepWarning.ToModChoiceOption());


        }
    }
}
