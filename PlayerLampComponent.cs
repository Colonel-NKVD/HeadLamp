using SDG.Unturned;
using UnityEngine;
using Rocket.Unturned.Player;
using System.Linq;

namespace HeadLamp
{
    public class PlayerLampComponent : MonoBehaviour
    {
        private UnturnedPlayer player;
        private float lastTick;
        private float drainAccumulator = 0f;

        void Awake()
        {
            player = UnturnedPlayer.FromPlayer(GetComponent<Player>());
        }

        void FixedUpdate()
        {
            if (Time.time - lastTick < 1f) return;
            lastTick = Time.time;

            CheckAndDrain();
        }

        private void CheckAndDrain()
        {
            var clothing = player.Player.clothing;

            // Работаем ТОЛЬКО с очками (Glasses/NVG)
            if (clothing.glassesAsset != null)
            {
                var asset = clothing.glassesAsset;
                
                // Проверяем, есть ли у очков вообще функция свечения
                if (asset.vision != ELightingVision.NONE)
                {
                    // Проверяем включены ли они сейчас (байт [0] в стейте)
                    bool isVisualOn = clothing.glassesState != null && clothing.glassesState.Length > 0 && clothing.glassesState[0] != 0;
                    
                    if (isVisualOn)
                    {
                        DrainItem(ref clothing.glassesQuality, asset.id);
                        
                        if (clothing.glassesQuality == 0)
                        {
                            // Принудительно выключаем через индекс 1 (Headlamp/NVG)
                            clothing.ServerSetVisualToggleState((EVisualToggleType)1, false);
                        }
                    }
                }
            }
        }

        private void DrainItem(ref byte quality, ushort itemId)
        {
            var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == itemId);
            float drainRate = config != null ? config.DrainPerSecond : 0.1f;

            if (quality > 0)
            {
                drainAccumulator += drainRate;
                if (drainAccumulator >= 1f)
                {
                    int drop = Mathf.FloorToInt(drainAccumulator);
                    drainAccumulator -= drop;
                    quality = (byte)Mathf.Max(0, quality - drop);

                    player.Player.clothing.sendUpdateGlassesQuality();
                }
            }

            if (quality == 0)
            {
                EffectManager.sendEffect(8, 24, player.Position);
                drainAccumulator = 0f;
            }
        }
    }
}
