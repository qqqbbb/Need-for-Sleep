using HarmonyLib;
using Nautilus.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UWE;
using static Bed;
using static ErrorMessage;
using Button = GameInput.Button;


namespace Need_for_Sleep
{
    internal class Patches
    {
        public static bool sleeping;
        public static float speedMod = 1;
        public static float updateInterval = 10;
        private const float oneHourDuration = DayNightCycle.kDayLengthSeconds / 24f;
        static Survival survival;
        static float sleepDebt;
        static float hungerUpdateTime;
        static bool frame;
        static Bed myBed;
        static Vector3 myBedLocalPos = new Vector3(0, -2, 0);
        static int checkLayerMask = ~(1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("Trigger"));
        static float sleepDurationMult = 1;
        static HashSet<Button> delayableButtons = new HashSet<Button> { Button.MoveForward, Button.MoveBackward, Button.MoveLeft, Button.MoveRight, Button.MoveDown, Button.MoveUp, Button.Jump, Button.PDA, Button.Deconstruct, Button.LeftHand, Button.RightHand, Button.CycleNext, Button.CyclePrev, Button.Slot1, Button.Slot2, Button.Slot3, Button.Slot4, Button.Slot5, Button.AltTool, Button.Reload, Button.Sprint, Button.AutoMove, Button.LookDown, Button.LookUp, Button.LookRight, Button.LookLeft };
        private static bool seaglideEquipped;
        static private RadialBlurScreenFXController radialBlurControl;
        private static bool lookingAtBed;
        private static bool forcedWakeUp;
        private static float sleepStartTime;
        private static float timeWokeUp;
        private static bool builderEquipped;
        private static Vector3 playerPosBeforeSleep;

        public static void ResetVars()
        {
            speedMod = 1;
            sleepDebt = 0;
        }

        public static void Setup()
        {
            survival = Player.main.GetComponent<Survival>();
            radialBlurControl = MainCamera.camera.GetComponent<RadialBlurScreenFXController>();
            Player.main.gameObject.AddComponent<SleepText>();
            Player.main.StartCoroutine(SpawnBed());
            //Main.logger.LogDebug($"Setup day {(float)DayNightCycle.main.GetDay()} timewokeUp {timeWokeUp}");
            timeWokeUp = GetTimeWokeUp();
            CoroutineHost.StartCoroutine(HandleSleepDebt());
            if (Main.enhancedSleepLoaded)
            {
                BasicText message = new BasicText();
                message.ShowMessage("You should not use Need for Sleep mod with Enhanced Sleep mod", 10);
            }
            //float timeAwake = day - Player.main.timeLastSleep;
            //AddDebug("Setup time " + day);
            //AddDebug("Setup time woke up " + Player.main.timeLastSleep);
            //AddDebug("Setup timeAwake " + timeAwake);
        }

        private static bool IsLookingAtGround()
        {
            if (IsStandingStill() == false)
                return false;

            if (lookingAtBed)
            {
                lookingAtBed = false;
                return false;
            }
            float x = MainCamera.camera.transform.rotation.eulerAngles.x;
            return x > 85 && x < 90;
        }
        private static float GetTimeWokeUp()
        {
            float day = (float)DayNightCycle.main.GetDay();
            float timeLastSleep = Player.main.timeLastSleep;
            //Main.logger.LogDebug($"GetTimeWokeUp day {day} timeLastSleep {timeLastSleep}");
            if (timeLastSleep > day || timeLastSleep == 0)
                return day;
            return timeLastSleep;
        }

        private static bool CanSleep()
        {
            if (IsStandingStill() == false)
                return false;

            if (Config.calorieBurnMultSleep.Value > 0)
            {
                if (IsTooThirstyToSleep() || IsTooHungryToSleep())
                    return false;
            }
            if (Config.sleepAnytime.Value)
                return true;

            //if (notify && sleepDebt == 0)
            //    AddMessage(Language.main.Get("BedSleepTimeOut"));

            return sleepDebt > 0;
        }

        private static bool IsStandingStill()
        {
            Player player = Player.main;
            return Main.gameLoaded && player.mode == Player.Mode.Normal && player.IsUnderwaterForSwimming() == false && player.cinematicModeActive == false && player.pda.isInUse == false && player.groundMotor.grounded && player.playerController.velocity == default && DayNightCycle.main.IsInSkipTimeMode() == false;
        }

