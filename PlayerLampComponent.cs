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
            if (Time.time - lastTick < 0.5f) return;
            lastTick = Time.time;

            CheckAndDrain();
        }

        private void CheckAndDrain()
        {
            var clothing = player.Player.clothing;
            if (clothing.glassesAsset == null) return;

            // Проверяем включен ли свет
            bool isVisualOn = clothing.glassesState != null && clothing.glassesState.Length > 0 && clothing.glassesState[0] != 0;

            if (isVisualOn)
            {
                // Если кто-то включил УЖЕ сломанный фонарь (например, багом) - тушим
                if (clothing.glassesQuality == 0)
                {
                    ForceOffSequence(clothing, true);
                    return;
                }

                var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == clothing.glassesAsset.id);
                float drainRate = config != null ? config.DrainPerSecond : 0.1f;

                drainAccumulator += (drainRate * 0.5f);
                if (drainAccumulator >= 1f)
                {
                    int drop = Mathf.FloorToInt(drainAccumulator);
                    drainAccumulator -= drop;

                    // Если этот тик убьет батарейку до 0
                    if (clothing.glassesQuality <= drop)
                    {
                        // ВАЖНО: Выполняем секвенцию выключения ДО того, как прочность станет 0
                        ForceOffSequence(clothing, false);
                        
                        // И только ТЕПЕРЬ убиваем прочность
                        clothing.glassesQuality = 0;
                        clothing.sendUpdateGlassesQuality();
                    }
                    else
                    {
                        clothing.glassesQuality -= (byte)drop;
                        clothing.sendUpdateGlassesQuality();
                    }
                }
            }
        }

        private void ForceOffSequence(PlayerClothing clothing, bool isAlreadyBroken)
        {
            // 1. Меняем состояние на сервере
            if (clothing.glassesState != null && clothing.glassesState.Length > 0)
            {
                clothing.glassesState[0] = 0;
            }

            // 2. Если предмет уже был сломан, нам придется временно "починить" его для клиента,
            // чтобы он принял пакет выключения.
            if (isAlreadyBroken)
            {
                clothing.glassesQuality = 1; 
                clothing.sendUpdateGlassesQuality();
            }

            // 3. Отправляем легальный ванильный пакет выключения
            // Клиент примет его, так как прочность предмета > 0
            clothing.ServerSetVisualToggleState((EVisualToggleType)1, false);
            
            // 4. Гасим локальные источники
            player.Player.updateGlassesLights(false);

            // 5. Если мы временно чинили предмет - возвращаем 0
            if (isAlreadyBroken)
            {
                clothing.glassesQuality = 0;
                clothing.sendUpdateGlassesQuality();
            }

            // Звук выключения
#pragma warning disable CS0618
            EffectManager.sendEffect(8, 24, player.Position);
#pragma warning restore CS0618

            drainAccumulator = 0f;
        }
    }
}
