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
            // Проверка каждые 0.5 сек для экономии ресурсов
            if (Time.time - lastTick < 0.5f) return;
            lastTick = Time.time;

            CheckAndDrain();
        }

        private void CheckAndDrain()
        {
            var clothing = player.Player.clothing;

            // Работаем ТОЛЬКО с очками
            if (clothing.glassesAsset == null || clothing.glassesState == null || clothing.glassesState.Length == 0) 
                return;

            // Проверяем включен ли визуальный эффект (байт 0)
            bool isVisualOn = clothing.glassesState[0] != 0;

            if (isVisualOn)
            {
                // Если заряд уже 0, но свет горит — тушим немедленно
                if (clothing.glassesQuality == 0)
                {
                    ForceOff(clothing);
                    return;
                }

                // Ищем конфиг для предмета
                var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == clothing.glassesAsset.id);
                float drainRate = config != null ? config.DrainPerSecond : 0.1f;

                // Накапливаем износ (0.5 сек интервал)
                drainAccumulator += (drainRate * 0.5f);
                
                if (drainAccumulator >= 1f)
                {
                    byte drop = (byte)Mathf.FloorToInt(drainAccumulator);
                    drainAccumulator -= drop;

                    if (clothing.glassesQuality <= drop)
                    {
                        clothing.glassesQuality = 0;
                        // Сначала синхронизируем 0% качества
                        clothing.sendUpdateGlassesQuality();
                        // Затем принудительно гасим свет
                        ForceOff(clothing);
                    }
                    else
                    {
                        clothing.glassesQuality -= drop;
                        clothing.sendUpdateGlassesQuality();
                    }
                }
            }
        }

        private void ForceOff(PlayerClothing clothing)
        {
            // 1. Принудительно правим стейт на сервере (байт 0 отвечает за ON/OFF)
            if (clothing.glassesState != null && clothing.glassesState.Length > 0)
            {
                clothing.glassesState[0] = 0;
            }

            // 2. Отправляем пакет переключения всем игрокам (включая владельца)
            // EVisualToggleType.VISION = 1 (ПНВ и налобные фонари)
            clothing.ServerSetVisualToggleState((EVisualToggleType)1, false);

            // 3. Обновляем локальные источники света для игрока
            player.Player.updateGlassesLights(false);

            // 4. Важный момент: переотправляем качество, так как этот пакет 
            // часто "проталкивает" обновление стейта в некоторых версиях игры
            clothing.sendUpdateGlassesQuality();

#pragma warning disable CS0618
            // Звуковой эффект выключения (клик)
            EffectManager.sendEffect(8, 24, player.Position);
#pragma warning restore CS0618

            drainAccumulator = 0f;
        }
    }
}
