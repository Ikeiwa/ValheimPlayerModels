#if PLUGIN
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

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
            Debug.LogWarning(__instance.m_nview.m_zdo.m_uid.m_userID);
        }
    }
}
#endif