        private static bool IsTooThirstyToSleep()
        {
            if (survival.water < SurvivalConstants.kCriticalWaterThreshold)
            {
                //if (sleeping && survival.waterWarningSounds[1])
                //survival.waterWarningSounds[1].Play();

                //if (notify)
                //    AddDebug(Language.main.Get("NS_too_thirsty_to_sleep"));

                return true;
            }
            return false;
        }

        private static bool IsTooHungryToSleep()
        {
            if (survival.food < SurvivalConstants.kCriticalFoodThreshold)
            {
                //if (sleeping && survival.foodWarningSounds[1])
                //survival.foodWarningSounds[1].Play();

                //if (notify)
                //    AddDebug(Language.main.Get("NS_too_hungry_to_sleep"));

                return true;
            }
            return false;
        }

        private static float GetSleepDurationMult(Bed bed)
        {
            if (bed != myBed)
                return 1;

            if (Player.main.currentSub || Player.main.currentEscapePod)
                return 1.25f;

            return 1.5f;
        }

        private static float GetSleepDebtThreshold()
        {
            return 1 - Util.MapTo01range(Config.hoursNeedToSleep.Value, 0, 24);
        }

        private static void WakeUp()
        {
            //AddDebug($"WakeUp {DayNightCycle.main.GetDay()} sleepDebt {sleepDebt}");
            hungerUpdateTime = 0;
            UpdateSleepDebtWakeUp();
            SleepText.Hide();
            sleeping = false;
        }

        private static void UpdateSleepDebtWakeUp()
        {
            if (forcedWakeUp)
            {
                //AddDebug("forcedWakeUp");
                float day = (float)DayNightCycle.main.GetDay();
                float timeSlept = day - sleepStartTime;
                sleepDebt -= timeSlept;
                timeWokeUp = day - (sleepDebt + GetSleepDebtThreshold());
                Player.main.timeLastSleep = timeWokeUp;
                //Main.logger.LogDebug($"UpdateSleepDebtWakeUp day {day} timeSlept {timeSlept} timeWokeUp {timeWokeUp} sleepDebt {sleepDebt}");
                UpdateSleepDebt();
                forcedWakeUp = false;
            }
            else
            {
                timeWokeUp = (float)DayNightCycle.main.GetDay();
                Player.main.timeLastSleep = timeWokeUp;
                //Main.logger.LogDebug($"UpdateSleepDebtWakeUp timeWokeUp {timeWokeUp} sleepDebt {sleepDebt}");
                UpdateSleepDebt();
            }
        }

        private static void UpdateSleepDebt()
        {
            //AddDebug("UpdateSleepDebt ");
            float timeAwake = (float)DayNightCycle.main.GetDay() - timeWokeUp;
            sleepDebt = Mathf.Clamp01(timeAwake - GetSleepDebtThreshold());
            //Main.logger.LogDebug($"UpdateSleepDebt day {day} sleepDebt {sleepDebt}");
            radialBlurControl.SetAmount(sleepDebt);
            speedMod = 1f - sleepDebt * .5f;
        }

        private static void StartSleep()
        {
            sleepStartTime = (float)DayNightCycle.main.GetDay();
            //AddDebug($"StartSleep sleepDebt {sleepDebt} sleepStartTime {sleepStartTime}");
            SetHungerUpdateTime();
            UpdateSleepDebt();
            sleeping = true;
            SleepText.Show();
            if (Config.calorieBurnMultSleep.Value > 0)
                Player.main.StartCoroutine(HandleSleep());
        }

        private static void StartSleepMyBed()
        {
            //AddDebug($"StartSleepMyBed sleepDebt {sleepDebt}");
            CheckSpace();
            playerPosBeforeSleep = Player.main.transform.position;
            if (Player.main.currentSub == null)
                myBed.transform.SetParent(null);
            else
                myBed.transform.SetParent(Player.main.currentSub.transform);

            myBed.OnHandClick(Player.main.guiHand);
        }

