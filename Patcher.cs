using HarmonyLib;
using SDG.Unturned;
using System.Linq;

namespace HeadLamp
{
    // 1. БЛОКИРОВКА ВКЛЮЧЕНИЯ (Кнопка 'N')
    [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.ServerSetVisualToggleState))]
    public class Patch_ServerSetVisualToggleState
    {
        // Используем ref bool wantOn, чтобы иметь возможность изменить решение сервера "на лету"
        public static bool Prefix(PlayerClothing __instance, EVisualToggleType type, ref bool wantOn)
        {
            // Если игрок пытается ВКЛЮЧИТЬ (wantOn == true) ПНВ/Фонарь (VISION)
            if (wantOn && type == EVisualToggleType.VISION)
            {
                if (__instance.glassesAsset != null && __instance.glassesQuality <= 0)
                {
                    var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == __instance.glassesAsset.id);
                    if (config != null)
                    {
                        // МАГИЯ ЗДЕСЬ: Мы не отменяем метод (не делаем return false).
                        // Мы меняем желание клиента с "Включить" на "Выключить".
                        // Сервер обработает это и разошлет всем клиентам пакет: "Фонарь ВЫКЛЮЧЕН".
                        // Это жестко подавляет любой клиентский рассинхрон.
                        wantOn = false; 
                    }
                }
            }
            
            return true; // Продолжаем выполнение оригинального метода
        }
    }

    // 2. АВТО-ВЫКЛЮЧЕНИЕ ПРИ РАЗРЯДКЕ В НОЛЬ
    // Срабатывает каждый раз, когда меняется качество предмета
    [HarmonyPatch(typeof(PlayerClothing), nameof(PlayerClothing.sendUpdateGlassesQuality))]
    public class Patch_sendUpdateGlassesQuality
    {
        public static void Postfix(PlayerClothing __instance)
        {
            if (__instance.glassesAsset != null && __instance.glassesQuality <= 0)
            {
                // Проверяем, горит ли сейчас свет (байт 0 не равен 0)
                if (__instance.glassesState != null && __instance.glassesState.Length > 0 && __instance.glassesState[0] != 0)
                {
                    var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == __instance.glassesAsset.id);
                    if (config != null)
                    {
                        // Как только заряд упал в 0, а свет горит - вызываем официальный серверный метод выключения.
                        // Он сам поменяет стейт и сам всё разошлет.
                        __instance.ServerSetVisualToggleState(EVisualToggleType.VISION, false);
                    }
                }
            }
        }
    }
}
