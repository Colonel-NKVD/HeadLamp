using SDG.Unturned;
using SDG.NetTransport;
using UnityEngine;
using Rocket.Unturned.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HeadLamp
{
    public class PlayerLampComponent : MonoBehaviour
    {
        private UnturnedPlayer player;
        private float lastTick;
        private float drainAccumulator = 0f;

        // Кэшируем методы для работы с сетевым кодом через Reflection
        private static object sendWearGlassesObject;
        private static MethodInfo sendWearGlassesInvoke;

        void Awake()
        {
            player = UnturnedPlayer.FromPlayer(GetComponent<Player>());

            // Инициализация доступа к SendWearGlasses (один раз на весь запуск сервера)
            if (sendWearGlassesInvoke == null)
            {
                var field = typeof(PlayerClothing).GetField("SendWearGlasses", BindingFlags.NonPublic | BindingFlags.Static);
                if (field != null)
                {
                    sendWearGlassesObject = field.GetValue(null);
                    if (sendWearGlassesObject != null)
                    {
                        // Сигнатура из твоего дампа: Guid, byte (quality), byte[] (state), bool (playEffect)
                        sendWearGlassesInvoke = sendWearGlassesObject.GetType().GetMethod("Invoke", new Type[] 
                        { 
                            typeof(ENetReliability), 
                            typeof(List<ITransportConnection>), 
                            typeof(Guid), 
                            typeof(byte), 
                            typeof(byte[]), 
                            typeof(bool) 
                        });
                    }
                }
            }
        }

        void FixedUpdate()
        {
            // Проверка раз в 0.5 сек для оптимизации
            if (Time.time - lastTick < 0.5f) return;
            lastTick = Time.time;

            CheckAndDrain();
        }

        private void CheckAndDrain()
        {
            var clothing = player.Player.clothing;
            if (clothing.glassesAsset == null) return;

            // Проверяем, горит ли лампа (первый байт состояния)
            bool isVisualOn = clothing.glassesState != null && clothing.glassesState.Length > 0 && clothing.glassesState[0] != 0;

            if (isVisualOn)
            {
                // Если заряд уже на нуле, но свет горит — принудительно гасим
                if (clothing.glassesQuality == 0)
                {
                    ForceOff(clothing);
                    return;
                }

                // Получаем скорость разряда из конфига
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
                        ForceOff(clothing); // Гасим свет ПЕРЕД окончательной установкой 0
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

            // Сохраняем данные текущего предмета
            Guid itemGuid = clothing.glassesAsset.GUID;
            byte[] offState = new byte[clothing.glassesState?.Length ?? 1];
            for (int i = 0; i < offState.Length; i++) offState[i] = 0; // Состояние "Всё выключено"

            // 1. Обновляем данные на сервере
            clothing.glassesState = offState;

            // 2. РЕАЛИЗАЦИЯ ТВОЕГО НАБЛЮДЕНИЯ: "Виртуальный Свап"
            if (sendWearGlassesInvoke != null && sendWearGlassesObject != null)
            {
                try
                {
                    var connections = Provider.GatherRemoteClientConnections();

                    // ШАГ А: Отправляем пакет "Снять очки" (пустой GUID)
                    sendWearGlassesInvoke.Invoke(sendWearGlassesObject, new object[] 
                    { 
                        ENetReliability.Reliable, 
                        connections, 
                        Guid.Empty, 
                        (byte)0, 
                        new byte[0], 
                        false 
                    });

                    // ШАГ Б: Мгновенно отправляем пакет "Надеть очки" (с нашими данными)
                    // Это заставит клиент пересоздать все визуальные объекты фонаря
                    sendWearGlassesInvoke.Invoke(sendWearGlassesObject, new object[] 
                    { 
                        ENetReliability.Reliable, 
                        connections, 
                        itemGuid, 
                        (byte)0, 
                        offState, 
                        false 
                    });
                }
                catch (Exception) 
                {
                    // Если Reflection не сработал, пробуем стандартный метод
                    clothing.ServerSetVisualToggleState((EVisualToggleType)1, false);
                }
            }

            // 3. Дополнительная зачистка (звуки, эффекты, локальный свет)
            player.Player.updateGlassesLights(false);

#pragma warning disable CS0618
            EffectManager.sendEffect(8, 24, player.Position);
#pragma warning restore CS0618

            drainAccumulator = 0f;
        }
    }
}
