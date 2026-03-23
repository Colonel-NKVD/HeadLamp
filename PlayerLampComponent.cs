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
            // Увеличим частоту проверки до 0.5с для лучшего отклика
            if (Time.time - lastTick < 0.5f) return;
            lastTick = Time.time;

            CheckAndDrain();
        }

        private void CheckAndDrain()
        {
            var clothing = player.Player.clothing;

            if (clothing.glassesAsset != null)
            {
                var asset = clothing.glassesAsset;
                
                if (asset.vision != ELightingVision.NONE)
                {
                    // Проверяем включен ли прибор
                    bool isVisualOn = clothing.glassesState != null && clothing.glassesState.Length > 0 && clothing.glassesState[0] != 0;
                    
                    if (isVisualOn)
                    {
                        if (clothing.glassesQuality == 0)
                        {
                            ForceOff(clothing);
                            return;
                        }

                        DrainItem(clothing, asset.id);
                    }
                }
            }
        }

        private void DrainItem(PlayerClothing clothing, ushort itemId)
        {
            var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == itemId);
            float drainRate = config != null ? config.DrainPerSecond : 0.1f;

            if (clothing.glassesQuality > 0)
            {
                // Умножаем на 0.5, так как тик теперь каждые 0.5с
                drainAccumulator += (drainRate * 0.5f); 
                if (drainAccumulator >= 1f)
                {
                    int drop = Mathf.FloorToInt(drainAccumulator);
                    drainAccumulator -= drop;
                    
                    clothing.glassesQuality = (byte)Mathf.Max(0, clothing.glassesQuality - drop);
                    clothing.sendUpdateGlassesQuality();

                    if (clothing.glassesQuality == 0)
                    {
                        ForceOff(clothing);
                        EffectManager.sendEffect(8, 24, player.Position);
                    }
                }
            }
        }

        private void ForceOff(PlayerClothing clothing)
        {
            if (clothing.glassesState != null && clothing.glassesState.Length > 0)
            {
                // 1. Обнуляем байт включения
                clothing.glassesState[0] = 0;

                // 2. Синхронизируем состояние очков (ID, Качество, Состояние)
                // В твоей версии это правильный метод для обновления всего ассета очков
                clothing.sendUpdateGlasses();

                // 3. Вызываем отключение света (из твоего декомпилятора)
                player.Player.updateGlassesLights(false);

                drainAccumulator = 0f;
            }
        }
    }
}
