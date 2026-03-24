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
            clothing.onGlassesUpdated += OnGlassesUpdated;
        }

        void OnDestroy()
        {
            if (clothing != null)
                clothing.onGlassesUpdated -= OnGlassesUpdated;
        }

        private void OnGlassesUpdated(ushort id, byte quality, byte[] state)
        {
            // Если игрок включил прибор при 0% прочности
            if (state != null && state.Length > 0 && state[0] != 0 && quality == 0)
            {
                // Имитируем жесткое выключение
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
            if (clothing.glassesAsset == null || clothing.glassesState == null || clothing.glassesState.Length == 0)
                return;

            // Проверяем 0-й байт (состояние включения)
            if (clothing.glassesState[0] != 0)
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
                        ForceOff();
                    }
                    else
                    {
                        clothing.glassesQuality -= drop;
                        // Синхронизируем качество обычным способом
                        clothing.sendUpdateGlassesQuality();
                    }
                }
            }
        }

        /// <summary>
        /// Самый жесткий способ выключить прибор: 
        /// Принудительная переотправка состояния предмета как при его надевании.
        /// </summary>
        private void ForceOff()
        {
            if (clothing.glassesAsset == null) return;

            // 1. Подготавливаем "выключенный" стейт
            byte[] newState = clothing.glassesState;
            if (newState != null && newState.Length > 0)
            {
                newState[0] = 0; // Выключаем
            }

            // 2. Устанавливаем качество в 0
            clothing.glassesQuality = 0;

            // 3. Явное выключение через RPC (VISION и TACTICAL)
            // Мы вызываем это ДО переотправки стейта
            clothing.ServerSetVisualToggleState((EVisualToggleType)1, false);
            clothing.ServerSetVisualToggleState((EVisualToggleType)0, false);

            // 4. ГЛАВНЫЙ КОСТЫЛЬ: Переотправка всего предмета (tellWearGlasses)
            // Это заставляет клиент пересоздать объект очков в Unity.
            // Используем внутренний метод синхронизации.
            clothing.sendUpdateGlassesQuality(); 
            
            // В Unturned 3.x метод обновления всего состояния очков выглядит так:
            // Он заставляет всех игроков (включая владельца) обновить модель предмета.
            player.Player.clothing.askUpdateGlasses(
                clothing.glassesAsset.id, 
                0, 
                newState
            );

            // 5. Принудительное гашение света в движке игрока
            player.Player.updateGlassesLights(false);

#pragma warning disable CS0618
            EffectManager.sendEffect(8, 24, player.Position);
#pragma warning restore CS0618

            drainAccumulator = 0f;
        }
    }
}