        private static void SetBedRotation(float degrees)
        {
            Quaternion newRot = Quaternion.identity;
            newRot.eulerAngles = new Vector3(0, degrees, 0);
            myBed.transform.rotation = newRot;
        }

        private static Vector3 GetBedPosition()
        {
            Vector3 playerPos = Player.main.transform.position;
            return new Vector3(playerPos.x, playerPos.y - 2, playerPos.z);
        }

        public static IEnumerator HandleSleepDebt()
        {
            while (true)
            {
                yield return new WaitUntil(() => sleeping == false);
                UpdateSleepDebt();
                //DebugSleepDebt();
                yield return new WaitForSeconds(updateInterval);
            }
        }

        static void DebugSleepDebt()
        {
            if (Input.GetKey(KeyCode.Z))
            {
                float day = (float)DayNightCycle.main.GetDay();
                float timeAwake = day - timeWokeUp;
                AddDebug($"HandleSleepDebtAwake day {day} threshold {GetSleepDebtThreshold()}");
                AddDebug("HandleSleepDebtAwake timeWokeUp " + timeWokeUp);
                AddDebug("HandleSleepDebtAwake timeAwake " + timeAwake);
                AddDebug("HandleSleepDebtAwake hoursTillTired " + GetHoursTillTired());
                if (sleepDebt > 0)
                {
                    AddDebug("HandleSleepDebtAwake sleepDebt " + sleepDebt);
                    float sleepDebtHours = sleepDebt / 24;
                    AddDebug("HandleSleepDebtAwake sleepDebtHours " + sleepDebtHours);
                }
                if (speedMod < 1)
                    AddDebug("HandleSleepDebtAwake speedMod " + speedMod);
            }
        }

        public static IEnumerator HandleSleep()
        {
            while (sleeping)
            {
                frame = !frame;
                if (frame == false)
                    yield return null;
                //AddDebug("HandleSleepDebtSleep FrozenStats " + Player.main.IsFrozenStats());
                if (DayNightCycle.main.timePassedAsFloat > hungerUpdateTime)
                {
                    UpdateHungerSleep();
                    SetHungerUpdateTime();
                }
                if (IsTooThirstyToSleep() || IsTooHungryToSleep())
                {
                    ForceWakeUp();
                    yield break;
                }
            }
        }

        private static void ForceWakeUp()
        {
            forcedWakeUp = true;
            DayNightCycle.main.skipModeEndTime = DayNightCycle.main.timePassed;
        }

        private static void UpdateHungerSleep()
        {
            //AddDebug("UpdateHungerSleep ");
            Player.main.UnfreezeStats();
            survival.UpdateHunger();
            Player.main.FreezeStats();
        }

        private static void SetHungerUpdateTime()
        {
            hungerUpdateTime = DayNightCycle.main.timePassedAsFloat + survival.kUpdateHungerInterval;
        }

        [HarmonyPatch(typeof(Player))]
        class Player_Patch
        {
            [HarmonyPostfix, HarmonyPatch("Update")]
            static void UpdatePostfix(Player __instance)
            {
                //if (Input.GetKey(KeyCode.R))
                //AddDebug($"CanSleep  {CanSleep()}");

                if (IsLookingAtGround())
                    OnBedHandHover(myBed);
            }

            [HarmonyPostfix, HarmonyPatch("OnTakeDamage")]
            static void OnTakeDamagePostfix(Player __instance)
            {
                if (sleeping)
                {
                    //AddDebug($"Player OnTakeDamage");
                    ForceWakeUp();
                }
            }
        }

        private static int GetHoursTillTired()
        {
            if (sleepDebt > 0)
                return -1;

            float timeTired = timeWokeUp + GetSleepDebtThreshold();
            timeTired *= DayNightCycle.kDayLengthSeconds;
            DateTime dateTimeTired = DayNightCycle.ToGameDateTime(timeTired);
            DateTime dateTimeNow = DayNightCycle.ToGameDateTime(DayNightCycle.main.timePassedAsFloat);
            TimeSpan timeSpan = dateTimeTired - dateTimeNow;
            //if (Input.GetKeyDown(KeyCode.R))
            //    AddDebug($"GetHoursTillTired {timeSpan.TotalHours.ToString("0.0")} RoundToInt {Mathf.RoundToInt((float)timeSpan.TotalHours)}");

            int hours = Mathf.RoundToInt((float)timeSpan.TotalHours);
            if (hours < 0) // sleep debt is yet to update
                hours = 0;

            return hours;
        }

