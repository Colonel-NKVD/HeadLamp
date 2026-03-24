using HarmonyLib;
using SDG.Unturned;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

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
                    // 1. Искры у головы
                    EffectManager.sendEffect(61, 16, __instance.player.look.aim.position);

                    // 2. ЗАПОМИНАЕМ уникальный ID предмета, который сейчас на голове
                    uint originalInstanceID = __instance.glassesInstanceID;
                    ushort itemID = __instance.glassesAsset.id;
                    byte[] state = __instance.glassesState;
                    if (state != null && state.Length > 0) state[0] = 0;

                    // 3. ПЕРЕОДЕВАЕМ (создает дюп с тем же InstanceID)
                    __instance.askWearGlasses(itemID, 0, state, true);

                    // 4. УДАЛЯЕМ ТОЛЬКО ДУБЛИКАТ (с тем же InstanceID)
                    // Чистим инвентарь
                    for (byte page = 0; page < PlayerInventory.PAGES; page++)
                    {
                        var items = __instance.player.inventory.items[page];
                        if (items == null) continue;
                        for (byte i = 0; i < items.getItemCount(); i++)
                        {
                            var jar = items.getItem(i);
                            // Если нашли предмет с тем же уникальным ID в инвентаре - это наш дюп
                            if (jar != null && jar.item.instanceID == originalInstanceID)
                            {
                                __instance.player.inventory.removeItem(page, i);
                                break; 
                            }
                        }
                    }

                    // Чистим землю (если выпал)
                    List<RegionCoordinate> regions = new List<RegionCoordinate>();
                    Regions.getRegionsInRadius(__instance.player.transform.position, 1f, regions);
                    foreach (var region in regions)
                    {
                        var items = ItemManager.regions[region.x, region.y].items;
                        for (int i = items.Count - 1; i >= 0; i--)
                        {
                            if (items[i].instanceID == originalInstanceID)
                            {
                                ItemManager.askTakeItem(region.x, region.y, items[i].instanceID);
                            }
                        }
                    }

                    return false;
                }
            }
            return true;
        }
    }
}
