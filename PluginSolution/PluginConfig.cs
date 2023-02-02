#if PLUGIN
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace ValheimPlayerModels
{
    public static class PluginConfig
    {
        public const string PLUGIN_GUID = "ValheimPlayerModels";
        public const string PLUGIN_NAME = "Valheim Player Models";
        public const string PLUGIN_VERSION = "1.0.1";

        public static ConfigFile config;
        public static ConfigEntry<bool> enablePlayerModels;
        public static ConfigEntry<bool> enableCustomRagdoll;
        public static ConfigEntry<string> serverUrl;
        public static ConfigEntry<string> selectedAvatar;
        public static ConfigEntry<KeyboardShortcut> reloadKey;
        public static ConfigEntry<KeyboardShortcut> actionMenuKey;
        public static ConfigEntry<KeyboardShortcut> avatarMenuKey;

        public static void InitConfig(ConfigFile _config)
        {
            config = _config;
            enablePlayerModels = config.Bind("General", "EnablePlayerModels", true,
                new ConfigDescription("Toggle the use of custom player models.", null,
                    new ConfigurationManagerAttributes {Order = 1}));
                
            enableCustomRagdoll = config.Bind("General", "EnableCustomRagdoll", true,
                new ConfigDescription("Toggle the use of custom player models for ragdolls.", null,
                    new ConfigurationManagerAttributes { Order = 2 }));

            selectedAvatar = config.Bind("General", "SelectedAvatar", "",
                new ConfigDescription("Selected avatar name, leave empty for automatic",null,
                    new ConfigurationManagerAttributes{Browsable = false}));

            reloadKey = config.Bind("General", "ReloadKey", new KeyboardShortcut(KeyCode.F4),
                new ConfigDescription("The key to reload the player models.", null,
                    new ConfigurationManagerAttributes { Order = 3 }));
            
            actionMenuKey = config.Bind("General", "ActionMenuKey", new KeyboardShortcut(KeyCode.G),
                new ConfigDescription("The key to open the action menu.", null,
                    new ConfigurationManagerAttributes { Order = 4 }));

            avatarMenuKey = config.Bind("General", "AvatarMenuKey", new KeyboardShortcut(KeyCode.End),
                new ConfigDescription("The key to open the avatar selection menu.", null,
                    new ConfigurationManagerAttributes { Order = 4 }));

            serverUrl = config.Bind("General", "ServerURL", "",
                new ConfigDescription("Player Models Server URL, keep empty for local player models only.", null,
                    new ConfigurationManagerAttributes { Order = 6 }));
        }
    }
}
#endif