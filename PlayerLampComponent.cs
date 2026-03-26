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

                // ВКЛЮЧАЕМ ЩИТ
                SwapState.IsSwapping = true;
                SwapState.ItemID = id;

                player.clothing.askWearGlasses(id, 0, state, true);

                // ВЫКЛЮЧАЕМ ЩИТ
                SwapState.IsSwapping = false;

                // ЧИСТИМ ИНВЕНТАРЬ
                for (byte page = 0; page < PlayerInventory.PAGES; page++)
                {
                    var items = player.inventory.items[page];
                    if (items != null)
                    {
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