        static string GetTiredText()
        {
            if (Config.calorieBurnMultSleep.Value > 0)
            {
                if (survival.water < SurvivalConstants.kCriticalWaterThreshold)
                    return Language.main.Get("NS_too_thirsty_to_sleep");
                else if (survival.food < SurvivalConstants.kCriticalFoodThreshold)
                    return Language.main.Get("NS_too_hungry_to_sleep");
            }
            //if (sleepDebt == 0 && Config.sleepAnytime.Value == false)
            //    return Language.main.Get("BedSleepTimeOut");
            if (sleepDebt > 0)
                return Language.main.Get("NS_tired");

            if (Config.showTimeTillTired.Value == false && sleepDebt == 0)
                return Language.main.Get("BedSleepTimeOut");

            int hoursTillTired = GetHoursTillTired();
            //if (Input.GetKeyDown(KeyCode.R))
            //    AddDebug($"sleepDebt {sleepDebt} hoursTillTired {hoursTillTired} ");

            if (hoursTillTired == 0 && sleepDebt == 0)
                hoursTillTired = 1;

            string hours = Language.main.GetFormat("TimeFormatHoursMinutes", hoursTillTired, 0);
            string[] arr = hours.Split(',');
            hours = arr[0];
            if (hoursTillTired == 1 && Language.main.GetCurrentLanguage() == "English")
                hours = hours.Substring(0, (hours.Length - 1));

            return Language.main.Get("NS_hours_till_tired") + hours;
        }

        static void CheckSpace()
        {
            Transform playerT = Player.main.transform;
            Vector3 pos = new Vector3(playerT.position.x, playerT.position.y - 1, playerT.position.z);
            if (!Physics.Raycast(new Ray(pos, playerT.forward), 1f, checkLayerMask))
                SetBedRotation(90);
            else if (!Physics.Raycast(new Ray(pos, -playerT.forward), 1f, checkLayerMask))
                SetBedRotation(270);
            else if (!Physics.Raycast(new Ray(pos, playerT.right), 1f, checkLayerMask))
                SetBedRotation(180);
            else if (!Physics.Raycast(new Ray(pos, -playerT.right), 1f, checkLayerMask))
                SetBedRotation(0);
        }

        private static IEnumerator SpawnBed()
        {
            //AddDebug($"StartSleep SpawnBed ");
            TaskResult<GameObject> result = new TaskResult<GameObject>();
            yield return CraftData.InstantiateFromPrefabAsync(TechType.NarrowBed, result);
            GameObject bedGO = result.Get();
            bedGO.transform.position = GetBedPosition();
            bedGO.name = "NeedForSleepBed";
            Transform t = bedGO.transform.Find("collisions");
            UnityEngine.Object.Destroy(t.gameObject);
            t = bedGO.transform.Find("bed_narrow");
            UnityEngine.Object.Destroy(t.gameObject);
            myBed = bedGO.GetComponent<Bed>();
            bedGO.transform.SetParent(Player.main.transform);
            var components = bedGO.GetComponents<Component>();
            foreach (var c in components)
            {
                if (c is Bed || c is Transform)
                    continue;

                UnityEngine.Object.Destroy(c);
            }
        }

        private static void OnBedHandHover(Bed bed)
        {
            if (Player.main.guiHand.IsFreeToInteract() == false)
                return;

            HandReticle.main.SetText(HandReticle.TextType.HandSubscript, GetTiredText(), false);
            if (CanSleep())
            {
                HandReticle.main.SetText(HandReticle.TextType.Hand, bed.handText, true, Button.LeftHand);
                HandReticle.main.SetIcon(HandReticle.IconType.Hand);
            }
        }

