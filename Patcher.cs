using HarmonyLib;
using SDG.Unturned;
using System.Linq;

namespace HeadLamp
{
    // 1. ПАТЧ НА ПЕРЕКЛЮЧЕНИЕ (Кнопка 'N')
    [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.ServerSetVisualToggleState))]
    public class Patch_ServerSetVisualToggleState
    {
        public static bool Prefix(PlayerClothing __instance, EVisualToggleType type, ref bool isVisible)
        {
            // Если пытаются включить что-либо на голове при 0% прочности
            if (isVisible && __instance.glassesAsset != null && __instance.glassesQuality <= 0)
            {
                var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == __instance.glassesAsset.id);
                if (config != null)
                {
                    // 1. Запрещаем визуальный стейт
                    isVisible = false;

                    // 2. ЖЕСТКО зануляем байт состояния (обычно это индекс 0)
                    if (__instance.glassesState != null && __instance.glassesState.Length > 0)
                    {
                        __instance.glassesState[0] = 0;
                    }

                    // 3. Синхронизируем стейт через "тяжелый" пакет
                    __instance.sendUpdateGlassesState();
                    return false; // Отменяем выполнение оригинального метода, мы всё сделали сами
                }
            }
            return true;
        }
    }

    // 2. ПАТЧ НА ПОТЕРЮ ПРОЧНОСТИ (Когда заряд сел в 0)
    [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.sendUpdateGlassesQuality))]
    public class Patch_sendUpdateGlassesQuality
    {
        public static void Postfix(PlayerClothing __instance)
        {
            if (__instance.glassesAsset != null && __instance.glassesQuality <= 0)
            {
                // Проверяем, горит ли свет
                if (__instance.glassesState != null && __instance.glassesState.Length > 0 && __instance.glassesState[0] != 0)
                {
                    var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == __instance.glassesAsset.id);
                    if (config != null)
                    {
                        // ТУТ ИДЕТ СИЛОВАЯ ПЕРЕЗАПИСЬ
                        __instance.glassesState[0] = 0;

                        // Вызываем все возможные методы синхронизации:
                        // а) Выключаем визуал (пробуем все индексы 0, 1, 2 на всякий случай)
                        __instance.ServerSetVisualToggleState((EVisualToggleType)0, false);
                        __instance.ServerSetVisualToggleState((EVisualToggleType)1, false);
                        __instance.ServerSetVisualToggleState((EVisualToggleType)2, false);

                        // б) Отправляем обновленный стейт байтов
                        __instance.sendUpdateGlassesState();

                        // в) ФИНАЛЬНЫЙ УДАР: Полная переотправка предмета игроку через внутренний RPC
                        // Это заставит Unity-модельку фонаря пересоздаться в выключенном состоянии
                        __instance.player.clothing.askUpdateGlasses(
                            __instance.glassesAsset.id, 
                            0, 
                            __instance.glassesState
                        );
                    }
                }
            }
        }
    }
}
