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

            // Проверяем, горит ли свет (байт состояния)
            bool isLightOn = player.clothing.glassesState != null && player.clothing.glassesState.Length > 0 && player.clothing.glassesState[0] != 0;

            if (isLightOn)
            {
                if (player.clothing.glassesQuality == 0)
                {
                    // --- ЯДЕРНЫЙ ВАРИАНТ ---
                    // Если свет всё еще горит при 0%, значит обычные пакеты не помогли.
                    // МЫ ПРИНУДИТЕЛЬНО СНИМАЕМ ФОНАРЬ С ИГРОКА.
                    // Метод askWearGlasses(0, ...) — это команда "надеть НИЧЕГО", то есть снять текущее.
                    
                    player.clothing.askWearGlasses(0, 0, new byte[0], true);
                    
                    // Сообщаем в чат, чтобы игрок не пугался (опционально)
                    ChatManager.say(player.channel.owner.playerID.steamID, "Ваш фонарь разрядился и перестал работать!", Color.red);
                    return;
                }

                // Логика разряда
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
