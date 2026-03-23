using SDG.Unturned;
using UnityEngine;
using Rocket.Unturned.Player;
using System;
using System.Linq;
using System.Reflection;

namespace HeadLamp
{
    public class PlayerLampComponent : MonoBehaviour
    {
        private UnturnedPlayer player;
        private float lastTick;
        private float drainAccumulator = 0f;

        // Кэшируем методы HumanClothes для производительности
        private static FieldInfo glassesGuidField;
        private static MethodInfo applyMethod;

        void Awake()
        {
            player = UnturnedPlayer.FromPlayer(GetComponent<Player>());

            // Находим поле glassesGuid и метод apply в классе HumanClothes
            if (applyMethod == null)
            {
                glassesGuidField = typeof(HumanClothes).GetField("glassesGuid", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                applyMethod = typeof(HumanClothes).GetMethod("apply", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }
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

            // Проверяем включен ли фонарь (байт 0 состояния)
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
            if (clothing.thirdClothes == null || clothing.glassesAsset == null) return;

            try
            {
                // 1. Сохраняем текущий Guid
                Guid currentGuid = clothing.glassesAsset.GUID;

                // 2. Создаем выключенное состояние (стейт)
                byte[] offState = new byte[clothing.glassesState.Length];
                Array.Copy(clothing.glassesState, offState, clothing.glassesState.Length);
                offState[0] = 0;

                // 3. Обновляем данные в PlayerClothing (серверная часть)
                clothing.glassesState = offState;

                // 4. "СБРОС" через HumanClothes (визуальная часть)
                // Имитируем снятие очков
                glassesGuidField?.SetValue(clothing.thirdClothes, Guid.Empty);
                applyMethod?.Invoke(clothing.thirdClothes, null);

                // Имитируем надевание очков обратно с новым состоянием
                glassesGuidField?.SetValue(clothing.thirdClothes, currentGuid);
                // Важно: в некоторых версиях нужно также обновить glassesState в thirdClothes
                var stateField = typeof(HumanClothes).GetField("glassesState", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                stateField?.SetValue(clothing.thirdClothes, offState);

                applyMethod?.Invoke(clothing.thirdClothes, null);

                // 5. Принудительная отправка пакета визуального состояния
                clothing.ServerSetVisualToggleState((EVisualToggleType)1, false);
                player.Player.updateGlassesLights(false);
                
                // Синхронизируем изменения с клиентами (важно!)
                clothing.sendUpdateGlassesQuality(); 
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogError("Ошибка принудительного выключения: " + ex.Message);
            }

#pragma warning disable CS0618
            EffectManager.sendEffect(8, 24, player.Position);
#pragma warning restore CS0618

            drainAccumulator = 0f;
        }
    }
}
