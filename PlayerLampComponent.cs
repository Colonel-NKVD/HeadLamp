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

        // Кэшируем метод полной синхронизации очков
        private static MethodInfo sendWearGlassesMethod;
        private static object sendWearGlassesObject;

        void Awake()
        {
            player = UnturnedPlayer.FromPlayer(GetComponent<Player>());

            if (sendWearGlassesMethod == null)
            {
                // Ищем SendWearGlasses — это самый надежный способ обновить предмет у всех
                var fieldInfo = typeof(PlayerClothing).GetField("SendWearGlasses", BindingFlags.NonPublic | BindingFlags.Static);
                if (fieldInfo != null)
                {
                    sendWearGlassesObject = fieldInfo.GetValue(null);
                    if (sendWearGlassesObject != null)
                    {
                        // Пакет принимает: Reliable, Connections, Guid, Quality, State, PlayEffect
                        sendWearGlassesMethod = sendWearGlassesObject.GetType().GetMethod("Invoke", new Type[] 
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

            // Проверяем включен ли прибор (байт 0)
            bool isVisualOn = clothing.glassesState != null && clothing.glassesState.Length > 0 && clothing.glassesState[0] != 0;

            if (isVisualOn)
            {
                // Если прочность 0, но он горит — гасим немедленно
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

            // 1. Принудительно гасим байт в стейте
            if (clothing.glassesState != null && clothing.glassesState.Length > 0)
            {
                clothing.glassesState[0] = 0;
            }

            // 2. ПОЛНАЯ СИНХРОНИЗАЦИЯ (Force Refresh)
            // Мы вызываем метод, который заставляет всех клиентов перерисовать очки игрока
            if (sendWearGlassesMethod != null && sendWearGlassesObject != null)
            {
                try
                {
                    sendWearGlassesMethod.Invoke(sendWearGlassesObject, new object[] 
                    { 
                        ENetReliability.Reliable, 
                        Provider.GatherRemoteClientConnections(), 
                        clothing.glassesAsset.GUID, // Используем GUID ассета
                        clothing.glassesQuality, 
                        clothing.glassesState, 
                        false // Не играть звук надевания
                    });
                }
                catch (Exception)
                {
                    // Если основной метод не сработал, пробуем ванильный переключатель
                    clothing.ServerSetVisualToggleState((EVisualToggleType)1, false);
                }
            }

            // 3. Дополнительно гасим локальные источники света
            player.Player.updateGlassesLights(false);
            
#pragma warning disable CS0618
            EffectManager.sendEffect(8, 24, player.Position);
#pragma warning restore CS0618

            drainAccumulator = 0f;
        }
    }
}
