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

                // Запоминаем конкретный экземпляр предмета
                uint originalInstanceID = player.clothing.glassesInstanceID;
                ushort id = player.clothing.glassesAsset.id;
                byte[] state = player.clothing.glassesState;
                if (state != null && state.Length > 0) state[0] = 0;

                // Переодеваем
                player.clothing.askWearGlasses(id, 0, state, true);

                // Чистим инвентарь по InstanceID
                for (byte page = 0; page < PlayerInventory.PAGES; page++)
                {
                    var items = player.inventory.items[page];
                    if (items == null) continue;
                    for (byte i = 0; i < items.getItemCount(); i++)
                    {
                        if (items.getItem(i)?.item.instanceID == originalInstanceID)
                        {
                            player.inventory.removeItem(page, i);
                            break;
                        }
                    }
                }

                // Чистим землю по InstanceID
                List<RegionCoordinate> regions = new List<RegionCoordinate>();
                Regions.getRegionsInRadius(player.transform.position, 1f, regions);
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
                return;
            }

            // Логика разряда...
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
