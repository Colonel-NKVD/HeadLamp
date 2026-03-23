using HarmonyLib;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Reflection;
using UnityEngine;

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

            // 1. Инициализация Harmony и ручной патч
            try
            {
                harmony = new Harmony(HarmonyId);
                
                // Ищем тот самый приватный метод в PlayerClothing
                var original = typeof(PlayerClothing).GetMethod("onGlassesUpdated", 
                    BindingFlags.Instance | BindingFlags.NonPublic);
                
                // Ищем наш префикс в классе Patch_onGlassesUpdated
                var prefix = typeof(Patch_onGlassesUpdated).GetMethod("Prefix", 
                    BindingFlags.Static | BindingFlags.Public);

                if (original != null && prefix != null)
                {
                    harmony.Patch(original, new HarmonyMethod(prefix));
                    Logger.Log("HeadLamp: Harmony patch 'onGlassesUpdated' applied successfully!", Color.green);
                }
                else
                {
                    Logger.LogError("HeadLamp: Critical error - method 'onGlassesUpdated' or Prefix not found!");
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogError("HeadLamp: Failed to apply Harmony patches: " + ex.Message);
            }

            // 2. Подписка на события игрока
            U.Events.OnPlayerConnected += OnPlayerConnected;

            Logger.Log("HeadLamp Plugin by Gemini & User loaded!");
        }

        protected override void Unload()
        {
            // Убираем патчи при выключении плагина
            if (harmony != null)
            {
                harmony.UnpatchAll(HarmonyId);
            }

            U.Events.OnPlayerConnected -= OnPlayerConnected;
            
            Logger.Log("HeadLamp Plugin unloaded.");
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            // Добавляем компонент, который будет отвечать за разрядку
            if (player.GameObject.GetComponent<PlayerLampComponent>() == null)
            {
                player.GameObject.AddComponent<PlayerLampComponent>();
            }
        }
    }
}
