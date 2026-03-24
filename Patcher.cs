using HarmonyLib;
using SDG.Unturned;
using System.Linq;

namespace HeadLamp
{
    // 1. БЛОКИРОВКА ВКЛЮЧЕНИЯ (Кнопка 'N')
    [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.ServerSetVisualToggleState))]
    public class Patch_ServerSetVisualToggleState
    {
        public static bool Prefix(PlayerClothing __instance, EVisualToggleType type, ref bool wantOn)
        {
            // Используем (int)type == 2, чтобы избежать ошибки CS0117
            // 2 — это индекс для VISION (ПНВ и налобники)
            if (wantOn && (int)type == 2)
            {
                if (__instance.glassesAsset != null && __instance.glassesQuality <= 0)
                {
                    var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == __instance.glassesAsset.id);
                    if (config != null)
                    {
                        wantOn = false; 
                    }
                }
            }
            
            return true;
        }
    }

    // 2. АВТО-ВЫКЛЮЧЕНИЕ ПРИ РАЗРЯДКЕ В НОЛЬ
    [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.sendUpdateGlassesQuality))]
    public class Patch_sendUpdateGlassesQuality
    {
        public static void Postfix(PlayerClothing __instance)
        {
            if (__instance.glassesAsset != null && __instance.glassesQuality <= 0)
            {
                // Проверяем состояние байта (включен ли свет)
                if (__instance.glassesState != null && __instance.glassesState.Length > 0 && __instance.glassesState[0] != 0)
                {
                    var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == __instance.glassesAsset.id);
                    if (config != null)
                    {
                        // Принудительно вызываем выключение через индекс 2
                        __instance.ServerSetVisualToggleState((EVisualToggleType)2, false);
                    }
                }
            }
        }
    }
}