        [HarmonyPatch(typeof(Bed))]
        private class Bed_Patch
        {
            [HarmonyPrefix, HarmonyPatch("GetCanSleep")]
            public static void GetCanSleepPrefix(Bed __instance, Player player, ref bool notify, ref bool __result)
            {
                notify = false;
                //AddDebug($"Bed GetCanSleep {__result}");
            }
            [HarmonyPostfix, HarmonyPatch("GetCanSleep")]
            public static void GetCanSleepPostfix(Bed __instance, Player player, bool notify, ref bool __result)
            {
                __result = CanSleep();
                //AddDebug($"Bed GetCanSleep {__result}");
            }
            [HarmonyPostfix, HarmonyPatch("OnHandHover")]
            public static void OnHandHoverPostfix(Bed __instance, GUIHand hand)
            {
                lookingAtBed = true;
                OnBedHandHover(__instance);
            }

            [HarmonyPrefix, HarmonyPatch("EnterInUseMode")]
            public static void EnterInUseModePrefix(Bed __instance, Player player)
            {
                sleepDurationMult = GetSleepDurationMult(__instance);
            }
            [HarmonyPostfix, HarmonyPatch("EnterInUseMode")]
            public static void EnterInUseModePostfix(Bed __instance, Player player)
            {
                StartSleep();
                //player.StartCoroutine(StartSleep());
            }
            [HarmonyPostfix, HarmonyPatch("ExitInUseMode")]
            public static void ExitInUseModePostfix(Bed __instance, Player player, bool skipCinematics)
            {
                //AddDebug("Bed ExitInUseMode ");
                if (sleeping)
                    WakeUp();

                if (__instance == myBed)
                {
                    Quaternion newRot = Quaternion.identity;
                    newRot.eulerAngles = new Vector3(0, myBed.transform.rotation.eulerAngles.y + 180, 0);
                    myBed.transform.rotation = newRot; // face the same direction on wake up
                    player.StartCoroutine(AttachBedToPlayer(__instance, player));
                }
            }

            private static void RestorePlayerPos(Player player)
            {
                Player.main.transform.position = playerPosBeforeSleep;
            }

            private static IEnumerator AttachBedToPlayer(Bed bed, Player player)
            {
                yield return new WaitUntil(() => player.cinematicModeActive == false);
                //AddDebug($"AttachToPlayer ");
                RestorePlayerPos(player);
                bed.transform.SetParent(player.transform);
                bed.transform.localPosition = myBedLocalPos;
            }

            [HarmonyPrefix, HarmonyPatch("GetSide")]
            public static bool GetSidePretfix(Bed __instance, Player player, ref BedSide __result)
            {
                if (__instance == myBed)
                {
                    //AddDebug("GetSide myBed");
                    //__result = myBed.transform.InverseTransformPoint(player.transform.position).x >= 0 ? BedSide.Right : BedSide.Left;
                    __result = BedSide.Right;
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(UnderwaterMotor), "AlterMaxSpeed")]
        class UnderwaterMotor_AlterMaxSpeed_Patch
        {
            public static void Postfix(UnderwaterMotor __instance, float inMaxSpeed, ref float __result)
            {
                //AddDebug("UnderwaterMotor AlterMaxSpeed");
                if (seaglideEquipped == false && speedMod > 0 && speedMod < 1)
                    __result *= speedMod;
            }
        }

        [HarmonyPatch(typeof(GroundMotor), "ApplyInputVelocityChange")]
        class GroundMotor_ApplyInputVelocityChange_Patch
        {
            public static void Prefix(GroundMotor __instance, ref Vector3 velocity, ref Vector3 __result)
            {
                if (Main.gameLoaded == false)
                    return;

                if (speedMod > 0 && speedMod < 1 && __instance.grounded)
                {
                    //AddDebug("GroundMotor ApplyInputVelocityChange Prefix " + speedMod);
                    __instance.sprintPressed = false;
                    __instance.movementInputDirection = __instance.movementInputDirection.normalized * speedMod;
                }
            }
        }

