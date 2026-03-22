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
        private float drainAccumulator = 0f; // Накопитель дробного расхода

        void Awake()
        {
            player = UnturnedPlayer.FromPlayer(GetComponent<Player>());
        }

        void FixedUpdate()
        {
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

            byte currentQuality = player.Player.clothing.headQuality;
            if (currentQuality > 0)
            {
                // Накапливаем расход
                drainAccumulator += config.DrainPerSecond;

                // Если накопилась хотя бы 1 единица для списания
                if (drainAccumulator >= 1f)
                {
                    int dropAmount = Mathf.FloorToInt(drainAccumulator);
                    drainAccumulator -= dropAmount; // Оставляем остаток

                    byte newValue = (byte)Mathf.Max(0, currentQuality - dropAmount);
                    player.Player.clothing.headQuality = newValue;
                    
                    // Обновляем сеть ТОЛЬКО при фактическом изменении значения
                    player.Player.clothing.sendUpdateCareful();
                }
            }

            // Проверяем выключение
            if (player.Player.clothing.headQuality == 0)
            {
                player.Player.clothing.isNightVisionActive = false;
                player.Player.clothing.sendUpdateNightVision(); // Синхронизация выключения
                EffectManager.sendEffect(8, 24, player.Position);
                drainAccumulator = 0f; // Сбрасываем накопитель
            }
        }
    }
}
