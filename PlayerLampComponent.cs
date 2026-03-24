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

            if (isLightOn)
            {
                if (player.clothing.glassesQuality == 0)
                {
                    // Эффект искр в момент окончательной разрядки
                    EffectManager.sendEffect(61, 16, player.transform.position);

                    player.clothing.glassesState[0] = 0;
                    player.clothing.askWearGlasses(player.clothing.glassesAsset.id, 0, player.clothing.glassesState, true);
                    return;
                }

                drainAccumulator += (config.DrainPerSecond * 0.5f);
                if (drainAccumulator >= 1f)
                {
                    byte drop = (byte)Mathf.FloorToInt(drainAccumulator);
                    drainAccumulator -= drop;

                    if (player.clothing.glassesQuality <= drop)
                        player.clothing.glassesQuality = 0;
                    else
                        player.clothing.glassesQuality -= drop;

                    player.clothing.sendUpdateGlassesQuality();
                }
            }
        }
    }
}
