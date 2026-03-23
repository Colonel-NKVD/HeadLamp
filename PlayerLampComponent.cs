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
            // Проверка раз в 1 секунду для экономии ресурсов
            if (Time.time - lastTick < 1f) return;
            lastTick = Time.time;

            CheckAndDrain();
        }

        private void CheckAndDrain()
        {
            var clothing = player.Player.clothing;

            // Работаем ТОЛЬКО с очками (Glasses/NVG)
            if (clothing.glassesAsset != null)
            {
                var asset = clothing.glassesAsset;
                
                // Проверяем, есть ли у очков вообще функция свечения (ПНВ или фонарь)
                if (asset.vision != ELightingVision.NONE)
                {
                    // Проверяем включены ли они сейчас (байт [0] в стейте: 1 - вкл, 0 - выкл)
                    bool isVisualOn = clothing.glassesState != null && clothing.glassesState.Length > 0 && clothing.glassesState[0] != 0;
                    
                    if (isVisualOn)
                    {
                        // ПАТЧ: Если включено, но прочность уже 0 — выключаем немедленно
                        if (clothing.glassesQuality == 0)
                        {
                            ForceOff(clothing);
                            return;
                        }

                        // Иначе — разряжаем
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
                drainAccumulator += drainRate;
                if (drainAccumulator >= 1f)
                {
                    int drop = Mathf.FloorToInt(drainAccumulator);
                    drainAccumulator -= drop;
                    
                    // Уменьшаем прочность
                    clothing.glassesQuality = (byte)Mathf.Max(0, clothing.glassesQuality - drop);
                    clothing.sendUpdateGlassesQuality();

                    // Если после уменьшения стало 0 — выключаем
                    if (clothing.glassesQuality == 0)
                    {
                        ForceOff(clothing);
                        EffectManager.sendEffect(8, 24, player.Position);
                    }
                }
            }
        }

        private void ForceOff(PlayerClothing clothing)
        {
            if (clothing.glassesState != null && clothing.glassesState.Length > 0)
            {
                // 1. Прямая перезапись байта состояния (0 = выключено)
                clothing.glassesState[0] = 0;

                // 2. Важнейший метод: заставляет сервер отправить обновленный стейт всем клиентам
                // Именно это выключает визуальный свет/ПНВ у игрока
                clothing.sendUpdateGlassesState();

                // Дополнительный вызов для надежности (если доступен в API)
                // player.Player.updateGlassesLights(false); 

                drainAccumulator = 0f;
            }
        }
    }
}
