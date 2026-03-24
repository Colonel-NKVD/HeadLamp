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
            if (Time.time - lastTick < 0.5f) return;
            lastTick = Time.time;

            CheckAndDrain();
        }

        private void CheckAndDrain()
        {
            var clothing = player.Player.clothing;

            // 1. Проверяем слот ОЧКОВ (ПНВ, Headlamp 1199)
            if (clothing.glassesAsset != null && clothing.glassesState != null && clothing.glassesState.Length > 0 && clothing.glassesState[0] != 0)
            {
                ProcessGlassesDrain(clothing);
                return; // Обрабатываем один прибор за раз
            }

            // 2. Проверяем слот ШАПКИ/КАСКИ (Miner Helmet 318, модовые шлемы с фонарем)
            if (clothing.hatAsset != null && clothing.hatState != null && clothing.hatState.Length > 0 && clothing.hatState[0] != 0)
            {
                ProcessHatDrain(clothing);
            }
        }

        private void ProcessGlassesDrain(PlayerClothing clothing)
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

        private void ProcessHatDrain(PlayerClothing clothing)
        {
            if (clothing.hatQuality == 0)
            {
                ForceOff(clothing);
                return;
            }

            var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == clothing.hatAsset.id);
            float drainRate = config != null ? config.DrainPerSecond : 0.1f;

            drainAccumulator += (drainRate * 0.5f);
            if (drainAccumulator >= 1f)
            {
                int drop = Mathf.FloorToInt(drainAccumulator);
                drainAccumulator -= drop;

                if (clothing.hatQuality <= drop)
                {
                    clothing.hatQuality = 0;
                    clothing.sendUpdateHatQuality();
                    ForceOff(clothing);
                }
                else
                {
                    clothing.hatQuality -= (byte)drop;
                    clothing.sendUpdateHatQuality();
                }
            }
        }

        private void ForceOff(PlayerClothing clothing)
        {
            // Встроенный метод сервера ServerSetVisualToggleState сам меняет байт состояния (state[0] = 0)
            // и сам рассылает RPC всем клиентам. Нам не нужно менять байты вручную.

            // Глушим режим VISION (ПНВ, налобные фонари)
            clothing.ServerSetVisualToggleState((EVisualToggleType)1, false);
            
            // Глушим режим TACTICAL (Некоторые модовые тактические шлемы)
            clothing.ServerSetVisualToggleState((EVisualToggleType)0, false);

            // Страховка: принудительно гасим локальные источники света на сервере
            player.Player.updateGlassesLights(false);
            player.Player.updateHatLights(false);

            // Звук выключения для атмосферы (по желанию можешь закомментить)
#pragma warning disable CS0618
            EffectManager.sendEffect(8, 24, player.Position);
#pragma warning restore CS0618

            drainAccumulator = 0f;
        }
    }
}
