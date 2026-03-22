using HarmonyLib;
using SDG.Unturned;

namespace HeadLamp
{
    [HarmonyPatch(typeof(PlayerClothing), "ReceiveToggleNightVision")]
    public class Patch_ToggleNightVision
    {
        [HarmonyPrefix]
        public static bool Prefix(PlayerClothing __instance)
        {
            // Если ПНВ сейчас выключен, и игрок пытается его ВКЛЮЧИТЬ
            if (!__instance.isNightVisionActive)
            {
                if (__instance.headQuality <= 0)
                {
                    // Отменяем выполнение оригинального метода (фонарь не включится)
                    return false; 
                }
            }
            return true;
        }
    }
}
