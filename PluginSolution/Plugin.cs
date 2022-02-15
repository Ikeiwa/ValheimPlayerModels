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
using ValheimPlayerModels.Loaders;

namespace ValheimPlayerModels
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("valheim.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public static string playerModelsPath => Path.Combine(Environment.CurrentDirectory, "PlayerModels");

        public static Dictionary<string, string> playerModelBundlePaths { get; private set; }
        public static Dictionary<string, AvatarLoaderBase> playerModelBundleCache = new Dictionary<string, AvatarLoaderBase>();

        private void Awake()
        {
            PluginConfig.InitConfig(Config);

            Config.SettingChanged += ConfigOnSettingChanged;

            if (!Directory.Exists(playerModelsPath))
                Directory.CreateDirectory(playerModelsPath);

            RefreshBundlePaths(false);

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

                    foreach (AvatarLoaderBase loader in playerModelBundleCache.Values)
                    {
                        if(loader != null) loader.Unload();
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
            else if (e.ChangedSetting.Definition.Key == "SelectedAvatar")
            {
                PlayerModel playerModel = null;

                if (Player.m_localPlayer != null)
                    playerModel = Player.m_localPlayer.gameObject.GetComponent<PlayerModel>();
                else
                    playerModel = FindObjectOfType<PlayerModel>();

                if (playerModel != null)
                {
                    if (playerModel.playerModelLoaded)
                    {
                        playerModel.ReloadAvatar();
                    }
                }
            }
        }

        public static void RefreshBundlePaths(bool silent = true)
        {
            playerModelBundlePaths = new Dictionary<string, string>();

            string[] files = Directory.GetFiles(playerModelsPath, "*.*", SearchOption.AllDirectories)
                .Where(s => s.EndsWith(".valavtr") || s.EndsWith(".vrm")).ToArray();

            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file).ToLower();

                if (!silent)
                    Debug.Log("Found avatar : " + fileName);
                if(!playerModelBundlePaths.ContainsKey(fileName))
                    playerModelBundlePaths.Add(fileName, file);
            }
        }


    }
}
#endif