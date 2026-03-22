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

            bool hasHat = player.Player.clothing.hatAsset != null;
            bool hasGlasses = player.Player.clothing.glassesAsset != null;

            if (!hasHat && !hasGlasses)
            {
                Rocket.Unturned.Chat.UnturnedChat.Say(player, "На вас не надето устройство (Шапка или Очки)!", UnityEngine.Color.red);
                return;
            }

            var inventoryItems = player.Inventory.search(batteryId, false, true);
            if (inventoryItems.Count > 0)
            {
                // Удаляем 1 батарейку
                player.Inventory.removeItem(inventoryItems[0].page, player.Inventory.getIndex(inventoryItems[0].page, inventoryItems[0].jar.x, inventoryItems[0].jar.y));

                // Чиним все надетые предметы, которые могут быть фонарем/ПНВ
                if (hasHat) player.Player.clothing.hatQuality = 100;
                if (hasGlasses) player.Player.clothing.glassesQuality = 100;

                Rocket.Unturned.Chat.UnturnedChat.Say(player, "Устройство успешно заряжено!", UnityEngine.Color.green);
            }
            else
            {
                Rocket.Unturned.Chat.UnturnedChat.Say(player, "У вас нет батарейки!", UnityEngine.Color.red);
            }
        }
    }
}
