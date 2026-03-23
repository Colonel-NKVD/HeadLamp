using HarmonyLib;
using SDG.Unturned;
using System.Linq;

namespace HeadLamp
{
    // Указываем тип метода и его параметры, чтобы Harmony точно его нашел
    [HarmonyPatch(typeof(PlayerClothing), "ReceiveToggleVisual")]
    public class Patch_ToggleVisual
    {
        [HarmonyPrefix]
        public static bool Prefix(PlayerClothing __instance, EVisualToggleType type, bool wantsOn)
        {
            // Нас интересует только тип 1 (NonCosmetic/Headlamp/NVG)
            // И только когда игрок хочет ВКЛЮЧИТЬ (wantsOn == true)
            if ((int)type == 1 && wantsOn)
            {
                var asset = __instance.glassesAsset;
                if (asset != null)
                {
                    // Проверяем, есть ли этот предмет в нашем конфиге
                    var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == asset.id);
                    
                    // Если предмет в конфиге и его прочность 0 — блокируем выполнение метода (return false)
                    if (config != null && __instance.glassesQuality <= 0)
                    {
                        return false; 
                    }
                }
            }
            // В остальных случаях (выключение или если прочность > 0) разрешаем работу
            return true;
        }
    }
}
