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
            // Проверка раз в секунду для оптимизации
            if (Time.time - lastTick < 1f) return;
            lastTick = TickUpdate();
        }

        private float TickUpdate()
        {
            CheckAndDrain();
            return Time.time;
        }

        private void CheckAndDrain()
        {
            var clothing = player.Player.clothing;

            // 1. Проверяем ОЧКИ (Glasses/ПНВ)
            if (clothing.glassesAsset != null && clothing.glassesAsset.vision != ELightingVision.NONE)
            {
                // Используем твою находку: проверяем байт state[0]
                bool isVisualOn = clothing.glassesState != null && clothing.glassesState.Length > 0 && clothing.glassesState[0] != 0;
                
                if (isVisualOn)
                {
                    DrainItem(ref clothing.glassesQuality, clothing.glassesAsset.id, false);
                    
                    if (clothing.glassesQuality == 0)
                    {
                        // Выключаем, если прочность 0
                        clothing.ServerSetVisualToggleState(EVisualToggleType.NON_COSMETIC, false);
                    }
                }
            }

            // 2. Проверяем ШАПКИ (Hats/Фонари)
            if (clothing.hatAsset != null && clothing.hatAsset.vision != ELightingVision.NONE)
            {
                // Проверяем hatState[0]
                bool isVisualOn = clothing.hatState != null && clothing.hatState.Length > 0 && clothing.hatState[0] != 0;

                if (isVisualOn)
                {
                    DrainItem(ref clothing.hatQuality, clothing.hatAsset.id, true);

                    if (clothing.hatQuality == 0)
                    {
                        clothing.ServerSetVisualToggleState(EVisualToggleType.NON_COSMETIC, false);
                    }
                }
            }
        }

        private void DrainItem(ref byte quality, ushort itemId, bool isHat)
        {
            // Ищем настройки в конфиге для конкретного ID
            var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == itemId);
            
            // Если в конфиге нет ID, берем базовый расход 0.1% в сек
            float drainRate = config != null ? config.DrainPerSecond : 0.1f;

            if (quality > 0)
            {
                drainAccumulator += drainRate;

                if (drainAccumulator >= 1f)
                {
                    int drop = Mathf.FloorToInt(drainAccumulator);
                    drainAccumulator -= drop;
                    
                    quality = (byte)Mathf.Max(0, quality - drop);

                    // Синхронизация полоски прочности в инвентаре
                    if (isHat) player.Player.clothing.sendUpdateHatQuality();
                    else player.Player.clothing.sendUpdateGlassesQuality();
                }
            }

            if (quality == 0)
            {
                // Звук поломки (ID 8)
                EffectManager.sendEffect(8, 24, player.Position);
                drainAccumulator = 0f;
            }
        }
    }
}
