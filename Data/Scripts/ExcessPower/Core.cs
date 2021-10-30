using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Timers;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using ExcessPower;

//using Sandbox.ModAPI.Ingame;

namespace ExcessPower
{
    [MySessionComponentDescriptor(
        MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation | MyUpdateOrder.Simulation )]
    public class Core : MySessionComponentBase
    {
        // Declarations
        private static readonly string version = "v1.0";

        private static bool _initialized;

        private static int interval = 0;

        override public void SaveData()
        {
            Config.Write();
        }

        // Initializers
        private void Initialize( )
        {
            if (!MyAPIGateway.Multiplayer.IsServer) return;
            Config.Load();
        }
        

        public void  UpdateBeforeEverySecond()
        {
            // TODO more logic maybe?
        }

        // Overrides
        public override void UpdateBeforeSimulation( )
        {
            try
            {
                if (MyAPIGateway.Session == null)
                    return;

                // Run the init
                if (!_initialized)
                {
                    _initialized = true;
                    Initialize();
                    return;
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine(string.Format("UpdateBeforeSimulation(): {0}", ex));
            }
        }

        

        public override void UpdateAfterSimulation( )
        {
            // TODO more logic maybe?
        }

        protected override void UnloadData( )
        {
            base.UnloadData( );
        }
    }
}