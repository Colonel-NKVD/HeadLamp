using HarmonyLib;
using SDG.Unturned;
using System.Linq;
using System.Reflection;

namespace HeadLamp
{
    [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.ServerSetVisualToggleState))]
    public class Patch_ServerSetVisualToggleState
    {
        public static bool Prefix(PlayerClothing __instance, EVisualToggleType type, ref bool isVisible)
        {
            // Если пытаются включить (isVisible = true) севший фонарь
            if (isVisible && __instance.glassesAsset != null && __instance.glassesQuality <= 0)
            {
                var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == __instance.glassesAsset.id);
                if (config != null)
                {
                    // 1. Принудительно ставим выключенное состояние в байтах
                    if (__instance.glassesState != null && __instance.glassesState.Length > 0)
                    {
                        __instance.glassesState[0] = 0;
                    }

                    // 2. Вызываем принудительную синхронизацию через "Ядерный метод"
                    NuclearSync(__instance);

                    return false; // ПОЛНАЯ ОСТАНОВКА. Оригинальный метод игры даже не узнает, что кнопку нажали.
                }
            }
            return true;
        }

        // Вспомогательный метод для синхронизации, чтобы не было ошибок компиляции
        public static void NuclearSync(PlayerClothing clothing)
        {
            // Пытаемся вызвать внутренний метод обновления через рефлексию
            // Это заставляет сервер отправить пакет "Обновить одежду" всем игрокам
            MethodInfo sendUpdate = typeof(PlayerClothing).GetMethod("sendUpdateGlassesQuality", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (sendUpdate != null) sendUpdate.Invoke(clothing, null);

            // Дополнительно спамим визуальным выключением (на всякий случай)
            clothing.player.updateGlassesLights(false);
        }
    }
}
