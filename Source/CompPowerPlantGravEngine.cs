using RimWorld;
using Verse;

namespace VanillaContinue
{
    public class CompProperties_PowerPlantGravEngine : CompProperties_Power
    {
        public CompProperties_PowerPlantGravEngine()
        {
            compClass = typeof(CompPowerPlantGravEngine);
        }
    }

    /// <summary>
    /// 逆重引擎发电：基础发电量 + 每单位燃料的额外发电量。
    /// 燃料不消耗（投资模型）：燃料上限与加注由设置与自动加注阈值控制。
    /// </summary>
    public class CompPowerPlantGravEngine : CompPowerPlant
    {
        private CompRefuelable refuelable;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            refuelable = parent.GetComp<CompRefuelable>();
            if (refuelable == null)
            {
                Log.Warning($"[VanillaContinue] {parent.Label} 缺少燃料组件，仅输出基础发电量。");
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            var settings = VanillaContinueMod.Instance?.Settings;
            if (settings == null)
            {
                return;
            }
            float fuel = refuelable?.Fuel ?? 0f;
            PowerOutput = settings.BasePower + fuel * settings.PowerPerFuelUnit;
        }
    }
}
