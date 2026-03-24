using HarmonyLib;
using SDG.Unturned;
using System.Linq;

namespace HeadLamp
{
    // --- УРОВЕНЬ 1: Блокировка нажатия кнопки 'N' ---
    [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.ServerSetVisualToggleState))]
    public class Patch_ServerSetVisualToggleState
    {
        public static bool Prefix(PlayerClothing __instance, EVisualToggleType type, ref bool isVisible)
        {
            // В ваниле VISION (ПНВ/Фонари) — это индекс 1
            if (isVisible && (int)type == 1) 
            {
                if (__instance.glassesAsset != null && __instance.glassesQuality <= 0)
                {
                    var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == __instance.glassesAsset.id);
                    if (config != null)
                    {
                        // Принудительно гасим попытку включения
                        isVisible = false;
                        
                        if (__instance.glassesState != null && __instance.glassesState.Length > 0)
                            __instance.glassesState[0] = 0;

                        // Вместо несуществующего sendUpdateGlassesState используем askUpdateGlasses
                        __instance.askUpdateGlasses(__instance.glassesAsset.id, __instance.glassesQuality, __instance.glassesState);
                        
                        return false; // Отменяем оригинальный метод, так как мы всё обновили сами
                    }
                }
            }
            return true;
        }
    }

    // --- УРОВЕНЬ 2: Блокировка физического света в Unity (Ультиматум) ---
    [HarmonyPatch(typeof(Player), nameof(Player.updateGlassesLights))]
    public class Patch_updateGlassesLights
    {
        // Этот патч — самый важный. Он перерезает "провода" в самом движке.
        public static void Prefix(Player __instance, ref bool isVisible)
        {
            if (isVisible && __instance.clothing.glassesAsset != null && __instance.clothing.glassesQuality <= 0)
            {
                var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == __instance.clothing.glassesAsset.id);
                if (config != null)
                {
                    // Даже если клиент уверен, что свет должен гореть — мы заставляем его выключиться
                    isVisible = false;
                }
            }
        }
    }

    // --- УРОВЕНЬ 3: Авто-выключение при разрядке в 0% ---
    [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.sendUpdateGlassesQuality))]
    public class Patch_sendUpdateGlassesQuality
    {
        public static void Postfix(PlayerClothing __instance)
        {
            if (__instance.glassesAsset != null && __instance.glassesQuality <= 0)
            {
                // Если заряд 0, а в байтах записано "включено"
                if (__instance.glassesState != null && __instance.glassesState.Length > 0 && __instance.glassesState[0] != 0)
                {
                    var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == __instance.glassesAsset.id);
                    if (config != null)
                    {
                        // Жестко зануляем байт включения
                        __instance.glassesState[0] = 0;

                        // Выключаем через стандартный метод (тип 1 — VISION)
                        __instance.ServerSetVisualToggleState((EVisualToggleType)1, false);

                        // И сразу принудительно синхронизируем всё состояние очков
                        __instance.askUpdateGlasses(__instance.glassesAsset.id, __instance.glassesQuality, __instance.glassesState);
                    }
                }
            }
        }
    }
}
