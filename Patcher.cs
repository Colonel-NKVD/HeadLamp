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

                    // 1. ЧИСТИМ ИНВЕНТАРЬ
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

                    // 2. ЧИСТКА ЗЕМЛИ (Через статический ItemManager)
                    // Мы просто говорим серверу: "Удали все севшие фонари в радиусе 2 метров"
                    byte x, y;
                    if (Regions.tryGetCoordinate(__instance.player.transform.position, out x, out y))
                    {
                        var region = ItemManager.regions[x, y];
                        for (int i = region.items.Count - 1; i >= 0; i--)
                        {
                            var itemData = region.items[i];
                            if (itemData.item.id == id && itemData.item.quality == 0)
                            {
                                // Прямое удаление из списка региона - самый надежный способ без instance
                                ItemManager.removeItem(x, y, (uint)i);
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
