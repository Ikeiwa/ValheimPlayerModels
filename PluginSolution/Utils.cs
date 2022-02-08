#if PLUGIN
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace ValheimPlayerModels
{
    public static class Utils
    {
        public static Tout GetField<Tin, Tout>(this Tin self, string fieldName)
        {
            return AccessTools.FieldRefAccess<Tin, Tout>(fieldName).Invoke(self);
        }
    }
}
#endif
