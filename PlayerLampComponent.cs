using SDG.Unturned;
using UnityEngine;
using Rocket.Unturned.Player;
using System.Linq;
using System.Reflection;

namespace HeadLamp
{
    public class PlayerLampComponent : MonoBehaviour
    {
        private UnturnedPlayer player;
        private float lastTick;
        private float drainAccumulator = 0f;

        // Кэшируем метод отправки пакета
        private MethodInfo sendVisualToggleStateMethod;

        void Awake()
        {
            player = UnturnedPlayer.FromPlayer(GetComponent<Player>());

            // Находим приватный метод SendVisualToggleState через Reflection
            var fieldInfo = typeof(PlayerClothing).GetField("SendVisualToggleState", BindingFlags.NonPublic | BindingFlags.Static);
            if (fieldInfo != null)
            {
                var clientInstanceMethod = fieldInfo.GetValue(null);
                if (clientInstanceMethod != null)
                {
                    sendVisualToggleStateMethod = clientInstanceMethod.GetType().GetMethod("Invoke", new System.Type[] { typeof(ENetReliability), typeof(System.Collections.Generic.List<ITransportConnection>), typeof(EVisualToggleType), typeof(bool) });
                    
                    // Резервный поиск, если сигнатура Invoke другая
                    if (sendVisualToggleStateMethod == null)
                    {
                         sendVisualToggleStateMethod = clientInstanceMethod.GetType().GetMethod("Invoke", new System.Type[] { typeof(ENetReliability), typeof(ITransportConnection), typeof(EVisualToggleType), typeof(bool) });
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

            if (clothing.glassesAsset != null)
            {
                var asset = clothing.glassesAsset;
                
                if (asset.vision != ELightingVision.NONE)
                {
                    bool isVisualOn = clothing.glassesState != null && clothing.glassesState.Length > 0 && clothing.glassesState[0] != 0;
                    
                    if (isVisualOn)
                    {
                        if (clothing.glassesQuality == 0)
                        {
                            ForceOff(clothing);
                            return;
                        }

                        DrainItem(clothing, asset.id);
                    }
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
                        ForceOff(clothing); // Сначала гасим свет
                        
                        clothing.glassesQuality = 0;
                        clothing.sendUpdateGlassesQuality();

#pragma warning disable CS0618 // Игнорируем предупреждение об устаревшем методе эффекта
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
                // 1. Принудительно ставим 0 в памяти сервера
                clothing.glassesState[0] = 0;

                // 2. Отправляем пакет клиентам на выключение через найденный метод
                if (sendVisualToggleStateMethod != null)
                {
                    var fieldInfo = typeof(PlayerClothing).GetField("SendVisualToggleState", BindingFlags.NonPublic | BindingFlags.Static);
                    var clientInstanceMethod = fieldInfo.GetValue(null);
                    
                    // Вызываем Invoke(ENetReliability.Reliable, Provider.GatherClientConnections(), type, wantsOn)
                    sendVisualToggleStateMethod.Invoke(clientInstanceMethod, new object[] { ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), (EVisualToggleType)1, false });
                }
                else
                {
                    // Fallback на старый метод, если Reflection не сработал
                    clothing.ServerSetVisualToggleState((EVisualToggleType)1, false);
                }

                // 3. Выключаем локально (если игрок - админ в ванише или локальный сервер)
                player.Player.updateGlassesLights(false);

                drainAccumulator = 0f;
            }
        }
    }
}
