using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Utils;

namespace ExcessPower
{
    public class GridLogic
    {
        public MyCubeGrid Grid;

        public readonly HashSet<Converter> Converters = new HashSet<Converter>();
        public readonly HashSet<IMyPowerProducer> Solars = new HashSet<IMyPowerProducer>();
        public readonly HashSet<IMyPowerProducer> Winds = new HashSet<IMyPowerProducer>();

        public float SolarExcess = 0;
        public float WindExcess = 0;
        public float WorkingConverters = 0;

        public float TotalExcess => SolarExcess + WindExcess;
        public float EffectiveExcess => TotalExcess / Math.Max(1, WorkingConverters);

        public float EffectiveSolarExcess => SolarExcess / Math.Max(1, WorkingConverters);
        public float EffectiveWindExcess => WindExcess / Math.Max(1, WorkingConverters);

        public DateTime LastUpdate = DateTime.Now;

        public void Init(MyCubeGrid grid)
        {
            try
            {
                if (grid == null)
                    throw new Exception("given grid was null!");

                Grid = grid;

                // NOTE: not all blocks are fatblocks, but the kind of blocks we need are always fatblocks.
                foreach (var block in Grid.GetFatBlocks())
                {
                    BlockAdded(block);
                }

                Grid.OnFatBlockAdded += BlockAdded;
                Grid.OnFatBlockRemoved += BlockRemoved;

                Update();
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("ExcessPower: ERROR" + e.Message);
            }
        }

        public void Update()
        {
            try
            {
                if (Grid == null || Grid.IsPreview || Grid.Physics == null || !Grid.Physics.Enabled)
                    return;

                SolarExcess = 0;
                WindExcess = 0;
                WorkingConverters = 0;

                if (Converters.Count == 0)
                    return; // no converters, skip.

                foreach (IMyPowerProducer p in Solars)
                    SolarExcess += Math.Max(0, p.MaxOutput - p.CurrentOutput);

                foreach (IMyPowerProducer p in Winds)
                    WindExcess += Math.Max(0, p.MaxOutput - p.CurrentOutput);

                foreach (Converter conv in Converters)
                    if (conv.TerminalBlock.Enabled)
                        WorkingConverters++;

                double TimeDiff = (DateTime.Now - LastUpdate).TotalSeconds;
                foreach (Converter conv in Converters)
                    conv.Update(TimeDiff);

                LastUpdate = DateTime.Now;

            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("ExcessPower: ERROR" + e.Message);
            }
        }

        public void Reset()
        {
            try
            {
                if (Grid != null)
                {
                    Grid.OnFatBlockAdded -= BlockAdded;
                    Grid.OnFatBlockRemoved -= BlockRemoved;
                    Grid = null;
                }

                Converters.Clear();
                Winds.Clear();
                Solars.Clear();
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("ExcessPower: ERROR" + e.Message);
            }
        }

        private void BlockRemoved(MyCubeBlock block)
        {
            try
            {
                if (block is MyConveyorSorter)
                {
                    Converter conv = block.GameLogic.GetAs<Converter>();
                    if(conv != null)
                        Converters.Remove(conv);
                    return;
                }
                if (block is IMyPowerProducer)
                {
                    IMyPowerProducer prod = (block as IMyPowerProducer);
                    if (prod is IMySolarPanel)
                        Solars.Remove(prod);
                    else if (prod.BlockDefinition.TypeId == Core.Instance.TurbineType)
                        Winds.Remove(prod);
                    return;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("ExcessPower: ERROR" + e.Message);
            }
        }

        private void BlockAdded(MyCubeBlock block)
        {
            try
            {
                if (block is MyConveyorSorter)
                {
                    Converter conv = block?.GameLogic?.GetAs<Converter>();
                    if (conv != null)
                    {
                        conv.SetLogic(this);
                        Converters.Add(conv);
                    }
                        
                    return;
                }
                if (block is IMyPowerProducer)
                {
                    IMyPowerProducer prod = (block as IMyPowerProducer);
                    if (prod is IMySolarPanel)
                        Solars.Add(prod);
                    else if (prod.BlockDefinition.SubtypeId.Contains("Wind"))
                        Winds.Add(prod);
                    return;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("ExcessPower: ERROR" + e.Message);
            }
        }
    }
}
