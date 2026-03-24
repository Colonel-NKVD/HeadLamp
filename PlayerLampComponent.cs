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
            // ПОДПИСКА: Ловим момент, когда игрок нажимает 'N' или меняет очки
            clothing.onGlassesUpdated += OnGlassesUpdated;
        }

        void OnDestroy()
        {
            // ОТПИСКА: Обязательно убираем за собой при выходе игрока
            if (clothing != null)
                clothing.onGlassesUpdated -= OnGlassesUpdated;
        }

        // Этот метод вызывается игрой каждый раз, когда меняется состояние очков
        private void OnGlassesUpdated(PlayerClothing clothing)
        {
            if (clothing.glassesAsset == null || clothing.glassesState == null || clothing.glassesState.Length == 0)
                return;

            // Если прибор включен (байт 0 != 0), но заряд 0% — не даем включить
            if (clothing.glassesState[0] != 0 && clothing.glassesQuality == 0)
            {
                // Мгновенно тушим «в зачатке»
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

            // Проверяем, горит ли свет сейчас
            bool isVisualOn = clothing.glassesState[0] != 0;

            if (isVisualOn)
            {
                // Если заряд уже 0, а свет горит (например, после входа на сервер)
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
                        ForceOff(); // Выключаем, так как заряд упал в 0
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
            // 1. Устанавливаем байт состояния в 0 (выключено)
            if (clothing.glassesState != null && clothing.glassesState.Length > 0)
            {
                clothing.glassesState[0] = 0;
            }

            // 2. Отправляем пакет на выключение всех типов визуальных эффектов
            // Индекс 1 (VISION) - основной для ПНВ и Фонарей
            clothing.ServerSetVisualToggleState((EVisualToggleType)1, false);
            // Индекс 0 (TACTICAL) - на случай, если мод использует этот канал
            clothing.ServerSetVisualToggleState((EVisualToggleType)0, false);

            // 3. Принудительно гасим Unity-источники света на голове
            player.Player.updateGlassesLights(false);

            // 4. Синхронизируем качество (это заставляет клиент перепроверить стейт предмета)
            clothing.sendUpdateGlassesQuality();

            // Эффект щелчка (звук)
#pragma warning disable CS0618
            EffectManager.sendEffect(8, 24, player.Position);
#pragma warning restore CS0618

            drainAccumulator = 0f;
        }
    }
}
