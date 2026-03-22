using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace HeadLamp
{
    public class LampSettings
    {
        [XmlAttribute] public ushort ItemID;
        [XmlAttribute] public float DrainPerSecond; // Сколько % прочности тратится в сек.

        public LampSettings() { }
        public LampSettings(ushort id, float drain)
        {
            ItemID = id;
            DrainPerSecond = drain;
        }
    }

    public class HeadLampConfiguration : IRocketPluginConfiguration
    {
        public ushort BatteryItemID; // ID предмета-батарейки (по дефолту 337)
        public List<LampSettings> Lamps;

        public void LoadDefaults()
        {
            BatteryItemID = 337;
            Lamps = new List<LampSettings>
            {
                new LampSettings(1199, 0.5f), // Headlamp
                new LampSettings(334, 0.2f)   // NVG
            };
        }
    }
}
