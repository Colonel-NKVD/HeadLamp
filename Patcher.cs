using HarmonyLib;
using SDG.Unturned;
using System.Linq;

namespace HeadLamp
{
    // Патчим метод, который ты нашел. 
    // Внимание: проверь в dnSpy, находится ли он в классе PlayerClothing или PlayerAnimate
    // Скорее всего, это PlayerClothing.
    [HarmonyPatch(typeof(PlayerClothing), "onGlassesUpdated")]
    public class Patch_onGlassesUpdated
    {
        [HarmonyPrefix]
        public static void Prefix(PlayerClothing __instance, ushort id, byte quality, ref byte[] state)
        {
            // Если id не 0 (очки надеты) и прочность 0
            if (id != 0 && quality <= 0)
            {
                // Проверяем, есть ли предмет в нашем конфиге
                var config = HeadLamp.Instance.Configuration.Instance.Lamps.FirstOrDefault(x => x.ItemID == id);
                if (config != null)
                {
                    // state[0] отвечает за включение (1 - вкл, 0 - выкл)
                    // Если кто-то пытается передать включенное состояние (1), принудительно ставим 0
                    if (state != null && state.Length > 0 && state[0] != 0)
                    {
                        state[0] = 0; 
                    }
                }
            }
        }
    }
}
