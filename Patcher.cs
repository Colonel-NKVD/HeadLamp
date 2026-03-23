using HarmonyLib;
using SDG.Unturned;
using System.Linq;

namespace HeadLamp
{
    public class Patch_onGlassesUpdated
    {
        // Убираем [HarmonyPatch] - мы его вызвали вручную в HeadLamp.cs
        public static bool Prefix(PlayerClothing __instance, ushort id, byte quality, byte[] state)
        {
            // Если прочность 0 и это предмет из конфига
            if (id != 0 && quality <= 0)
            {
                var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == id);
                if (config != null)
                {
                    // Если пытаются включить (state[0] != 0), принудительно гасим в 0
                    if (state != null && state.Length > 0 && state[0] != 0)
                    {
                        state[0] = 0;
                    }
                }
            }
            return true; // Разрешаем выполнение оригинального метода с нашими правками в state
        }
    }
}
