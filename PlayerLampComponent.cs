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
            if (Time.time - lastTick < 1f) return;
            lastTick = Time.time;

            CheckAndDrain();
        }

        private void CheckAndDrain()
        {
            var clothing = player.Player.clothing;

            // 1. Проверяем ОЧКИ
            if (clothing.glassesAsset != null)
            {
                // Проверяем наличие vision через каст
                var asset = clothing.glassesAsset;
                if (asset.vision != ELightingVision.NONE)
                {
                    bool isVisualOn = clothing.glassesState != null && clothing.glassesState.Length > 0 && clothing.glassesState[0] != 0;
                    if (isVisualOn)
                    {
                        DrainItem(ref clothing.glassesQuality, asset.id, false);
                        if (clothing.glassesQuality == 0)
                        {
                            // Используем индекс (EVisualToggleType)1 вместо имени NON_COSMETIC
                            clothing.ServerSetVisualToggleState((EVisualToggleType)1, false);
                        }
                    }
                }
            }

            // 2. Проверяем ШАПКУ
            if (clothing.hatAsset != null)
            {
                var asset = clothing.hatAsset;
                // В некоторых версиях у ItemHatAsset свойство vision может быть в базовом классе или скрыто
                // Используем прямое сравнение, если оно доступно
                if (asset.vision != ELightingVision.NONE)
                {
                    bool isVisualOn = clothing.hatState != null && clothing.hatState.Length > 0 && clothing.hatState[0] != 0;
                    if (isVisualOn)
                    {
                        DrainItem(ref clothing.hatQuality, asset.id, true);
                        if (clothing.hatQuality == 0)
                        {
                            clothing.ServerSetVisualToggleState((EVisualToggleType)1, false);
                        }
                    }
                }
            }
        }

        private void DrainItem(ref byte quality, ushort itemId, bool isHat)
        {
            var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == itemId);
            float drainRate = config != null ? config.DrainPerSecond : 0.1f;

            if (quality > 0)
            {
                drainAccumulator += drainRate;
                if (drainAccumulator >= 1f)
                {
                    int drop = Mathf.FloorToInt(drainAccumulator);
                    drainAccumulator -= drop;
                    quality = (byte)Mathf.Max(0, quality - drop);

                    if (isHat) player.Player.clothing.sendUpdateHatQuality();
                    else player.Player.clothing.sendUpdateGlassesQuality();
                }
            }

            if (quality == 0)
            {
                EffectManager.sendEffect(8, 24, player.Position);
                drainAccumulator = 0f;
            }
        }
    }
}
