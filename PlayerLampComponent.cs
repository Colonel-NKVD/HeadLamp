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

        // Кэшируем метод SendWearGlasses, который ты нашел в дампе
        private static object sendWearGlassesObject;
        private static MethodInfo sendWearGlassesInvoke;

        void Awake()
        {
            player = UnturnedPlayer.FromPlayer(GetComponent<Player>());

            if (sendWearGlassesInvoke == null)
            {
                // Достаем приватный ClientInstanceMethod из PlayerClothing
                var field = typeof(PlayerClothing).GetField("SendWearGlasses", BindingFlags.NonPublic | BindingFlags.Static);
                if (field != null)
                {
                    sendWearGlassesObject = field.GetValue(null);
                    if (sendWearGlassesObject != null)
                    {
                        // Параметры: ENetReliability, List<ITransportConnection>, Guid, byte, byte[], bool
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
            if (Time.time - lastTick < 0.5f) return;
            lastTick = Time.time;

            CheckAndDrain();
        }

        private void CheckAndDrain()
        {
            var clothing = player.Player.clothing;
            if (clothing.glassesAsset == null) return;

            // Проверяем, горит ли лампа (байт [0] в glassesState)
            bool isVisualOn = clothing.glassesState != null && clothing.glassesState.Length > 0 && clothing.glassesState[0] != 0;

            if (isVisualOn)
            {
                // Если прочность уже 0, но лампа горит — тушим
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
                        ForceOff(clothing); // Тушим при достижении 0%
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

            // 1. Подготавливаем состояние "Выключено"
            if (clothing.glassesState == null || clothing.glassesState.Length == 0)
            {
                clothing.glassesState = new byte[1];
            }
            clothing.glassesState[0] = 0;

            // 2. ИСПОЛЬЗУЕМ ТВОЕ НАБЛЮДЕНИЕ: "Переодеваем" очки через RPC
            // Это заставит всех клиентов (включая владельца) вызвать updateVision()
            if (sendWearGlassesInvoke != null && sendWearGlassesObject != null)
            {
                try
                {
                    sendWearGlassesInvoke.Invoke(sendWearGlassesObject, new object[] 
                    { 
                        ENetReliability.Reliable, 
                        Provider.GatherRemoteClientConnections(), 
                        clothing.glassesAsset.GUID, 
                        clothing.glassesQuality, 
                        clothing.glassesState, 
                        false // Не проигрывать звук надевания, чтобы не спамить
                    });
                }
                catch (Exception) { /* Ошибка Reflection */ }
            }

            // 3. Дублируем стандартными методами для надежности
            clothing.ServerSetVisualToggleState((EVisualToggleType)1, false);
            player.Player.updateGlassesLights(false);

#pragma warning disable CS0618
            EffectManager.sendEffect(8, 24, player.Position);
#pragma warning restore CS0618

            drainAccumulator = 0f;
        }
    }
}
