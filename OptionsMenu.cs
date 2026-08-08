using Nautilus.Handlers;
using Nautilus.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Need_for_Sleep
{
    public class OptionsMenu : ModOptions
    {
        static public GameInput.Button sleepButton = EnumHandler.AddEntry<GameInput.Button>("NS_sleepButton_")
            .CreateInput(Language.main.Get("NS_sleep_button"))
            .WithKeyboardBinding("<Keyboard>/r")
            .WithControllerBinding("<Gamepad>/buttonWest")
            .WithCategory(Main.PLUGIN_NAME);

        public OptionsMenu() : base(Main.PLUGIN_NAME)
        {
            ModSliderOption hoursNeedToSleepSlider = Config.hoursNeedToSleep.ToModSliderOption(3, 12, 1);
            ModSliderOption coffeeHoursSlider = Config.coffeeHours.ToModSliderOption(1, 12, 1);
            ModSliderOption calorieBurnMultSleepSlider = Config.calorieBurnMultSleep.ToModSliderOption(0, 1f, .01f, "{0:0.0.#}");

            AddItem(hoursNeedToSleepSlider);
            AddItem(coffeeHoursSlider);
            AddItem(calorieBurnMultSleepSlider);
            AddItem(Config.sleepAnytime.ToModToggleOption());
            AddItem(Config.showTimeTillTired.ToModToggleOption());
            AddItem(Config.showTimeTillTireSleepButton.ToModToggleOption());
            //AddItem(Config.delayButtons.ToModToggleOption()); broken after 82304 update
            AddItem(Config.turnSensivity.ToModToggleOption());
            AddItem(Config.blurryVision.ToModToggleOption());
            AddItem(Config.slowMovement.ToModToggleOption());

        }
    }
}
