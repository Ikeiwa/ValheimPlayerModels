#if PLUGIN
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ValheimPlayerModels
{
	[HarmonyPatch(typeof(Player), "Awake")]
	static class Patch_Player_Awake
	{
		[HarmonyPostfix]
		static void Postfix(Player __instance)
        {
            if(PluginConfig.enablePlayerModels.Value)
                __instance.gameObject.AddComponent<PlayerModel>();
        }
	}

    [HarmonyPatch(typeof(VisEquipment), "UpdateLodgroup")]
    static class Patch_VisEquipment_UpdateLodgroup
    {
        [HarmonyPostfix]
        static void Postfix(VisEquipment __instance)
        {
            if (PluginConfig.enablePlayerModels.Value)
                __instance.GetComponent<PlayerModel>()?.ToggleEquipments();
        }
    }

    [HarmonyPatch(typeof(Ragdoll), "Start")]
    static class Patch_Ragdoll_Start
    {
        [HarmonyPostfix]
        static void Postfix(Ragdoll __instance)
        {
            if (PluginConfig.enableCustomRagdoll.Value)
            {
                if (__instance.gameObject.name.StartsWith("Player"))
                {
                    if (ZNet.instance)
                    {
                        PlayerModel[] playerModels = Object.FindObjectsOfType<PlayerModel>();
                        PlayerModel player = playerModels.FirstOrDefault(p =>
                            p.player.GetZDOID().m_userID == __instance.m_nview.m_zdo.m_uid.m_userID);

                        if (player) player.SetupRagdoll(__instance);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Terminal), "TryRunCommand")]
    static class Patch_Terminal_TryRunCommand
    {
        [HarmonyPostfix]
        static void Postfix(Terminal __instance, string text, bool silentFail = false, bool skipAllowedCheck = false)
        {
            string command = text.ToLower();
            string[] param = command.Split(' ');
            if (command.StartsWith("anim") && param.Length == 3)
            {
                if (Player.m_localPlayer)
                {
                    PlayerModel playerModel = Player.m_localPlayer.GetComponent<PlayerModel>();
                    if (playerModel)
                    {
                        if (bool.TryParse(param[2], out bool valueBool))
                            if (playerModel.SetBool(param[1], valueBool)) return;

                        if (int.TryParse(param[2], out int valueInt))
                            if (playerModel.SetInt(param[1], valueInt)) return;

                        if (float.TryParse(param[2], out float valuefloat))
                            playerModel.SetFloat(param[1], valuefloat);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(GameCamera), "UpdateMouseCapture")]
    static class Patch_GameCamera_UpdateMouseCapture
    {
        [HarmonyPrefix]
        static bool Prefix(GameCamera __instance)
        {
            if (PluginConfig.enablePlayerModels.Value && (PlayerModel.showActionMenu || PlayerModel.showAvatarMenu))
                return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(Player), "SetMouseLook")]
    static class Patch_Player_SetMouseLook
    {
        [HarmonyPrefix]
        static bool Prefix(Player __instance)
        {
            if (PluginConfig.enablePlayerModels.Value && (PlayerModel.showActionMenu || PlayerModel.showAvatarMenu))
                return false;
            return true;
        }
    }
}
#endif