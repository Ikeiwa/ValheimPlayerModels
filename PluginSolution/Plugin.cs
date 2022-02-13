#if PLUGIN
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
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
        public static Dictionary<string, AssetBundle> playerModelBundleCache = new Dictionary<string, AssetBundle>();

        private void Awake()
        {
            PluginConfig.InitConfig(Config);

            Config.SettingChanged += ConfigOnSettingChanged;

            if (!Directory.Exists(playerModelsPath))
                Directory.CreateDirectory(playerModelsPath);

            RefreshBundlePaths();

            var harmony = new Harmony(PluginInfo.PLUGIN_GUID+".patch");
            harmony.PatchAll();

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void Update()
        {
            if (PluginConfig.reloadKey.Value.IsDown())
            {
                if (PluginConfig.enablePlayerModels.Value)
                {
                    PlayerModel[] playerModels = FindObjectsOfType<PlayerModel>();
                    bool canReload = true;

                    foreach (PlayerModel playerModel in playerModels)
                    {
                        if (!playerModel.playerModelLoaded)
                        {
                            canReload = false;
                            break;
                        }
                    }

                    if (!canReload) return;

                    foreach (PlayerModel playerModel in playerModels)
                    {
                        playerModel.ToggleAvatar(false);
                        playerModel.Unload();
                        Destroy(playerModel);
                    }

                    foreach (AssetBundle assetBundle in playerModelBundleCache.Values)
                    {
                        if(assetBundle) assetBundle.Unload(true);
                    }
                    playerModelBundleCache.Clear();
                    RefreshBundlePaths();

                    Player[] players = FindObjectsOfType<Player>();
                    foreach (Player player in players)
                    {
                        player.gameObject.AddComponent<PlayerModel>();
                    }
                }
            }
        }

        private void ConfigOnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            if (e.ChangedSetting.Definition.Key == "EnablePlayerModels")
            {
                PlayerModel[] playerModels = FindObjectsOfType<PlayerModel>();
                if ((bool) e.ChangedSetting.BoxedValue)
                {
                    foreach (PlayerModel playerModel in playerModels)
                    {
                        playerModel.ToggleAvatar();
                    }

                    Player[] players = FindObjectsOfType<Player>();
                    foreach (Player player in players)
                    {
                        if (player.GetComponent<PlayerModel>() == null)
                        {
                            player.gameObject.AddComponent<PlayerModel>();
                        }
                    }
                }
                else
                {
                    foreach (PlayerModel playerModel in playerModels)
                    {
                        playerModel.ToggleAvatar(false);
                    }
                }
            }
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