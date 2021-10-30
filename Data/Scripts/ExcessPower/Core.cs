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
using VRage.Collections;
using Sandbox.ModAPI.Interfaces.Terminal;

//using Sandbox.ModAPI.Ingame;

namespace ExcessPower
{
    [MySessionComponentDescriptor( MyUpdateOrder.BeforeSimulation )]
    public class Core : MySessionComponentBase
    {
        // Declarations
        public static Core Instance;
        public MyConfigHandler ConfigHandler;

        public const int LogicUpdateInterval = 60 * 2;
        private int LogicUpdateIndex;
        private readonly List<GridLogic> GridLogic = new List<GridLogic>();
        private readonly Dictionary<long, GridLogic> GridLogicLookup = new Dictionary<long, GridLogic>();
        private readonly List<int> RemoveLogicIndex = new List<int>();
        private readonly MyConcurrentPool<GridLogic> LogicPool = new MyConcurrentPool<GridLogic>();

        public override void LoadData()
        {
            Instance = this;
            MyAPIGateway.Entities.OnEntityAdd += EntityAdded;
            ConfigHandler = new MyConfigHandler();
            ConfigHandler.Load();
            MyAPIGateway.TerminalControls.CustomControlGetter += GetCustomControlls;
            MyAPIGateway.TerminalControls.CustomActionGetter += GetCustomActions;
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Entities.OnEntityAdd -= EntityAdded;
            MyAPIGateway.TerminalControls.CustomControlGetter -= GetCustomControlls;
            MyAPIGateway.TerminalControls.CustomActionGetter -= GetCustomActions;
            Instance = null;
        }

        private void GetCustomActions(Sandbox.ModAPI.IMyTerminalBlock block, List<IMyTerminalAction> actions)
        {
            if (block.BlockDefinition.SubtypeId.Equals("ExcessPower"))
            {
                actions.RemoveRange(actions.Count - 1, 1);
            }
        }

        private void GetCustomControlls(Sandbox.ModAPI.IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if(block.BlockDefinition.SubtypeId.Equals("ExcessPower"))
            {
                controls.RemoveRange(controls.Count - 8, 8);
            }
        }

        void EntityAdded(IMyEntity ent)
        {
            try
            {
                var grid = ent as MyCubeGrid;
                if (grid != null && grid.CreatePhysics)
                {
                    var logic = GridLogicLookup.GetValueOrDefault(grid.EntityId, null);
                    if (logic != null)
                    {
                        logic.Reset();
                        logic.Init(grid);
                    }
                    else
                    {
                        logic = LogicPool.Get();
                        logic.Init(grid);

                        GridLogic.Add(logic);
                        GridLogicLookup.Add(grid.EntityId, logic);
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("ExcessPower: ERROR" + e.Message);
            }
        }

        public override void UpdateBeforeSimulation()
        {
            try
            {
                int logicCount = GridLogic.Count;
                if (logicCount == 0)
                    return;

                // logic from MyDistributedUpdater
                LogicUpdateIndex = (LogicUpdateIndex + 1) % LogicUpdateInterval;

                for (int i = LogicUpdateIndex; i < logicCount; i += LogicUpdateInterval)
                {
                    GridLogic logic = GridLogic[i];
                    if (logic.Grid.MarkedForClose)
                    {
                        RemoveLogicIndex.Add(i);
                        continue;
                    }
                    logic.Update();
                }

                if (RemoveLogicIndex.Count > 0)
                {
                    try
                    {
                        // sort ascending + iterate in reverse is required to avoid shifting indexes as we're removing.
                        RemoveLogicIndex.Sort();

                        for (int i = (RemoveLogicIndex.Count - 1); i >= 0; i--)
                        {
                            int index = RemoveLogicIndex[i];
                            GridLogic logic = GridLogic[index];

                            GridLogic.RemoveAtFast(index);
                            GridLogicLookup.Remove(logic.Grid.EntityId);

                            logic.Reset();
                            LogicPool.Return(logic);
                        }
                    }
                    finally
                    {
                        RemoveLogicIndex.Clear();
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("ExcessPower: ERROR" + e.Message);
            }
        }
    }
}