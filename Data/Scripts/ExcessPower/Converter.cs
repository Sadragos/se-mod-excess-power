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
using Sandbox.Game.Entities.Cube;

namespace ExcessPower
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_ConveyorSorter), false, "ExcessPower")]
    public class Converter : MyGameLogicComponent
    {
        bool _init = false;
        public MyConveyorSorter Block;
        public Sandbox.ModAPI.IMyFunctionalBlock TerminalBlock;
        public GridLogic Logic;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
            Block = (MyConveyorSorter)Entity;
            TerminalBlock = (Sandbox.ModAPI.IMyFunctionalBlock)Entity;
            TerminalBlock.AppendingCustomInfo += AppendingCustomInfo;
            MyLog.Default.WriteLine("ExcessPower: DEBUG init");
        }


        public override void Close()
        {
            Logic = null;
            TerminalBlock.AppendingCustomInfo -= AppendingCustomInfo;
        }

        public void SetLogic(GridLogic logic)
        {
            Logic = logic;
        }

        void AppendingCustomInfo(Sandbox.ModAPI.IMyTerminalBlock block, StringBuilder stringBuilder)
        {
            try
            {
                stringBuilder
                    .Append("Converting Excess Power\n");

                if(!TerminalBlock.Enabled)
                {
                    stringBuilder.Append("- Block disabled");
                    return;
                }
                if (Logic.Solars.Count > 0)
                {
                    stringBuilder
                        .Append("- ")
                        .Append(Logic.Solars.Count)
                        .Append(" Solar Panels: ")
                        .Append(Logic.EffectiveSolarExcess.ToString("0.00"))
                        .Append(" MW Excess\n");
                }
                if (Logic.Winds.Count > 0)
                {
                    stringBuilder
                        .Append("- ")
                        .Append(Logic.Winds.Count)
                        .Append(" Wind Turbines: ")
                        .Append(Logic.EffectiveWindExcess.ToString("0.00"))
                        .Append(" MW Excess\n");
                }
                if (Logic.WorkingConverters > 1)
                {
                    stringBuilder
                        .Append("- ")
                        .Append((Logic.WorkingConverters - 1))
                        .Append(" other Converters\n");
                }
                stringBuilder
                    .Append("\n")
                    .Append(Logic.EffectiveExcess.ToString("0.00"))
                    .Append(" MW Excess Power ")
                    .Append("\n");
                float grams = Logic.EffectiveExcess * Core.Instance.ConfigHandler.Config.ItemPerMWs * 1000f;
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

        internal void Update(double timeDiff)
        {
            if (TerminalBlock.Enabled)
            {
                float amount = (float)(Core.Instance.ConfigHandler.Config.ItemPerMWs * Logic.EffectiveExcess * timeDiff);
                TerminalBlock.GetInventory().AddItems((VRage.MyFixedPoint)amount, Core.Instance.ConfigHandler.Config.MYOB);
                TerminalBlock.RefreshCustomInfo();
                RefreshControls(TerminalBlock);
            }
        }

        public static void RefreshControls(Sandbox.ModAPI.IMyFunctionalBlock block)
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
}