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

        void Awake()
        {
            player = UnturnedPlayer.FromPlayer(GetComponent<Player>());
        }

        void FixedUpdate()
        {
            // Проверяем раз в секунду для оптимизации
            if (Time.time - lastTick < 1f) return;
            lastTick = Time.time;

            if (player.Player.clothing.isNightVisionActive)
            {
                CheckAndDrain();
            }
        }

        private void CheckAndDrain()
        {
            var headItem = player.Player.clothing.headAsset;
            if (headItem == null) return;

            var config = HeadLamp.Instance.Configuration.Instance.Lamps
                .FirstOrDefault(x => x.ItemID == headItem.id);

            if (config == null) return;

            // Уменьшаем прочность
            if (player.Player.clothing.headQuality > 0)
            {
                byte currentQuality = player.Player.clothing.headQuality;
                int newValue = Mathf.Max(0, currentQuality - (int)config.DrainPerSecond);
                
                player.Player.clothing.headQuality = (byte)newValue;
                
                // Обновляем состояние у клиента
                player.Player.clothing.sendUpdateCareful();
            }

            // Если разрядилось — выключаем
            if (player.Player.clothing.headQuality <= 0)
            {
                player.Player.clothing.isNightVisionActive = false;
                player.Player.clothing.sendUpdateNightVision();
                EffectManager.sendEffect(8, 24, player.Position); // Звук поломки/выключения
            }
        }
    }
}
