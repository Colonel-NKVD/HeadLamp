using HarmonyLib;
using SDG.Unturned;
using System.Linq;
using UnityEngine;

namespace HeadLamp
{
    // Статический класс-сигнализация для общения между нашими скриптами
    public static class SwapState
    {
        public static bool IsSwapping = false;
        public static ushort ItemID = 0;
    }

    // НОВЫЙ ПАТЧ: Перехватываем попытку игры выкинуть предмет
    [HarmonyPatch(typeof(ItemManager), nameof(ItemManager.dropItem))]
    public class Patch_ItemManager_dropItem
    {
        public static bool Prefix(Item item)
        {
            // Если мы прямо сейчас переодеваем фонарик, и игра пытается выкинуть его копию на землю...
            if (SwapState.IsSwapping && item != null && item.id == SwapState.ItemID && item.quality == 0)
            {
                // ...мы отменяем выпадение! Предмет просто исчезает в небытие.
                return false; 
            }
            return true; // В остальное время разрешаем выбрасывать предметы
        }
    }

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
                    EffectManager.sendEffect(61, 16, __instance.player.look.aim.position);

                    ushort id = __instance.glassesAsset.id;
                    byte[] newState = new byte[__instance.glassesState.Length];
                    __instance.glassesState.CopyTo(newState, 0);
                    if (newState.Length > 0) newState[0] = 0;

                    // 1. ВКЛЮЧАЕМ ЩИТ (защита от выпадения дюпа на землю)
                    SwapState.IsSwapping = true;
                    SwapState.ItemID = id;

                    // 2. ПЕРЕОДЕВАЕМ
                    __instance.askWearGlasses(id, 0, newState, true);

                    // 3. ВЫКЛЮЧАЕМ ЩИТ
                    SwapState.IsSwapping = false;

                    // 4. ЧИСТИМ ИНВЕНТАРЬ (если место было, дюп упал в рюкзак)
                    for (byte page = 0; page < PlayerInventory.PAGES; page++)
                    {
                        var items = __instance.player.inventory.items[page];
                        if (items == null) continue;
                        for (byte i = 0; i < items.getItemCount(); i++)
                        {
                            var jar = items.getItem(i);
                            if (jar != null && jar.item != null && jar.item.id == id && jar.item.quality == 0)
                            {
                                __instance.player.inventory.removeItem(page, i);
                                break; 
                            }
                        }
                    }

                    // Чистить землю больше не нужно - патч не даст ему туда упасть!
                    return false;
                }
            }
            return true;
        }
    }
}
