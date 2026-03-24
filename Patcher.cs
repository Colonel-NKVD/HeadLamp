using HarmonyLib;
using SDG.Unturned;
using System.Linq;

namespace HeadLamp
{
    // --- УРОВЕНЬ 1: Перехват нажатия 'N' и подавление предикции ---
    [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.ServerSetVisualToggleState))]
    public class Patch_ServerSetVisualToggleState
    {
        public static bool Prefix(PlayerClothing __instance, EVisualToggleType type, ref bool isVisible)
        {
            // Проверяем индексы 1 и 2 (на случай разных версий модов/ванилы для VISION)
            if (isVisible && ((int)type == 1 || (int)type == 2)) 
            {
                if (__instance.glassesAsset != null && __instance.glassesQuality <= 0)
                {
                    var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == __instance.glassesAsset.id);
                    if (config != null)
                    {
                        // МАГИЯ: Меняем желание клиента на "Выключить"
                        isVisible = false;
                        
                        // ВАЖНО: Мы возвращаем TRUE! 
                        // Оригинальный метод продолжит работу, увидит isVisible = false,
                        // сам обновит байты и САМ разошлет всем клиентам пакет выключения.
                        // Это собьет локально включенный свет у игрока.
                        return true; 
                    }
                }
            }
            return true;
        }
    }

    // --- УРОВЕНЬ 2: Блокировка рендера Unity (Ультиматум) ---
    // (Этот метод у тебя компилировался без ошибок, оставляем его как страховку)
    [HarmonyPatch(typeof(Player), nameof(Player.updateGlassesLights))]
    public class Patch_updateGlassesLights
    {
        public static void Prefix(Player __instance, ref bool isVisible)
        {
            if (isVisible && __instance.clothing != null && __instance.clothing.glassesAsset != null && __instance.clothing.glassesQuality <= 0)
            {
                var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == __instance.clothing.glassesAsset.id);
                if (config != null)
                {
                    // "Перерезаем провода": физически запрещаем движку зажигать свет
                    isVisible = false;
                }
            }
        }
    }

    // --- УРОВЕНЬ 3: Авто-выключение при достижении 0% ---
    [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.sendUpdateGlassesQuality))]
    public class Patch_sendUpdateGlassesQuality
    {
        public static void Postfix(PlayerClothing __instance)
        {
            if (__instance.glassesAsset != null && __instance.glassesQuality <= 0)
            {
                // Проверяем байт (горит ли свет)
                if (__instance.glassesState != null && __instance.glassesState.Length > 0 && __instance.glassesState[0] != 0)
                {
                    var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == __instance.glassesAsset.id);
                    if (config != null)
                    {
                        // Просто вызываем оригинальный публичный метод сервера.
                        // Он сам поменяет стейты и всё синхронизирует. Без "левых" методов.
                        __instance.ServerSetVisualToggleState((EVisualToggleType)1, false);
                        __instance.ServerSetVisualToggleState((EVisualToggleType)2, false); // Бьем по обоим индексам для надежности
                    }
                }
            }
        }
    }
}