        [HarmonyPatch(typeof(Survival))]
        class Survival_Start_patch
        {
            [HarmonyPrefix, HarmonyPatch("UpdateStats")]
            static bool UpdateStatsPrefix(Survival __instance, ref float timePassed, ref float __result)
            {
                if (Main.gameLoaded == false)
                    return false;

                if (sleeping && Config.calorieBurnMultSleep.Value > 0)
                {
                    //Main.logger.LogInfo("UpdateStats sleeping DayNightCycle.timePassed " + (int)DayNightCycle.main.timePassed + " timePassed " + (int)timePassed);
                    timePassed *= Config.calorieBurnMultSleep.Value;
                    //AddDebug($"UpdateStats sleeping timePassed {timePassed}");
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(DayNightCycle))]
        class DayNightCycle_patch
        {
            [HarmonyPrefix, HarmonyPatch("SkipTime")]
            static void SkipTimePrefix(DayNightCycle __instance, ref float timeAmount, ref float skipDuration, ref bool __result)
            {
                skipDuration = Config.hoursNeedToSleep.Value * sleepDurationMult; // game hour is 1 real second
                timeAmount = skipDuration * oneHourDuration;
                //AddDebug(" SkipTime amount " + timeAmount + " duration " + skipDuration);
            }
        }

