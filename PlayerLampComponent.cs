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

            // Используем новый метод проверки состояния ПНВ/Фонаря
            if (player.Player.clothing.isVisualToggleActive(EVisualToggleType.Headlamp))
            {
                CheckAndDrain();
            }
        }

        private void CheckAndDrain()
        {
            bool isHat = false;
            LampSettings config = null;

            if (player.Player.clothing.hatAsset != null)
            {
                config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == player.Player.clothing.hatAsset.id);
                isHat = true;
            }

            if (config == null && player.Player.clothing.glassesAsset != null)
            {
                config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == player.Player.clothing.glassesAsset.id);
                isHat = false;
            }

            if (config == null) return;

            byte currentQuality = isHat ? player.Player.clothing.hatQuality : player.Player.clothing.glassesQuality;

            if (currentQuality > 0)
            {
                drainAccumulator += config.DrainPerSecond;

                if (drainAccumulator >= 1f)
                {
                    int dropAmount = Mathf.FloorToInt(drainAccumulator);
                    drainAccumulator -= dropAmount;

                    byte newValue = (byte)Mathf.Max(0, currentQuality - dropAmount);
                    
                    if (isHat) player.Player.clothing.hatQuality = newValue;
                    else player.Player.clothing.glassesQuality = newValue;
                }
            }

            currentQuality = isHat ? player.Player.clothing.hatQuality : player.Player.clothing.glassesQuality;

            if (currentQuality == 0)
            {
                // Указываем тип (Headlamp) и состояние (false)
                player.Player.clothing.ServerSetVisualToggleState(EVisualToggleType.Headlamp, false);
                EffectManager.sendEffect(8, 24, player.Position);
                drainAccumulator = 0f;
            }
        }
    }
}
