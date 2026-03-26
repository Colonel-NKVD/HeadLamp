using SDG.Unturned;
using UnityEngine;
using System.Linq;

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
            lastTick = Player.player != null ? Time.time : 0; // Защита от null

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

                player.clothing.askWearGlasses(id, 0, state, true);

                // Чистка инвентаря
                for (byte page = 0; page < PlayerInventory.PAGES; page++)
                {
                    var items = player.inventory.items[page];
                    if (items != null)
                    {
                        for (byte i = 0; i < items.getItemCount(); i++)
                        {
                            if (items.getItem(i)?.item.id == id && items.getItem(i)?.item.quality == 0)
                            {
                                player.inventory.removeItem(page, i);
                                break;
                            }
                        }
                    }
                }

                // Чистка земли (Прямое удаление через removeItem)
                byte x, y;
                if (Regions.tryGetCoordinate(player.transform.position, out x, out y))
                {
                    var region = ItemManager.regions[x, y];
                    for (int i = region.items.Count - 1; i >= 0; i--)
                    {
                        if (region.items[i].item.id == id && region.items[i].item.quality == 0)
                        {
                            ItemManager.removeItem(x, y, (uint)i);
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