        [HarmonyPatch(typeof(CrawlerAttackLastTarget))]
        class CrawlerAttackLastTarget_Patch
        {
            [HarmonyPostfix, HarmonyPatch("Evaluate")]
            static void EvaluatePostfix(CrawlerAttackLastTarget __instance)
            {
                if (sleeping && __instance.crawler.IsOnSurface() && __instance.lastTarget.target && __instance.lastTarget.target.name == "Player")
                {
                    //AddDebug($" CrawlerAttackLastTarget Evaluate Player");
                    AttackPlayerSleep(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(GameInput))]
        class GameInput_Patch
        {
            static Button delayedButton = Button.None;
            static Button heldButtonWasDelayed = Button.None;
            static bool pressDelayedButton;
            private static bool pressDelayedHeldButton;

            [HarmonyPostfix, HarmonyPatch("GetLookDelta")]
            static void GetLookDeltaPostfix(GameInput __instance, ref Vector2 __result)
            {
                if (__result == default || sleepDebt == 0 || Player.main.mode == Player.Mode.LockedPiloting)
                    return;

                float mod = 1 - sleepDebt * .5f;
                __result *= mod;
                //AddDebug($"GetLookDelta {__result}");
            }
            //[HarmonyPostfix, HarmonyPatch("GetMoveDirection")]
            static void GetMoveDirectionPostfix(GameInput __instance, ref Vector3 __result)
            {
                if (__result == default || sleepDebt == 0 || Player.main.mode != Player.Mode.Normal)
                    return;

                //AddDebug($"GetMoveDirection {__result}");
            }
            //[HarmonyPrefix, HarmonyPatch("SetAutoMove")]
            static bool SetAutoMovePrefix(GameInput __instance, bool _autoMove)
            {
                if (_autoMove && sleepDebt > 0)
                    return false;

                return true;
            }
            [HarmonyPostfix, HarmonyPatch("GetButtonDown")]
            static void ScanInputsPostfix(GameInput __instance, Button button, ref bool __result)
            {
                if (Main.gameLoaded == false || Time.timeScale == 0)
                    return;

                if (__result)
                {
                    if (Main.tweaksFixesLoaded)
                    {
                        if (button == Button.AltTool)
                        {
                            PlayerTool tool = Inventory.main.GetHeldTool();
                            if (tool is Flare)
                                return;
                        }
                    }
                    if (button == Button.LeftHand)
                    {

                        if (IsLookingAtGround() && CanSleep())
                        {
                            StartSleepMyBed();
                            return;
                        }
                    }
                    if (seaglideEquipped)
                    {
                        if (button == Button.RightHand || button == Button.AltTool)
                            return;
                    }
                    else if (builderEquipped)
                    {
                        if (button == Button.RightHand || button == Button.LeftHand)
                            return;
                    }
                    if (Player.main.currentMountedVehicle)
                    {
                        if (Player.main.currentMountedVehicle is SeaMoth)
                        {
                            if (button == Button.RightHand)
                                return;
                        }
                        else if (Player.main.currentMountedVehicle is Exosuit)
                        {
                            Exosuit exosuit = Player.main.currentMountedVehicle as Exosuit;
                            //AddDebug($"LeftArmType {exosuit.currentLeftArmType} RightArmType {exosuit.currentRightArmType}");
                            if (exosuit.currentLeftArmType != TechType.ExosuitClawArmModule && button == Button.LeftHand)
                                return;
                            else if (exosuit.currentRightArmType != TechType.ExosuitClawArmModule && button == Button.RightHand)
                                return;
                        }
                    }
                }
                if (button == delayedButton)
                {
                    __result = false;
                    if (pressDelayedButton)
                    {
                        __result = true;
                        pressDelayedButton = false;
                        delayedButton = Button.None;
                        //AddDebug("pressDelayedButton");
                    }
                    return;
                }
                if (__result && delayedButton == Button.None && sleepDebt > 0 && delayableButtons.Contains(button))
                {
                    float delayTime = sleepDebt - UnityEngine.Random.value;
                    if (delayTime > 0)
                    {
                        __result = false;
                        Player.main.StartCoroutine(DelayInput(button, delayTime));
                    }
                }
            }

            private static IEnumerator DelayInput(Button button, float delayTime)
            {
                //AddDebug($"DelayInput {button}");
                delayedButton = button;
                yield return new WaitForSeconds(delayTime);
                pressDelayedButton = true;
            }

            private static IEnumerator DelayHeldInput(Button button, float delayTime)
            {
                //AddDebug($"DelayHeldInput {button}");
                //heldButtonWasDelayed = Button.None;
                delayedButton = button;
                yield return new WaitForSeconds(delayTime);
                pressDelayedHeldButton = true;
            }

            [HarmonyPostfix, HarmonyPatch("GetButtonHeld")]
            static void GetButtonHeldPostfix(GameInput __instance, Button button, ref bool __result)
            {
                if (builderEquipped)
                {
                    if (button == Button.LeftHand || button == Button.RightHand)
                        return;
                }
                if (button == heldButtonWasDelayed)
                {
                    if (__result == false)
                        heldButtonWasDelayed = Button.None;
                    else
                        return;
                }
                if (button == delayedButton)
                {
                    __result = false;
                    if (pressDelayedHeldButton)
                    {
                        __result = true;
                        pressDelayedHeldButton = false;
                        heldButtonWasDelayed = delayedButton;
                        delayedButton = Button.None;
                        //AddDebug("GetButtonHeld pressDelayedButton");
                    }
                    return;
                }
                if (__result && delayedButton == Button.None && sleepDebt > 0 && delayableButtons.Contains(button))
                {
                    float delayTime = sleepDebt - UnityEngine.Random.value;
                    if (delayTime > 0)
                    {
                        //AddDebug($"GetButtonHeld DelayInput {button}");
                        __result = false;
                        Player.main.StartCoroutine(DelayHeldInput(button, delayTime));
                    }
                }
            }
        }

        static void AttackPlayerSleep(CrawlerAttackLastTarget crawlerAttackLastTarget)
        {
            Player.main.cinematicModeActive = false;
            crawlerAttackLastTarget.swimBehaviour.SwimTo(crawlerAttackLastTarget.lastTarget.target.transform.position, crawlerAttackLastTarget.moveVelocity);
        }

        [HarmonyPatch(typeof(Seaglide))]
        class Seaglide_Patch
        {
            [HarmonyPostfix, HarmonyPatch("OnDraw")]
            static void OnDrawPostfix(Seaglide __instance)
            {
                seaglideEquipped = true;
                //AddDebug("Seaglide OnDraw");
            }
            [HarmonyPostfix, HarmonyPatch("OnHolster")]
            static void OnHolsterPostfix(Seaglide __instance)
            {
                //AddDebug("Seaglide OnHolster");
                seaglideEquipped = false;
            }
        }

        [HarmonyPatch(typeof(BuilderTool))]
        class BuilderTool_Patch
        {
            [HarmonyPostfix, HarmonyPatch("OnDraw")]
            static void OnDrawPostfix(BuilderTool __instance)
            {
                builderEquipped = true;
                //AddDebug("BuilderTool OnDraw");
            }
            [HarmonyPostfix, HarmonyPatch("OnHolster")]
            static void OnHolsterPostfix(BuilderTool __instance)
            {
                //AddDebug("BuilderTool OnHolster");
                builderEquipped = false;
            }
        }



    }
}
