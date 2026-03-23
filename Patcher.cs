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
            bool isGlassesOff = __instance.glassesState != null && __instance.glassesState.Length > 0 && __instance.glassesState[0] == 0;

            if (isGlassesOff)
            {
                bool isGlassesInConfig = __instance.glassesAsset != null && HeadLamp.Instance.Configuration.Instance.Lamps.Any(x => x.ItemID == __instance.glassesAsset.id);

                // Если предмет в конфиге и сломан — не даем включить
                if (isGlassesInConfig && __instance.glassesQuality <= 0) return false;
            }
            return true;
        }
    }
}
