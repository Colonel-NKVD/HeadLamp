using HarmonyLib;
using SDG.Unturned;
using System.Linq;

namespace HeadLamp
{
    // --- УРОВЕНЬ 1: Блокировка сетевого пакета ---
    [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.ServerSetVisualToggleState))]
    public class Patch_ServerSetVisualToggleState
    {
        public static bool Prefix(PlayerClothing __instance, EVisualToggleType type, ref bool isVisible)
        {
            // В ваниле ПНВ и Фонари — это индекс 1 (VISION)
            if (isVisible && (int)type == 1) 
            {
                if (__instance.glassesAsset != null && __instance.glassesQuality <= 0)
                {
                    var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == __instance.glassesAsset.id);
                    if (config != null)
                    {
                        // Принудительно ставим ложь
                        isVisible = false;
                        
                        // Обнуляем байт состояния в массиве
                        if (__instance.glassesState != null && __instance.glassesState.Length > 0)
                            __instance.glassesState[0] = 0;

                        // Синхронизируем стейт байтов
                        __instance.sendUpdateGlassesState();
                    }
                }
            }
            return true;
        }
    }

    // --- УРОВЕНЬ 2: Блокировка самого Unity-света (Ядро) ---
    [HarmonyPatch(typeof(Player), nameof(Player.updateGlassesLights))]
    public class Patch_updateGlassesLights
    {
        // Этот метод вызывается игрой, чтобы включить/выключить модельку света
        public static void Prefix(Player __instance, ref bool isVisible)
        {
            if (isVisible && __instance.clothing.glassesAsset != null && __instance.clothing.glassesQuality <= 0)
            {
                var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == __instance.clothing.glassesAsset.id);
                if (config != null)
                {
                    // "Перерезаем провода": даже если игра хочет зажечь свет, мы передаем false
                    isVisible = false;
                }
            }
        }
    }

    // --- УРОВЕНЬ 3: Авто-выключение при разрядке ---
    [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.sendUpdateGlassesQuality))]
    public class Patch_sendUpdateGlassesQuality
    {
        public static void Postfix(PlayerClothing __instance)
        {
            if (__instance.glassesAsset != null && __instance.glassesQuality <= 0)
            {
                // Если заряд 0, а свет всё ещё числится включенным в байтах
                if (__instance.glassesState != null && __instance.glassesState.Length > 0 && __instance.glassesState[0] != 0)
                {
                    var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == __instance.glassesAsset.id);
                    if (config != null)
                    {
                        // Гасим всё
                        __instance.glassesState[0] = 0;
                        __instance.ServerSetVisualToggleState((EVisualToggleType)1, false);
                        __instance.player.updateGlassesLights(false);
                        __instance.sendUpdateGlassesState();
                    }
                }
            }
        }
    }
}
