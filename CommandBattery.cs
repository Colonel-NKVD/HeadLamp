using Rocket.API;
using Rocket.Unturned.Player;
using System.Collections.Generic;
using SDG.Unturned;

namespace HeadLamp
{
    public class CommandBattery : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "battery";
        public string Help => "Зарядить налобный фонарь/ПНВ";
        public string Syntax => "";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "headlamp.battery" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer player = (UnturnedPlayer)caller;
            ushort batteryId = HeadLamp.Instance.Configuration.Instance.BatteryItemID;

            if (player.Player.clothing.headAsset == null)
            {
                Rocket.Unturned.Chat.UnturnedChat.Say(player, "На вас не надето устройство!", UnityEngine.Color.red);
                return;
            }

            // Ищем батарейку в инвентаре
            var inventoryItems = player.Inventory.search(batteryId, false, true);
            if (inventoryItems.Count > 0)
            {
                // Удаляем 1 батарейку
                player.Inventory.removeItem(inventoryItems[0].page, player.Inventory.getIndex(inventoryItems[0].page, inventoryItems[0].jar.x, inventoryItems[0].jar.y));

                // Чиним вещь
                player.Player.clothing.headQuality = 100;
                player.Player.clothing.sendUpdateCareful();

                Rocket.Unturned.Chat.UnturnedChat.Say(player, "Устройство успешно заряжено!", UnityEngine.Color.green);
            }
            else
            {
                Rocket.Unturned.Chat.UnturnedChat.Say(player, "У вас нет батарейки!", UnityEngine.Color.red);
            }
        }
    }
}
