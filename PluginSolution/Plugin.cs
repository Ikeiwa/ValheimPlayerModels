#if PLUGIN
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace ValheimPlayerModels
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("valheim.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static string playerModelsPath => Path.Combine(Environment.CurrentDirectory, "PlayerModels");

        public static Dictionary<string, string> playerModelBundlePaths { get; private set; }
        public static Dictionary<string, AssetBundle> playerModelBundleCache;

        private void Awake()
        {
            if (!Directory.Exists(playerModelsPath))
                Directory.CreateDirectory(playerModelsPath);

            playerModelBundleCache = new Dictionary<string, AssetBundle>();
            RefreshBundlePaths();

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            var harmony = new Harmony(PluginInfo.PLUGIN_GUID+".patch");
            harmony.PatchAll();
        }

        private void Update()
        {
        }

        public static void RefreshBundlePaths()
        {
            playerModelBundlePaths = new Dictionary<string, string>();

            string[] files = Directory.GetFiles(playerModelsPath, "*.valavtr");
            foreach (string file in files)
            {
                Debug.Log("Found avatar : " + Path.GetFileNameWithoutExtension(file).ToLower());
                playerModelBundlePaths.Add(Path.GetFileNameWithoutExtension(file).ToLower(), file);
            }
        }
    }
}
#endif