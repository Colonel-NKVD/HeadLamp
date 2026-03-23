using HarmonyLib;
using SDG.Unturned;
using System.Linq;

namespace HeadLamp
{
    [HarmonyPatch(typeof(PlayerClothing), "ReceiveToggleVisual")]
    public class Patch_ToggleVisual
    {
        [HarmonyPrefix]
        public static bool Prefix(PlayerClothing __instance)
        {
            // Используем NON_COSMETIC для ПНВ и фонарей
            if (!__instance.isVisualToggleActive(EVisualToggleType.NON_COSMETIC)) 
            {
                bool isHat = __instance.hatAsset != null && HeadLamp.Instance.Configuration.Instance.Lamps.Any(x => x.ItemID == __instance.hatAsset.id);
                bool isGlasses = __instance.glassesAsset != null && HeadLamp.Instance.Configuration.Instance.Lamps.Any(x => x.ItemID == __instance.glassesAsset.id);

                if (isHat && __instance.hatQuality <= 0) return false; 
                if (isGlasses && __instance.glassesQuality <= 0) return false;
            }
            return true;
        }
    }
}
