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
                    EffectManager.sendEffect(61, 16, __instance.player.look.aim.position);

                    ushort id = __instance.glassesAsset.id;
                    byte[] newState = new byte[__instance.glassesState.Length];
                    __instance.glassesState.CopyTo(newState, 0);
                    if (newState.Length > 0) newState[0] = 0;

                    // ПЕРЕОДЕВАЕМ
                    __instance.askWearGlasses(id, 0, newState, true);

                    // ЧИСТКА ИНВЕНТАРЯ
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

                    // ЧИСТКА ЗЕМЛИ (через .instance)
                    List<RegionCoordinate> regions = new List<RegionCoordinate>();
                    Regions.getRegionsInRadius(__instance.player.transform.position, 1f, regions);
                    foreach (var region in regions)
                    {
                        var items = ItemManager.regions[region.x, region.y].items;
                        for (int i = items.Count - 1; i >= 0; i--)
                        {
                            var drop = items[i];
                            if (drop.item.id == id && drop.item.quality == 0)
                            {
                                // ИСПРАВЛЕНИЕ: Используем .instance вместо .manager
                                ItemManager.instance.askTakeItem(__instance.player.channel.owner.playerID.steamID, region.x, region.y, drop.instanceID, 0, 0, 0, 0);
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
