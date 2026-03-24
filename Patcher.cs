using HarmonyLib;
using SDG.Unturned;
using System.Linq;

namespace HeadLamp
{
    // 1. БЛОКИРОВКА ВКЛЮЧЕНИЯ (Кнопка 'N')
    [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.ServerSetVisualToggleState))]
    public class Patch_ServerSetVisualToggleState
    {
        // ВАЖНО: имя параметра изменено с wantOn на isVisible, чтобы соответствовать игре
        public static bool Prefix(PlayerClothing __instance, EVisualToggleType type, ref bool isVisible)
        {
            // Если игрок пытается ВКЛЮЧИТЬ (isVisible == true) ПНВ/Фонарь
            // Используем (int)type == 2 (VISION) для универсальности
            if (isVisible && (int)type == 2)
            {
                if (__instance.glassesAsset != null && __instance.glassesQuality <= 0)
                {
                    var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == __instance.glassesAsset.id);
                    if (config != null)
                    {
                        // Меняем намерение игрока на "выключить"
                        isVisible = false; 
                    }
                }
            }
            
            return true; // Продолжаем выполнение метода с измененным параметром
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
                // Если байт стейта не 0 (свет горит)
                if (__instance.glassesState != null && __instance.glassesState.Length > 0 && __instance.glassesState[0] != 0)
                {
                    var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == __instance.glassesAsset.id);
                    if (config != null)
                    {
                        // Вызываем выключение. 
                        // Теперь, когда патч выше работает, этот вызов точно пройдет корректно.
                        __instance.ServerSetVisualToggleState((EVisualToggleType)2, false);
                    }
                }
            }
        }
    }
}
