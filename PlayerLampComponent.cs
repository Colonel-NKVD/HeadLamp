using SDG.Unturned;
using SDG.NetTransport; // Важно: это лечит ошибку ENetReliability
using UnityEngine;
using Rocket.Unturned.Player;
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

        // Кэшируем метод для отправки пакета
        private static MethodInfo sendVisualToggleStateMethod;
        private static object clientInstanceMethodObject;

        void Awake()
        {
            player = UnturnedPlayer.FromPlayer(GetComponent<Player>());

            // Инициализируем Reflection только один раз для всех компонентов (static)
            if (sendVisualToggleStateMethod == null)
            {
                var fieldInfo = typeof(PlayerClothing).GetField("SendVisualToggleState", BindingFlags.NonPublic | BindingFlags.Static);
                if (fieldInfo != null)
                {
                    clientInstanceMethodObject = fieldInfo.GetValue(null);
                    if (clientInstanceMethodObject != null)
                    {
                        // Ищем метод Invoke, не привязываясь к конкретным типам в GetMethod, чтобы избежать ошибок компиляции
                        sendVisualToggleStateMethod = clientInstanceMethodObject.GetType().GetMethods()
                            .FirstOrDefault(m => m.Name == "Invoke" && m.GetParameters().Length == 4);
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

            if (clothing.glassesAsset.vision != ELightingVision.NONE)
            {
                bool isVisualOn = clothing.glassesState != null && clothing.glassesState.Length > 0 && clothing.glassesState[0] != 0;
                
                if (isVisualOn)
                {
                    if (clothing.glassesQuality == 0)
                    {
                        ForceOff(clothing);
                        return;
                    }

                    DrainItem(clothing, clothing.glassesAsset.id);
                }
            }
        }

        private void DrainItem(PlayerClothing clothing, ushort itemId)
        {
            var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == itemId);
            float drainRate = config != null ? config.DrainPerSecond : 0.1f;

            if (clothing.glassesQuality > 0)
            {
                drainAccumulator += (drainRate * 0.5f); 
                if (drainAccumulator >= 1f)
                {
                    int drop = Mathf.FloorToInt(drainAccumulator);
                    drainAccumulator -= drop;
                    
                    if (clothing.glassesQuality <= drop)
                    {
                        ForceOff(clothing); // Сначала выключаем визуал
                        
                        clothing.glassesQuality = 0;
                        clothing.sendUpdateGlassesQuality();

#pragma warning disable CS0618
                        EffectManager.sendEffect(8, 24, player.Position);
#pragma warning restore CS0618
                    }
                    else
                    {
                        clothing.glassesQuality = (byte)(clothing.glassesQuality - drop);
                        clothing.sendUpdateGlassesQuality();
                    }
                }
            }
        }

        private void ForceOff(PlayerClothing clothing)
        {
            if (clothing.glassesState != null && clothing.glassesState.Length > 0)
            {
                // 1. Меняем состояние в памяти сервера
                clothing.glassesState[0] = 0;

                // 2. Отправляем RPC пакет через найденный ClientInstanceMethod
                if (sendVisualToggleStateMethod != null && clientInstanceMethodObject != null)
                {
                    try 
                    {
                        // Вызов: Invoke(Reliable, Connections, Type, State)
                        sendVisualToggleStateMethod.Invoke(clientInstanceMethodObject, new object[] 
                        { 
                            ENetReliability.Reliable, 
                            Provider.GatherRemoteClientConnections(), 
                            (EVisualToggleType)1, 
                            false 
                        });
                    }
                    catch { /* Если не вышло через Reflection, попробуем ванильный метод */ }
                }
                
                // 3. Запасной/дублирующий вариант
                clothing.ServerSetVisualToggleState((EVisualToggleType)1, false);

                // 4. Локальное обновление для самого игрока
                player.Player.updateGlassesLights(false);

                drainAccumulator = 0f;
            }
        }
    }
}
