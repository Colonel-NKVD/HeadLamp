using HarmonyLib;
using SDG.Unturned;
using System.Linq;

namespace HeadLamp
{
    [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.ServerSetVisualToggleState))]
    public class Patch_ServerSetVisualToggleState
    {
        public static bool Prefix(PlayerClothing __instance, EVisualToggleType type, ref bool isVisible)
        {
            // Если игрок пытается включить свет на севшем фонаре
            if (isVisible && __instance.glassesAsset != null && __instance.glassesQuality <= 0)
            {
                var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == __instance.glassesAsset.id);
                if (config != null)
                {
                    // 1. Принудительно гасим логическое состояние
                    if (__instance.glassesState != null && __instance.glassesState.Length > 0)
                    {
                        __instance.glassesState[0] = 0;
                    }

                    // 2. ХИТРОСТЬ: "Перенадеваем" тот же фонарь.
                    // Мы посылаем клиенту команду надеть фонарь с тем же ID, но с выключенными байтами.
                    // Это мгновенно сбрасывает клиентский визуальный эффект света.
                    __instance.askWearGlasses(
                        __instance.glassesAsset.id, 
                        0, // Качество 0
                        __instance.glassesState, 
                        true // Принудительно
                    );

                    // 3. Останавливаем оригинальный метод, чтобы он не мешал нашей "перезагрузке"
                    return false; 
                }
            }
            return true;
        }
    }
}
