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
        private PlayerClothing clothing;

        void Awake()
        {
            player = UnturnedPlayer.FromPlayer(GetComponent<Player>());
            clothing = player.Player.clothing;
        }

        void Start()
        {
            // Исправленная подписка: теперь сигнатура метода OnGlassesUpdated совпадает с GlassesUpdated
            clothing.onGlassesUpdated += OnGlassesUpdated;
        }

        void OnDestroy()
        {
            if (clothing != null)
                clothing.onGlassesUpdated -= OnGlassesUpdated;
        }

        // ПРАВИЛЬНАЯ СИГНАТУРА: (ushort id, byte quality, byte[] state)
        private void OnGlassesUpdated(ushort id, byte quality, byte[] state)
        {
            // Если очков нет или стейт пустой - выходим
            if (state == null || state.Length == 0)
                return;

            // Если прибор включен (state[0] != 0), но качество 0 — гасим немедленно
            if (state[0] != 0 && quality == 0)
            {
                ForceOff();
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
            // Проверка через кешированный объект clothing
            if (clothing.glassesAsset == null || clothing.glassesState == null || clothing.glassesState.Length == 0) 
                return;

            bool isVisualOn = clothing.glassesState[0] != 0;

            if (isVisualOn)
            {
                if (clothing.glassesQuality == 0)
                {
                    ForceOff();
                    return;
                }

                var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == clothing.glassesAsset.id);
                float drainRate = config != null ? config.DrainPerSecond : 0.1f;

                drainAccumulator += (drainRate * 0.5f);
                
                if (drainAccumulator >= 1f)
                {
                    byte drop = (byte)Mathf.FloorToInt(drainAccumulator);
                    drainAccumulator -= drop;

                    if (clothing.glassesQuality <= drop)
                    {
                        clothing.glassesQuality = 0;
                        clothing.sendUpdateGlassesQuality();
                        ForceOff();
                    }
                    else
                    {
                        clothing.glassesQuality -= drop;
                        clothing.sendUpdateGlassesQuality();
                    }
                }
            }
        }

        private void ForceOff()
        {
            // 1. Меняем состояние в памяти сервера
            if (clothing.glassesState != null && clothing.glassesState.Length > 0)
            {
                clothing.glassesState[0] = 0;
            }

            // 2. Рассылаем RPC пакеты на выключение визуалов
            clothing.ServerSetVisualToggleState((EVisualToggleType)1, false); // VISION
            clothing.ServerSetVisualToggleState((EVisualToggleType)0, false); // TACTICAL

            // 3. Гасим локальный свет игрока
            player.Player.updateGlassesLights(false);

            // 4. Синхронизируем стейт и качество с клиентом
            clothing.sendUpdateGlassesQuality();

#pragma warning disable CS0618
            EffectManager.sendEffect(8, 24, player.Position);
#pragma warning restore CS0618

            drainAccumulator = 0f;
        }
    }
}
