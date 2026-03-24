using HarmonyLib;
using SDG.Unturned;
using System.Linq;
using UnityEngine;

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
                    // 1. Искры у головы (высота 1.8м)
                    EffectManager.sendEffect(61, 16, __instance.player.transform.position + Vector3.up * 1.8f);

                    // 2. Сохраняем данные фонаря во временные переменные
                    ushort id = __instance.glassesAsset.id;
                    byte quality = 0;
                    byte[] state = __instance.glassesState;
                    if (state != null && state.Length > 0) state[0] = 0; // Выключаем в стейте

                    // 3. ФОКУС ПРОТИВ ДЮПА:
                    // Обнуляем ссылку на очки в памяти сервера. 
                    // Теперь метод askWearGlasses будет думать, что голова пустая.
                    var glassesField = typeof(PlayerClothing).GetField("_glasses", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (glassesField != null) glassesField.SetValue(__instance, (ushort)0);
                    
                    // 4. Переодеваем. Теперь игре нечего "выплевывать" в инвентарь!
                    __instance.askWearGlasses(id, quality, state, true);

                    return false; // Полный блок оригинала
                }
            }
            return true;
        }
    }
}
