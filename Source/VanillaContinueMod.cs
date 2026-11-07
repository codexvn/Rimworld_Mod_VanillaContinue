using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace VanillaContinue
{
    /// <summary>
    /// 设置与入口：基础发电量、每单位燃料发电量、燃料上限与燃料类型全部即时生效
    /// （上限与燃料类型通过运行时改写 Def 实现，无需重启游戏）。
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
            return "VanillaContinue.SettingsCategory".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("VanillaContinue.BasePower".Translate(Settings.BasePower.ToString("F0")));
            Settings.BasePower = listing.Slider(Settings.BasePower, 0f, 5000f);

            listing.Label("VanillaContinue.PowerPerFuelUnit".Translate(Settings.PowerPerFuelUnit.ToString("F1")));
            Settings.PowerPerFuelUnit = listing.Slider(Settings.PowerPerFuelUnit, 1f, 100f);

            listing.Label("VanillaContinue.MaxFuelCapacity".Translate(Settings.MaxFuelCapacity.ToString("F0")));
            listing.Label("VanillaContinue.MaxFuelCapacityNote".Translate());
            float newCap = listing.Slider(Settings.MaxFuelCapacity, 100f, 20000f);
            if (!Mathf.Approximately(newCap, Settings.MaxFuelCapacity))
            {
                Settings.MaxFuelCapacity = Mathf.Round(newCap);
                GravEnginePatcher.ApplyFuelSettings(Settings);
            }

            if (listing.ButtonText("VanillaContinue.FuelType".Translate(
                    Settings.UseGravlite
                        ? "VanillaContinue.FuelGravlite".Translate()
                        : "VanillaContinue.FuelPlasteel".Translate())))
            {
                Settings.UseGravlite = !Settings.UseGravlite;
                GravEnginePatcher.ApplyFuelSettings(Settings);
            }
            listing.Label("VanillaContinue.FuelTypeNote".Translate());

            if (listing.ButtonText("VanillaContinue.ResetDefaults".Translate()))
            {
                Settings.BasePower = 1000f;
                Settings.PowerPerFuelUnit = 20f;
                Settings.MaxFuelCapacity = 10000f;
                Settings.UseGravlite = false;
                GravEnginePatcher.ApplyFuelSettings(Settings);
            }

            listing.End();
        }
    }

    public class VanillaContinueSettings : ModSettings
    {
        public float BasePower = 1000f;
        public float PowerPerFuelUnit = 20f;
        public float MaxFuelCapacity = 10000f;

        /// <summary>燃料类型：false=玻璃钢（默认），true=逆重板。</summary>
        public bool UseGravlite = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref BasePower, "basePower", 1000f);
            Scribe_Values.Look(ref PowerPerFuelUnit, "powerPerFuelUnit", 20f);
            Scribe_Values.Look(ref MaxFuelCapacity, "maxFuelCapacity", 10000f);
            Scribe_Values.Look(ref UseGravlite, "useGravlite", false);
        }
    }

    /// <summary>启动与设置变更时，把燃料上限与燃料类型写入逆重引擎的燃料组件定义。</summary>
    [StaticConstructorOnStartup]
    public static class GravEnginePatcher
    {
        static GravEnginePatcher()
        {
            ApplyFuelSettings(VanillaContinueMod.Instance?.Settings);
        }

        public static void ApplyFuelSettings(VanillaContinueSettings settings)
        {
            var def = DefDatabase<ThingDef>.GetNamedSilentFail("GravEngine");
            var refuelable = def?.comps?.Find(c => c is CompProperties_Refuelable) as CompProperties_Refuelable;
            if (refuelable == null)
            {
                return;
            }

            refuelable.fuelCapacity = settings?.MaxFuelCapacity ?? 10000f;
            refuelable.initialConfigurableTargetFuelLevel = refuelable.fuelCapacity;

            string fuelDefName = settings?.UseGravlite == true ? "GravlitePanel" : "Plasteel";
            var filter = new ThingFilter();
            filter.SetAllow(DefDatabase<ThingDef>.GetNamed(fuelDefName), true);
            refuelable.fuelFilter = filter;
            string fuelLabel = ("VanillaContinue.Fuel" + (settings?.UseGravlite == true ? "Gravlite" : "Plasteel")).Translate();
            refuelable.fuelLabel = fuelLabel;
            refuelable.fuelGizmoLabel = fuelLabel;
        }
    }
}
