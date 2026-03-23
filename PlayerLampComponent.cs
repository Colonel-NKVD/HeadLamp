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

        // Кэшируем метод и поле для скорости
        private static object sendWearGlassesInstance;
        private static MethodInfo sendWearGlassesInvoke;

        void Awake()
        {
            player = UnturnedPlayer.FromPlayer(GetComponent<Player>());

            // Инициализируем доступ к приватному методу SendWearGlasses один раз
            if (sendWearGlassesInvoke == null)
            {
                var field = typeof(PlayerClothing).GetField("SendWearGlasses", BindingFlags.NonPublic | BindingFlags.Static);
                if (field != null)
                {
                    sendWearGlassesInstance = field.GetValue(null);
                    if (sendWearGlassesInstance != null)
                    {
                        // Ищем метод Invoke(ENetReliability, List<ITransportConnection>, Guid, byte, byte[], bool)
                        sendWearGlassesInvoke = sendWearGlassesInstance.GetType().GetMethod("Invoke", new Type[] 
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
            if (Time.time - lastTick < 0.5f) return;
            lastTick = Time.time;

            CheckAndDrain();
        }

        private void CheckAndDrain()
        {
            var clothing = player.Player.clothing;
            if (clothing.glassesAsset == null) return;

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
            if (clothing.glassesAsset == null || clothing.glassesState == null || clothing.glassesState.Length == 0) return;

            // 1. Создаем НОВЫЙ массив состояния, чтобы игра точно увидела изменения
            byte[] newState = new byte[clothing.glassesState.Length];
            Array.Copy(clothing.glassesState, newState, clothing.glassesState.Length);
            newState[0] = 0; // Выключаем

            // Обновляем состояние на сервере
            clothing.glassesState[0] = 0;

            // 2. ОТПРАВЛЯЕМ ПАКЕТ (RPC)
            // Это заставит клиент перерисовать очки и вызвать updateVision()
            if (sendWearGlassesInvoke != null)
            {
                try
                {
                    sendWearGlassesInvoke.Invoke(sendWearGlassesInstance, new object[] 
                    { 
                        ENetReliability.Reliable, 
                        Provider.GatherRemoteClientConnections(), 
                        clothing.glassesAsset.GUID, 
                        clothing.glassesQuality, 
                        newState, 
                        false 
                    });
                }
                catch (Exception) { /* Игнорируем ошибки рефлексии */ }
            }

            // 3. Дополнительные попытки для разных типов предметов
            clothing.ServerSetVisualToggleState((EVisualToggleType)1, false);
            player.Player.updateGlassesLights(false);

#pragma warning disable CS0618
            EffectManager.sendEffect(8, 24, player.Position);
#pragma warning restore CS0618

            drainAccumulator = 0f;
        }
    }
}
