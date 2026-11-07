using RimWorld;
using UnityEngine;
using Verse;

namespace VanillaContinue
{
    /// <summary>
    /// 设置与入口：基础发电量、每单位燃料发电量与燃料上限全部即时生效
    /// （上限通过运行时改写 Def 的 fuelCapacity 实现，无需重启游戏）。
    /// </summary>
    public class VanillaContinueMod : Mod
    {
        public static VanillaContinueMod Instance { get; private set; }
        public VanillaContinueSettings Settings { get; private set; }

        public VanillaContinueMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<VanillaContinueSettings>();
        }

        public override string SettingsCategory()
        {
            return "原版-continue（会发电的逆重引擎）";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label($"基础发电量: {Settings.BasePower:F0} W");
            Settings.BasePower = listing.Slider(Settings.BasePower, 0f, 5000f);

            listing.Label($"每单位逆重板发电量: {Settings.PowerPerFuelUnit:F1} W");
            Settings.PowerPerFuelUnit = listing.Slider(Settings.PowerPerFuelUnit, 1f, 100f);

            listing.Label($"燃料（逆重板）上限: {Settings.MaxFuelCapacity:F0}");
            listing.Label("  修改即时生效，无需重启游戏；已装填超出上限的燃料仍按实际上限计算发电量。");
            float newCap = listing.Slider(Settings.MaxFuelCapacity, 100f, 20000f);
            if (!Mathf.Approximately(newCap, Settings.MaxFuelCapacity))
            {
                Settings.MaxFuelCapacity = Mathf.Round(newCap);
                GravEnginePatcher.ApplyFuelCapacity(Settings.MaxFuelCapacity);
            }

            if (listing.ButtonText("恢复默认值"))
            {
                Settings.BasePower = 1000f;
                Settings.PowerPerFuelUnit = 20f;
                Settings.MaxFuelCapacity = 10000f;
                GravEnginePatcher.ApplyFuelCapacity(Settings.MaxFuelCapacity);
            }

            listing.End();
        }
    }

    public class VanillaContinueSettings : ModSettings
    {
        public float BasePower = 1000f;
        public float PowerPerFuelUnit = 20f;
        public float MaxFuelCapacity = 10000f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref BasePower, "basePower", 1000f);
            Scribe_Values.Look(ref PowerPerFuelUnit, "powerPerFuelUnit", 20f);
            Scribe_Values.Look(ref MaxFuelCapacity, "maxFuelCapacity", 10000f);
        }
    }

    /// <summary>启动时把设置的燃料上限写入逆重引擎的燃料组件定义。</summary>
    [StaticConstructorOnStartup]
    public static class GravEnginePatcher
    {
        static GravEnginePatcher()
        {
            ApplyFuelCapacity(VanillaContinueMod.Instance?.Settings?.MaxFuelCapacity ?? 10000f);
        }

        public static void ApplyFuelCapacity(float capacity)
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("GravEngine");
            var refuelable = def?.comps?.Find(c => c is CompProperties_Refuelable) as CompProperties_Refuelable;
            if (refuelable != null)
            {
                refuelable.fuelCapacity = capacity;
                refuelable.initialConfigurableTargetFuelLevel = capacity;
            }
        }
    }
}
