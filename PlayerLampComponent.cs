using SDG.Unturned;
using UnityEngine;
using System.Linq;

namespace HeadLamp
{
    public class PlayerLampComponent : MonoBehaviour
    {
        private Player player;
        private float lastTick;
        private float drainAccumulator = 0f;

        void Awake()
        {
            player = GetComponent<Player>();
        }

        void Update()
        {
            if (Time.time - lastTick < 0.5f) return;
            lastTick = Time.time;

            if (player.clothing.glassesAsset == null) return;

            var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == player.clothing.glassesAsset.id);
            if (config == null) return;

            // Горит ли свет (байт состояния)
            bool isLightOn = player.clothing.glassesState != null && player.clothing.glassesState.Length > 0 && player.clothing.glassesState[0] != 0;

            if (isLightOn)
            {
                // Если прочность упала в 0, но свет все еще горит
                if (player.clothing.glassesQuality == 0)
                {
                    // КОСТЫЛЬ 2: Агрессивное подавление.
                    // Меняем байт принудительно
                    player.clothing.glassesState[0] = 0;

                    // Спамим серверным методом выключения по ВСЕМ возможным индексам Unturned
                    // Клиент просто физически не сможет удержать свет включенным
                    player.clothing.ServerSetVisualToggleState((EVisualToggleType)0, false);
                    player.clothing.ServerSetVisualToggleState((EVisualToggleType)1, false);
                    player.clothing.ServerSetVisualToggleState((EVisualToggleType)2, false);
                    player.clothing.ServerSetVisualToggleState((EVisualToggleType)3, false);

                    // Синхронизируем иконку
                    player.clothing.sendUpdateGlassesQuality();
                    return; // Больше ничего не делаем
                }

                // --- Логика разряда (работает только если качество > 0) ---
                drainAccumulator += (config.DrainPerSecond * 0.5f);
                if (drainAccumulator >= 1f)
                {
                    byte drop = (byte)Mathf.FloorToInt(drainAccumulator);
                    drainAccumulator -= drop;

                    if (player.clothing.glassesQuality <= drop)
                        player.clothing.glassesQuality = 0;
                    else
                        player.clothing.glassesQuality -= drop;

                    player.clothing.sendUpdateGlassesQuality();
                }
            }
        }
    }
}
