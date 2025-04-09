﻿using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using FMOD;
using HarmonyLib;
using Nautilus.Handlers;
using Nautilus.Options;
using Nautilus.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UWE;


namespace Need_for_Sleep
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("com.snmodding.nautilus")]
    public class Main : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "qqqbbb.subnautica.NeedForSleep";
        public const string PLUGIN_NAME = "Need for Sleep";
        public const string PLUGIN_VERSION = "1.0.0";
        public static ManualLogSource logger { get; private set; }
        static string configPath = Paths.ConfigPath + Path.DirectorySeparatorChar + PLUGIN_NAME + Path.DirectorySeparatorChar + "Config.cfg";
        public static ConfigFile config;
        internal static OptionsMenu options;
        public static bool gameLoaded;
        public static bool enhancedSleepLoaded;
        public static bool tweaksFixesLoaded;

        private void Start()
        {
            Setup();
        }

        private void Setup()
        {
            logger = base.Logger;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), $"{PLUGIN_GUID}");
            Logger.LogInfo($"Plugin {PLUGIN_GUID} {PLUGIN_VERSION} is loaded! ");
            SaveUtils.RegisterOnQuitEvent(OnApplicationQuit);
            SaveUtils.RegisterOnFinishLoadingEvent(LoadedGameSetup);
            LanguageHandler.RegisterLocalizationFolder();
            config = new ConfigFile(configPath, false);
            Need_for_Sleep.Config.Bind();
            options = new OptionsMenu();
            OptionsPanelHandler.RegisterModOptions(options);
            GetLoadedMods();
            //SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(OnSceneLoaded);
        }

        private void LoadedGameSetup()
        {
            gameLoaded = true;
            Patches.Setup();
        }

        private void OnApplicationQuit()
        {
            //Logger.LogDebug("Need for Sleep OnApplicationQuit");
            Patches.ResetVars();
            gameLoaded = false;
        }

        [HarmonyPatch(typeof(SaveLoadManager))]
        class SaveLoadManager_Patch
        {
            [HarmonyPrefix]
            [HarmonyPatch("SaveToDeepStorageAsync", new Type[0])]
            public static void SaveToDeepStorageAsyncPrefix(SaveLoadManager __instance)
            {
                Patches.SaveTimeWokeUp();
            }
            [HarmonyPostfix]
            [HarmonyPatch("SaveToDeepStorageAsync", new Type[0])]
            public static void SaveToDeepStorageAsyncPostfix(SaveLoadManager __instance)
            { // runs after nautilus SaveEvent
              //AddDebug("SaveToDeepStorageAsync");
                config.Save();
            }
        }

        public static void GetLoadedMods()
        {
            enhancedSleepLoaded = Chainloader.PluginInfos.ContainsKey("Cookay_EnhancedSleep");
            tweaksFixesLoaded = Chainloader.PluginInfos.ContainsKey("qqqbbb.subnautica.tweaksAndFixes");
        }



    }
}