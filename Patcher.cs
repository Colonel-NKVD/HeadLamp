using HarmonyLib;
using SDG.Unturned;
using System.Linq;

namespace HeadLamp
{
    // В современных версиях Unturned функция переключения называется ReceiveToggleVisual
    [HarmonyPatch(typeof(PlayerClothing), "ReceiveToggleVisual")]
    public class Patch_ToggleVisual
    {
        [HarmonyPrefix]
        public static bool Prefix(PlayerClothing __instance)
        {
            if (!__instance.isVisualToggleActive) // Пытаются включить
            {
                bool isHat = __instance.hatAsset != null && HeadLamp.Instance.Configuration.Instance.Lamps.Any(x => x.ItemID == __instance.hatAsset.id);
                bool isGlasses = __instance.glassesAsset != null && HeadLamp.Instance.Configuration.Instance.Lamps.Any(x => x.ItemID == __instance.glassesAsset.id);

                if (isHat && __instance.hatQuality <= 0) return false; // Запрещаем, если шапка сломана
                if (isGlasses && __instance.glassesQuality <= 0) return false; // Запрещаем, если ПНВ сломано
            }
            return true;
        }
    }
}
