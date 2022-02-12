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
        public static ConfigFile config;
        public static ConfigEntry<bool> enablePlayerModels;
        public static ConfigEntry<bool> enableCustomRagdoll;
        public static ConfigEntry<string> serverUrl;
        public static ConfigEntry<KeyCode> reloadKey;
        public static ConfigEntry<KeyCode> actionMenuKey;

        public static void InitConfig(ConfigFile _config)
        {
            config = _config;
            enablePlayerModels = config.Bind("General", "EnablePlayerModels", true,
                "Toggle the use of custom player models.");
            enableCustomRagdoll = config.Bind("General", "EnableCustomRagdoll", true,
                "Toggle the use of custom player models for ragdolls.");
            serverUrl = config.Bind("General", "ServerURL", "",
                "Player Models Server URL, keep empty for local player models only.");
            reloadKey = config.Bind("General", "ReloadKey", KeyCode.F5,
                "The key to reload the player models.");
            actionMenuKey = config.Bind("General", "ActionMenuKey", KeyCode.AltGr,
                "The key to open the action menu.");
        }
    }
}
#endif