using HarmonyLib;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Reflection;
using UnityEngine;
using System.Linq;

namespace HeadLamp
{
    public class HeadLamp : RocketPlugin<HeadLampConfiguration>
    {
        public static HeadLamp Instance;
        private Harmony harmony;
        private const string HarmonyId = "com.plugin.headlamp";

        protected override void Load()
        {
            Instance = this;

            // 1. Инициализация Harmony вручную через Reflection
            try
            {
                harmony = new Harmony(HarmonyId);
                
                // Ищем приватный метод в PlayerClothing
                var original = typeof(PlayerClothing).GetMethod("onGlassesUpdated", 
                    BindingFlags.Instance | BindingFlags.NonPublic);
                
                // Ищем наш префикс в классе Patch_onGlassesUpdated
                var prefix = typeof(Patch_onGlassesUpdated).GetMethod("Prefix", 
                    BindingFlags.Static | BindingFlags.Public);

                if (original != null && prefix != null)
                {
                    harmony.Patch(original, new HarmonyMethod(prefix));
                    Rocket.Core.Logging.Logger.Log("HeadLamp: Harmony patch applied successfully!");
                }
                else
                {
                    Rocket.Core.Logging.Logger.LogError("HeadLamp: Critical error - method 'onGlassesUpdated' not found!");
                }
            }
            catch (System.Exception ex)
            {
                Rocket.Core.Logging.Logger.LogError("HeadLamp: Failed to apply Harmony patches: " + ex.Message);
            }

            // 2. Подписка на события игрока
            U.Events.OnPlayerConnected += OnPlayerConnected;

            Rocket.Core.Logging.Logger.Log("HeadLamp Plugin loaded!");
        }

        protected override void Unload()
        {
            if (harmony != null)
            {
                harmony.UnpatchAll(HarmonyId);
            }

            U.Events.OnPlayerConnected -= OnPlayerConnected;
            
            Rocket.Core.Logging.Logger.Log("HeadLamp Plugin unloaded.");
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            // У UnturnedPlayer объект находится в player.Player.gameObject
            if (player.Player.gameObject.GetComponent<PlayerLampComponent>() == null)
            {
                player.Player.gameObject.AddComponent<PlayerLampComponent>();
            }
        }
    }
}
