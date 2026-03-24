using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using HarmonyLib;
using System.Reflection;
using Rocket.Core.Logging;

namespace HeadLamp
{
    public class HeadLamp : RocketPlugin<HeadLampConfiguration>
    {
        public static HeadLamp Instance;
        private Harmony harmony;
        private const string HarmonyId = "com.headlamp.patcher";

        protected override void Load()
        {
            Instance = this;

            // 1. Инициализируем Harmony и применяем все патчи из Patcher.cs
            try 
            {
                harmony = new Harmony(HarmonyId);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Logger.Log("HeadLamp: Harmony patches applied successfully.");
            }
            catch (System.Exception ex)
            {
                Logger.LogException(ex, "HeadLamp: Failed to apply Harmony patches!");
            }

            // 2. Подписываемся на события
            U.Events.OnPlayerConnected += OnPlayerConnected;

            Logger.Log("HeadLamp Plugin loaded! (Battery + Harmony Protection)");
        }

        protected override void Unload()
        {
            // 3. Отписываемся от событий
            U.Events.OnPlayerConnected -= OnPlayerConnected;

            // 4. Снимаем патчи при выгрузке (важно для горячей перезагрузки плагинов)
            if (harmony != null)
            {
                harmony.UnpatchAll(HarmonyId);
                harmony = null;
            }

            Logger.Log("HeadLamp Plugin unloaded!");
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            // Вешаем компонент логики на игрока
            if (player.Player.gameObject.GetComponent<PlayerLampComponent>() == null)
            {
                player.Player.gameObject.AddComponent<PlayerLampComponent>();
            }
        }
    }
}
