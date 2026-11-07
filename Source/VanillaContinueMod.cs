using RimWorld;
using UnityEngine;
using Verse;

namespace VanillaContinue
{
    /// <summary>
    /// 设置与入口：基础发电量、每单位燃料发电量与燃料上限全部即时生效
    /// （上限通过运行时改写 Def 实现，无需重启游戏）。燃料固定为玻璃钢，与原 mod 一致。
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
                GravEnginePatcher.ApplyFuelCapacity(Settings.MaxFuelCapacity);
            }

            if (listing.ButtonText("VanillaContinue.ResetDefaults".Translate()))
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

    /// <summary>启动与设置变更时，把燃料上限写入逆重引擎的燃料组件定义。</summary>
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
            if (refuelable == null)
            {
                return;
            }
            refuelable.fuelCapacity = capacity;
            refuelable.initialConfigurableTargetFuelLevel = capacity;
        }
    }
}
