using System;
using System.IO;
using System.Text;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Game;
using VRage.Utils;
using ExcessPower;

namespace ExcessPower
{

    public class MyConfigHandler
    {

        public MyConfig Config;

        public void Load()
        {
            // Load config xml
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage("ExcessPower.xml", typeof(MyConfig)))
            {
                try
                {
                    TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("ExcessPower.xml", typeof(MyConfig));
                    var xmlData = reader.ReadToEnd();
                    Config = MyAPIGateway.Utilities.SerializeFromXML<MyConfig>(xmlData);
                    reader.Dispose();
                    MyLog.Default.WriteLine("ExcessPower: found and loaded");
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLine("ExcessPower: loading failed, generating new Config");
                }
            }

            if (Config == null)
            {
                MyLog.Default.WriteLine("ExcessPower: No Config found, creating New");
                // Create default values
                Config = new MyConfig();
                Write();
            }

            Config.MYOB = GetItemBuilder(Config.ItemId);
            if (Config.MYOB == null) ;
                MyLog.Default.WriteLine("ExcessPower: ERROR - unknown ItemId");
        }

        public static MyObjectBuilder_PhysicalObject GetItemBuilder(string itemId)
        {
            string[] parts = itemId.Split('/');
            if (parts.Length <= 1) return null;

            switch (parts[0])
            {
                case "MyObjectBuilder_Component":
                    return new MyObjectBuilder_Component() { SubtypeName = parts[1] };
                case "MyObjectBuilder_AmmoMagazine":
                    return new MyObjectBuilder_AmmoMagazine() { SubtypeName = parts[1] };
                case "MyObjectBuilder_Ingot":
                    return new MyObjectBuilder_Ingot() { SubtypeName = parts[1] };
                case "MyObjectBuilder_Ore":
                    return new MyObjectBuilder_Ore() { SubtypeName = parts[1] };
                case "MyObjectBuilder_PhysicalObject":
                    return new MyObjectBuilder_PhysicalObject() { SubtypeName = parts[1] };
                case "MyObjectBuilder_ConsumableItem":
                    return new MyObjectBuilder_ConsumableItem() { SubtypeName = parts[1] };
                default: return null;
            }
        }


        public void Write()
        {
            if (Config == null) return;

            try
            {
                MyLog.Default.WriteLine("ExcessPower: Serializing to XML... ");
                string xml = MyAPIGateway.Utilities.SerializeToXML<MyConfig>(Config);
                MyLog.Default.WriteLine("ExcessPower: Writing to disk... ");
                TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("ExcessPower.xml", typeof(MyConfig));
                writer.Write(xml);
                writer.Flush();
                writer.Close();
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("ExcessPower: Error saving XML!" + e.StackTrace);
            }
        }
    }
}