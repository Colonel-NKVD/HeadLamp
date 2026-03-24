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
            // КОСТЫЛЬ 1: Игнорируем переменную "type". 
            // Какая бы это ни была кнопка (ПНВ, Тактика, Фары) - перехватываем всё.
            if (isVisible && __instance.glassesAsset != null && __instance.glassesQuality <= 0)
            {
                var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == __instance.glassesAsset.id);
                if (config != null)
                {
                    isVisible = false; // Бьем по рукам
                    return true; // Разрешаем серверу обработать наш "false" и разослать всем
                }
            }
            return true;
        }
    }
}
