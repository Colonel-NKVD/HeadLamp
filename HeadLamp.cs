using Rocket.Core.Plugins;
using Rocket.Unturned;
using HarmonyLib;
using Rocket.Unturned.Player;
using SDG.Unturned;

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

            // Подхватываем игроков, которые УЖЕ на сервере
            foreach (var steamPlayer in Provider.clients)
            {
                UnturnedPlayer uPlayer = UnturnedPlayer.FromSteamPlayer(steamPlayer);
                if (uPlayer.Player.gameObject.GetComponent<PlayerLampComponent>() == null)
                {
                    uPlayer.Player.gameObject.AddComponent<PlayerLampComponent>();
                }
            }
            
            Rocket.Core.Logging.Logger.Log("HeadLamp Plugin Loaded successfully!");
        }

        protected override void Unload()
        {
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            harmony.UnpatchAll("com.headlamp.patch");

            foreach (var steamPlayer in Provider.clients)
            {
                UnturnedPlayer uPlayer = UnturnedPlayer.FromSteamPlayer(steamPlayer);
                var comp = uPlayer.Player.gameObject.GetComponent<PlayerLampComponent>();
                if (comp != null)
                {
                    UnityEngine.Object.Destroy(comp);
                }
            }
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            if (player.Player.gameObject.GetComponent<PlayerLampComponent>() == null)
            {
                player.Player.gameObject.AddComponent<PlayerLampComponent>();
            }
        }
    }
}
