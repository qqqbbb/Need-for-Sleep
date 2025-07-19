using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus.Handlers;
using Nautilus.Options;
using Nautilus.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;


namespace Need_for_Sleep
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInDependency("com.snmodding.nautilus")]
    public class Main : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "qqqbbb.subnautica.NeedForSleep";
        public const string PLUGIN_NAME = "Need for Sleep";
        public const string PLUGIN_VERSION = "1.5.0";
        public static ManualLogSource logger { get; private set; }
        static string configPath = Paths.ConfigPath + Path.DirectorySeparatorChar + PLUGIN_NAME + Path.DirectorySeparatorChar + "Config.cfg";
        public static ConfigFile config;
        internal static OptionsMenu options;
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
            SaveUtils.RegisterOnQuitEvent(OnQuit);
            SaveUtils.RegisterOnFinishLoadingEvent(LoadedGameSetup);
            //WaitScreenHandler.RegisterLateLoadTask("Need for Sleep", task => LoadedGameSetup());
            LanguageHandler.RegisterLocalizationFolder();
            config = new ConfigFile(configPath, false);
            Need_for_Sleep.Config.Bind();
            options = new OptionsMenu();
            OptionsPanelHandler.RegisterModOptions(options);
            GetLoadedMods();
            Logger.LogInfo($"Plugin {PLUGIN_GUID} {PLUGIN_VERSION} is loaded ");
        }

        private void LoadedGameSetup()
        {
            //ErrorMessage.AddDebug("LoadedGameSetup");
            Patches.Setup();
        }

        private void OnQuit()
        {
            //Logger.LogDebug("Need for Sleep OnQuit");
            Patches.ResetVars();
        }

        public static void GetLoadedMods()
        {
            enhancedSleepLoaded = Chainloader.PluginInfos.ContainsKey("Cookay_EnhancedSleep");
            tweaksFixesLoaded = Chainloader.PluginInfos.ContainsKey("qqqbbb.subnautica.tweaksAndFixes");
        }



    }
}