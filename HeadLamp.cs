using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;

namespace HeadLamp
{
    public class HeadLamp : RocketPlugin<HeadLampConfiguration>
    {
        // Синглтон для доступа к конфигурации из нашего компонента
        public static HeadLamp Instance;

        protected override void Load()
        {
            Instance = this;

            // Подписываемся на событие входа игрока
            U.Events.OnPlayerConnected += OnPlayerConnected;

            Rocket.Core.Logging.Logger.Log("HeadLamp Plugin loaded successfully! (Clean API Version)");
        }

        protected override void Unload()
        {
            // Отписываемся от событий при выгрузке, чтобы не было утечек памяти
            U.Events.OnPlayerConnected -= OnPlayerConnected;

            Rocket.Core.Logging.Logger.Log("HeadLamp Plugin unloaded!");
        }

        private void OnPlayerConnected(UnturnedPlayer player)
        {
            // Вешаем наш скрипт логики батарейки на игрока, если его там еще нет
            if (player.Player.gameObject.GetComponent<PlayerLampComponent>() == null)
            {
                player.Player.gameObject.AddComponent<PlayerLampComponent>();
            }
        }
    }
}
