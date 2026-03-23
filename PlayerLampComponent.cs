using SDG.Unturned;
using UnityEngine;
using Rocket.Unturned.Player;
using System;
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
                if (clothing.glassesQuality == 0)
                {
                    ForceOff(clothing);
                    return;
                }

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
                        ForceOff(clothing);
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
            if (clothing.glassesAsset == null) return;

            // 1. Сохраняем данные текущих очков
            Guid currentGuid = clothing.glassesGuid;
            byte currentQuality = clothing.glassesQuality;
            
            // Создаем выключенный стейт
            byte[] newState = new byte[clothing.glassesState.Length];
            Array.Copy(clothing.glassesState, newState, clothing.glassesState.Length);
            newState[0] = 0; 

            // 2. ЖЕСТКИЙ СБРОС (Force Re-apply)
            // Убираем GUID очков из системы одежды сервера
            clothing.glassesGuid = Guid.Empty;
            clothing.glassesState = new byte[0];
            
            // Применяем изменения (это заставит сервер послать пакет "очков нет")
            clothing.apply();

            // 3. Возвращаем очки обратно с выключенным состоянием
            clothing.glassesGuid = currentGuid;
            clothing.glassesQuality = currentQuality;
            clothing.glassesState = newState;

            // Снова применяем (сервер пошлет пакет "надеты новые очки")
            clothing.apply();

            // 4. Дополнительная синхронизация через старые методы
            clothing.ServerSetVisualToggleState((EVisualToggleType)1, false);
            player.Player.updateGlassesLights(false);

#pragma warning disable CS0618
            EffectManager.sendEffect(8, 24, player.Position);
#pragma warning restore CS0618

            drainAccumulator = 0f;
            
            // Логируем для отладки в консоль сервера (удали потом, если мешает)
            // Rocket.Core.Logging.Logger.Log($"[HeadLamp] Forced OFF for {player.CharacterName}");
        }
    }
}
