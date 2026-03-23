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
            // Проверяем: если сейчас выключено (state[0] == 0), значит игрок пытается ВКЛЮЧИТЬ
            // Проверяем очки
            bool isGlassesOff = __instance.glassesState != null && __instance.glassesState.Length > 0 && __instance.glassesState[0] == 0;
            // Проверяем шапку
            bool isHatOff = __instance.hatState != null && __instance.hatState.Length > 0 && __instance.hatState[0] == 0;

            if (isGlassesOff || isHatOff)
            {
                bool isHatInConfig = __instance.hatAsset != null && HeadLamp.Instance.Configuration.Instance.Lamps.Any(x => x.ItemID == __instance.hatAsset.id);
                bool isGlassesInConfig = __instance.glassesAsset != null && HeadLamp.Instance.Configuration.Instance.Lamps.Any(x => x.ItemID == __instance.glassesAsset.id);

                if (isHatInConfig && __instance.hatQuality <= 0) return false; 
                if (isGlassesInConfig && __instance.glassesQuality <= 0) return false;
            }
            return true;
        }
    }
}
