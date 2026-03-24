using SDG.Unturned;
using UnityEngine;
using System.Linq;

namespace HeadLamp
{
    public class PlayerLampComponent : MonoBehaviour
    {
        // Используем нативный класс Player (он быстрее, так как не вызывает обертки RocketMod)
        private Player player;
        private PlayerClothing clothing;
        
        private float lastTick;
        private float drainAccumulator = 0f;
        
        // --- КЭШИРОВАНИЕ ---
        // Эти переменные избавят сервер от лишних вычислений каждый кадр
        private bool hasLampEquipped = false;
        private float currentDrainRate = 0f;

        void Awake()
        {
            player = GetComponent<Player>();
            clothing = player.clothing;
        }

        void Start()
        {
            // Подписываемся на смену очков, чтобы обновлять наш кэш
            clothing.onGlassesUpdated += OnGlassesUpdated;
            
            // Проверяем, что надето на игроке прямо сейчас (при входе на сервер)
            UpdateCache(clothing.glassesAsset != null ? clothing.glassesAsset.id : (ushort)0);
        }

        void OnDestroy()
        {
            if (clothing != null)
            {
                clothing.onGlassesUpdated -= OnGlassesUpdated;
            }
        }

        // Событие срабатывает ТОЛЬКО когда игрок снимает/надевает очки
        private void OnGlassesUpdated(ushort id, byte quality, byte[] state)
        {
            UpdateCache(id);
        }

        // Обновляем информацию о скорости разряда
        private void UpdateCache(ushort id)
        {
            drainAccumulator = 0f; // Сбрасываем таймер при смене предмета

            if (id == 0)
            {
                hasLampEquipped = false;
                currentDrainRate = 0f;
                return;
            }

            // Ищем предмет в конфиге ОДИН раз при надевании, а не каждый тик
            var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == id);
            
            if (config != null)
            {
                hasLampEquipped = true;
                currentDrainRate = config.DrainPerSecond;
            }
            else
            {
                hasLampEquipped = false;
                currentDrainRate = 0f;
            }
        }

        void Update()
        {
            // Проверка каждые 0.5 секунд
            if (Time.time - lastTick < 0.5f) return;
            lastTick = Time.time;

            // Если предмет не из конфига (или слот пуст) — выходим, экономя ресурсы
            if (!hasLampEquipped || clothing.glassesAsset == null) 
                return;

            // Проверяем, включен ли прибор (байт 0 не равен 0)
            bool isVisualOn = clothing.glassesState != null && clothing.glassesState.Length > 0 && clothing.glassesState[0] != 0;

            if (isVisualOn)
            {
                // Если заряд уже 0, нам ничего делать не нужно. 
                // Harmony уже должен был всё выключить.
                if (clothing.glassesQuality == 0)
                    return;

                // Накапливаем разряд. Умножаем на 0.5, так как тик проходит раз в полсекунды.
                drainAccumulator += (currentDrainRate * 0.5f);
                
                if (drainAccumulator >= 1f)
                {
                    byte drop = (byte)Mathf.FloorToInt(drainAccumulator);
                    drainAccumulator -= drop; // Оставляем дробный остаток для точности

                    // Отнимаем прочность
                    if (clothing.glassesQuality <= drop)
                    {
                        clothing.glassesQuality = 0;
                    }
                    else
                    {
                        clothing.glassesQuality -= drop;
                    }

                    // ОТПРАВЛЯЕМ СИНХРОНИЗАЦИЮ
                    // Внимание: Если качество стало 0, именно ЭТОТ вызов активирует 
                    // наш Harmony-патч Patch_sendUpdateGlassesQuality, который выключит свет!
                    clothing.sendUpdateGlassesQuality();
                }
            }
        }
    }
}
