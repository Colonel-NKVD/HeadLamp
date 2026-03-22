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

            // Подхватываем игроков, которые УЖЕ на сервере (при /rocket reload)
            foreach (var steamPlayer in Provider.clients)
            {
                UnturnedPlayer uPlayer = UnturnedPlayer.FromSteamPlayer(steamPlayer);
                if (uPlayer.GameObject.GetComponent<PlayerLampComponent>() == null)
                {
                    uPlayer.GameObject.AddComponent<PlayerLampComponent>();
                }
            }
            
            Rocket.Core.Logging.Logger.Log("HeadLamp Plugin Loaded successfully!");
        }

        protected override void Unload()
        {
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            harmony.UnpatchAll("com.headlamp.patch");

            // Зачищаем компоненты при выгрузке плагина
            foreach (var steamPlayer in Provider.clients)
            {
                UnturnedPlayer uPlayer = UnturnedPlayer.FromSteamPlayer(steamPlayer);
                var comp = uPlayer.GameObject.GetComponent<PlayerLampComponent>();
                if (comp != null)
                {
                    UnityEngine.Object.Destroy(comp);
                }
            }
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            player.GameObject.AddComponent<PlayerLampComponent>();
        }
    }
}
