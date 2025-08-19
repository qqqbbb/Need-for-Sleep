using HarmonyLib;
using Nautilus.Utility;
using Platform.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static ErrorMessage;

namespace Need_for_Sleep
{
    internal class Testing
    {

        //[HarmonyPatch(typeof(Player), "Update")]
        class Player_Update_Patch
        {
            static void Postfix(Player __instance)
            {
                //AddDebug($"timeScale {Time.timeScale}");
                //string keyDown = GameInput.GetPressedInput(GameInput.lastDevice);
                //if (!string.IsNullOrEmpty(keyDown))
                //    AddDebug($"GetPressedInput {keyDown}");

                if (Input.GetKeyDown(KeyCode.C))
                {
                    //uGUI_PlayerSleep.main.fadeInSpeed *= .5f;
                    //uGUI_PlayerSleep.main.StartSleepScreen();
                    //AddDebug("timePassed " + DayNightCycle.main.timePassedAsFloat);
                }
                if (Input.GetKeyDown(KeyCode.V))
                {
                    //uGUI_PlayerSleep.main.StopSleepScreen();
                }
            }


        }

        bool fadeIn;
        bool fadeOut;
        private static Color invisibleColor = new Color(0, 0, 0, 0);

        void CLoseEyes()
        {
            //uGUI_PlayerSleep.main.state = uGUI_PlayerSleep.State.FadeIn;
            uGUI_PlayerSleep.main.blackOverlay.color = invisibleColor;
            fadeIn = true;
            fadeOut = false;
        }

        void OpenEyes()
        {
            fadeIn = false;
            fadeOut = true;
        }

        void UpdateEyes()
        {
            if (fadeIn)
            {
                AddDebug($"fadeIn {uGUI_PlayerSleep.main.fadeInSpeed}");
                uGUI_PlayerSleep.main.blackOverlay.color = Color.Lerp(uGUI_PlayerSleep.main.blackOverlay.color, Color.black, Time.deltaTime * uGUI_PlayerSleep.main.fadeInSpeed);
                uGUI_PlayerSleep.main.blackOverlay.enabled = true;
                if (uGUI_PlayerSleep.main.blackOverlay.color.a < 0.99f)
                    return;
                uGUI_PlayerSleep.main.blackOverlay.color = Color.black;
                //uGUI_PlayerSleep.main.state = uGUI_PlayerSleep.State.Enabled;
                fadeIn = false;
            }
            else if (fadeOut)
            {
                AddDebug("fadeOut");
                uGUI_PlayerSleep.main.blackOverlay.color = Color.Lerp(uGUI_PlayerSleep.main.blackOverlay.color, invisibleColor, Time.deltaTime * uGUI_PlayerSleep.main.fadeOutSpeed);
                if (uGUI_PlayerSleep.main.blackOverlay.color.a >= 0.01f)
                    return;
                uGUI_PlayerSleep.main.blackOverlay.color = invisibleColor;
                uGUI_PlayerSleep.main.blackOverlay.enabled = false;
                fadeOut = false;
                //uGUI_PlayerSleep.main.state = uGUI_PlayerSleep.State.Disabled;
            }
        }


    }
}
