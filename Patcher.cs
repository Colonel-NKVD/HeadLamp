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
            if (isVisible && __instance.glassesAsset != null && __instance.glassesQuality <= 0)
            {
                var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == __instance.glassesAsset.id);
                if (config != null)
                {
                    // 1. Проигрываем эффект искр (ID 61 — ванильные электрические искры со звуком)
                    // Параметры: ID эффекта, радиус видимости, позиция
                    EffectManager.sendEffect(61, 16, __instance.player.transform.position);

                    // 2. Принудительно выключаем стейт
                    if (__instance.glassesState != null && __instance.glassesState.Length > 0)
                        __instance.glassesState[0] = 0;

                    // 3. Мгновенная перезагрузка предмета
                    __instance.askWearGlasses(__instance.glassesAsset.id, 0, __instance.glassesState, true);

                    return false; 
                }
            }
            return true;
        }
    }
}
