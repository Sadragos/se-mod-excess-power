using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;

using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRage.Game.Entity;
using VRage.Voxels;
using SpaceEngineers.Game.ModAPI;
using ExcessPower;

namespace ExcessPower
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CargoContainer), true, "ExcessPower")]
    public class Compost : MyGameLogicComponent
    {
        bool _init = false;
        Sandbox.ModAPI.IMyTerminalBlock TerminalBlock;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();

            if (!_init)
            {
                TerminalBlock = Entity as Sandbox.ModAPI.IMyTerminalBlock;
                TerminalBlock.AppendingCustomInfo += AppendingCustomInfo;
                _init = true;
            }
            if (TerminalBlock.IsFunctional)
            {
                ConverterGrid gridData = ConverterDatastore.Get(TerminalBlock);
                gridData.Refresh();
                float amount = Config.Instance.ItemPerMWs * gridData.EffectiveExcess * 1.66f;
                TerminalBlock.GetInventory().AddItems((VRage.MyFixedPoint)amount, Config.Instance.MYOB);
                TerminalBlock.RefreshCustomInfo();
                RefreshControls(TerminalBlock);
            }
        }

        void AppendingCustomInfo(Sandbox.ModAPI.IMyTerminalBlock block, StringBuilder stringBuilder)
        {
            try
            {
                ConverterGrid gridData = ConverterDatastore.Get(block);

                stringBuilder
                    .Append("Converting Excess Power\n");
                if (gridData.Solar.Count > 0)
                {
                    stringBuilder
                        .Append("- ")
                        .Append(gridData.Solar.Count)
                        .Append(" Solar Panels: ")
                        .Append(gridData.EffectiveSolarExcess.ToString("0.00"))
                        .Append(" MW Excess\n");
                }
                if (gridData.Wind.Count > 0)
                {
                    stringBuilder
                        .Append("- ")
                        .Append(gridData.Wind.Count)
                        .Append(" Wind Turbines: ")
                        .Append(gridData.EffectiveWindExcess.ToString("0.00"))
                        .Append(" MW Excess\n");
                }
                if (gridData.Converters > 1)
                {
                    stringBuilder
                        .Append("- ")
                        .Append((gridData.Converters - 1))
                        .Append(" other Converters\n");
                }
                stringBuilder
                    .Append("\n")
                    .Append(gridData.EffectiveExcess.ToString("0.00"))
                    .Append(" MW Excess Power ")
                    .Append("\n");
                float grams = gridData.EffectiveExcess * Config.Instance.ItemPerMWs * 1000f;
                if(grams >= 1)
                    stringBuilder
                        .Append((grams).ToString("0.00"))
                        .Append(" g/s");
                else
                    stringBuilder
                        .Append((grams*1000f).ToString("0.00"))
                        .Append(" mg/s");
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("ExcessPower: ERROR " + e);
            }
        }

        public static void RefreshControls(Sandbox.ModAPI.IMyTerminalBlock block)
        {

            if (MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel)
            {
                var myCubeBlock = block as MyCubeBlock;

                if (myCubeBlock.IDModule != null)
                {

                    var share = myCubeBlock.IDModule.ShareMode;
                    var owner = myCubeBlock.IDModule.Owner;
                    myCubeBlock.ChangeOwner(owner, share == MyOwnershipShareModeEnum.None ? MyOwnershipShareModeEnum.All : MyOwnershipShareModeEnum.None);
                    myCubeBlock.ChangeOwner(owner, share);
                }
            }
        }
    }

    public class ConverterDatastore
    {
        public static Dictionary<long, ConverterGrid> Grids = new Dictionary<long, ConverterGrid>();

        public static ConverterGrid Get(Sandbox.ModAPI.IMyTerminalBlock block)
        {
            IMyCubeGrid grid = block.CubeGrid;
            if (!Grids.ContainsKey(grid.EntityId))
            {
                Grids.Add(grid.EntityId, new ConverterGrid()
                {
                    Grid = grid
                });
            }
            return Grids[grid.EntityId];
        }
    }

    public class ConverterGrid
    {
        List<IMySlimBlock> blocks = new List<IMySlimBlock>();
        public IMyCubeGrid Grid;
        public List<Sandbox.ModAPI.IMyPowerProducer> Solar = new List<Sandbox.ModAPI.IMyPowerProducer>();
        public List<Sandbox.ModAPI.IMyPowerProducer> Wind = new List<Sandbox.ModAPI.IMyPowerProducer>();
        public int Converters = 0;
        private int ScanCountdown = 0;
        public float SolarExcess = 0f;
        public float WindExcess = 0f;

        public float TotalExcess => SolarExcess + WindExcess;
        public float EffectiveExcess => TotalExcess / Math.Max(1, Converters);

        public float EffectiveSolarExcess => SolarExcess / Math.Max(1, Converters);
        public float EffectiveWindExcess => WindExcess / Math.Max(1, Converters);



        public void Refresh()
        {
            if (--ScanCountdown <= 0)
            {
                ScanCountdown = 5;

                blocks.Clear();
                Solar.Clear();
                Wind.Clear();
                Converters = 0;

                Grid.GetBlocks(blocks, b => b.FatBlock != null);

                foreach (IMySlimBlock block in blocks)
                {
                    if (block.FatBlock is Sandbox.ModAPI.IMyCargoContainer && block.FatBlock.BlockDefinition.SubtypeId.Equals("ExcessPower"))
                        Converters++;
                    else if (block.FatBlock is Sandbox.ModAPI.IMyPowerProducer)
                    {
                        Sandbox.ModAPI.IMyPowerProducer prod = (block.FatBlock as Sandbox.ModAPI.IMyPowerProducer);
                        if (prod != null && prod.BlockDefinition.SubtypeId.Contains("Solar"))
                            Solar.Add(prod);
                        else if (prod != null && prod.BlockDefinition.SubtypeId.Contains("Wind"))
                            Wind.Add(prod);
                    }
                }
            }

            SolarExcess = 0;
            WindExcess = 0;
            foreach (Sandbox.ModAPI.IMyPowerProducer p in Solar)
                SolarExcess += Math.Max(0, p.MaxOutput - p.CurrentOutput);

            foreach (Sandbox.ModAPI.IMyPowerProducer p in Wind)
                WindExcess += Math.Max(0, p.MaxOutput - p.CurrentOutput);

        }
    }
}