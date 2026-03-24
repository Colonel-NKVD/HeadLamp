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

        void Awake() => player = GetComponent<Player>();

        void Update()
        {
            if (Time.time - lastTick < 0.5f) return;
            lastTick = Time.time;

            if (player.clothing.glassesAsset == null) return;

            var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == player.clothing.glassesAsset.id);
            if (config == null) return;

            bool isLightOn = player.clothing.glassesState != null && player.clothing.glassesState.Length > 0 && player.clothing.glassesState[0] != 0;

            if (isLightOn)
            {
                if (player.clothing.glassesQuality == 0)
                {
                    // Искры
                    EffectManager.sendEffect(61, 16, player.transform.position + Vector3.up * 1.8f);

                    // Сохраняем данные
                    ushort id = player.clothing.glassesAsset.id;
                    byte[] state = player.clothing.glassesState;
                    if (state != null && state.Length > 0) state[0] = 0;

                    // ФОКУС ПРОТИВ ДЮПА (Рефлексия для обнуления слота перед надеванием)
                    var glassesField = typeof(PlayerClothing).GetField("_glasses", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (glassesField != null) glassesField.SetValue(player.clothing, (ushort)0);

                    // Переодеваем "на пустую голову"
                    player.clothing.askWearGlasses(id, 0, state, true);
                    return;
                }

                drainAccumulator += (config.DrainPerSecond * 0.5f);
                if (drainAccumulator >= 1f)
                {
                    byte drop = (byte)Mathf.FloorToInt(drainAccumulator);
                    drainAccumulator -= drop;
                    if (player.clothing.glassesQuality <= drop) player.clothing.glassesQuality = 0;
                    else player.clothing.glassesQuality -= drop;
                    player.clothing.sendUpdateGlassesQuality();
                }
            }
        }
    }
}
