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

            // Проверяем включен ли свет (байт [0] не равен 0)
            bool isVisualOn = clothing.glassesState != null && clothing.glassesState.Length > 0 && clothing.glassesState[0] != 0;

            if (isVisualOn)
            {
                // Если включено, но прочности 0 — выключаем принудительно
                if (clothing.glassesQuality == 0)
                {
                    ForceOff(clothing);
                    return;
                }

                // Логика разряда
                var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == clothing.glassesAsset.id);
                float drainRate = config != null ? config.DrainPerSecond : 0.1f;

                drainAccumulator += (drainRate * 0.5f);
                if (drainAccumulator >= 1f)
                {
                    int drop = Mathf.FloorToInt(drainAccumulator);
                    drainAccumulator -= drop;

                    if (clothing.glassesQuality <= drop)
                    {
                        clothing.glassesQuality = 0;
                        clothing.sendUpdateGlassesQuality();
                        ForceOff(clothing); // Сначала 0 прочности, потом выключаем
                    }
                    else
                    {
                        clothing.glassesQuality -= (byte)drop;
                        clothing.sendUpdateGlassesQuality();
                    }
                }
            }
        }

        private void ForceOff(PlayerClothing clothing)
        {
            if (clothing.glassesState == null || clothing.glassesState.Length == 0) return;

            // 1. Меняем состояние на "выключено"
            clothing.glassesState[0] = 0;

            // 2. ЯДЕРНЫЙ МЕТОД: Используем встроенный метод синхронизации "всего сразу"
            // Мы вызываем обновление очков так, будто игрок их только что надел.
            // Это гарантированно заставит клиент пересчитать updateVision()
            clothing.updateGlasses(clothing.glasses, clothing.glassesQuality, clothing.glassesState);

            // 3. Дополнительно шлем пакет на переключение визуала (для ПНВ)
            clothing.ServerSetVisualToggleState((EVisualToggleType)1, false);

            // 4. Гасим фонари локально (для тех кто рядом)
            player.Player.updateGlassesLights(false);

#pragma warning disable CS0618
            EffectManager.sendEffect(8, 24, player.Position);
#pragma warning restore CS0618

            drainAccumulator = 0f;
        }
    }
}
