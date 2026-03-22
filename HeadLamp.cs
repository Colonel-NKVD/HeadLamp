using Rocket.Core.Plugins;
using Rocket.Unturned;
using HarmonyLib;
using Rocket.Unturned.Player;
using System;

namespace HeadLamp
{
    public class HeadLamp : RocketPlugin<HeadLampConfiguration>
    {
        public static HeadLamp Instance;
        private Harmony harmony;

        protected override void Load()
        {
            Instance = this;
            harmony = new Harmony("com.headlamp.patch");
            harmony.PatchAll();

            U.Events.OnPlayerConnected += OnPlayerConnected;
            
            Rocket.Core.Logging.Logger.Log("HeadLamp Plugin Loaded!");
        }

        protected override void Unload()
        {
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            harmony.UnpatchAll("com.headlamp.patch");
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            // Добавляем компонент для отслеживания
            player.GameObject.AddComponent<PlayerLampComponent>();
        }
    }
}
