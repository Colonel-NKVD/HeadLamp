using SDG.Unturned;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace HeadLamp
{
    public class PlayerLampComponent : MonoBehaviour
    {
        private Player player;
        private float lastTick;
        private float drainAccumulator = 0f;

        void Awake() => player = GetComponent<Player>();

        void Update()
        {
            if (Time.time - lastTick < 0.5f) return;
            lastTick = Time.time;

            if (player.clothing.glassesAsset == null) return;
            var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == player.clothing.glassesAsset.id);
            if (config == null) return;

            bool isLightOn = player.clothing.glassesState != null && player.clothing.glassesState.Length > 0 && player.clothing.glassesState[0] != 0;

            if (isLightOn && player.clothing.glassesQuality == 0)
            {
                EffectManager.sendEffect(61, 16, player.look.aim.position);

                ushort id = player.clothing.glassesAsset.id;
                byte[] state = new byte[player.clothing.glassesState.Length];
                player.clothing.glassesState.CopyTo(state, 0);
                if (state.Length > 0) state[0] = 0;

                // ПЕРЕОДЕВАЕМ
                player.clothing.askWearGlasses(id, 0, state, true);

                // ЧИСТКА ИНВЕНТАРЯ
                for (byte page = 0; page < PlayerInventory.PAGES; page++)
                {
                    var items = player.inventory.items[page];
                    if (items == null) continue;
                    for (byte i = 0; i < items.getItemCount(); i++)
                    {
                        var jar = items.getItem(i);
                        if (jar != null && jar.item != null && jar.item.id == id && jar.item.quality == 0)
                        {
                            player.inventory.removeItem(page, i);
                            break;
                        }
                    }
                }

                // ЧИСТКА ЗЕМЛИ (через экземпляр manager)
                List<RegionCoordinate> regions = new List<RegionCoordinate>();
                Regions.getRegionsInRadius(player.transform.position, 1f, regions);
                foreach (var region in regions)
                {
                    var items = ItemManager.regions[region.x, region.y].items;
                    for (int i = items.Count - 1; i >= 0; i--)
                    {
                        var drop = items[i];
                        if (drop.item.id == id && drop.item.quality == 0)
                        {
                            // ВЫЗОВ ЧЕРЕЗ .manager (исправление ошибки CS0120)
                            ItemManager.manager.askTakeItem(player.channel.owner.playerID.steamID, region.x, region.y, drop.instanceID, 0, 0, 0, 0);
                        }
                    }
                }
                return;
            }

            if (isLightOn)
            {
                drainAccumulator += (config.DrainPerSecond * 0.5f);
                if (drainAccumulator >= 1f)
                {
                    byte drop = (byte)Mathf.FloorToInt(drainAccumulator);
                    drainAccumulator -= drop;
                    if (player.clothing.glassesQuality <= drop) player.clothing.glassesQuality = 0;
                    else player.clothing.glassesQuality -= drop;
                    player.clothing.sendUpdateGlassesQuality();
                }
            }
        }
    }
}
