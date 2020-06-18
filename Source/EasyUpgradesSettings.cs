using System;
using Verse;
using RimWorld;
using UnityEngine;

namespace EasyUpgrades
{
    // https://rimworldwiki.com/wiki/Modding_Tutorials/ModSettings
    class EasyUpgradesSettings : ModSettings
    {
        public static float increaseAwfulQualityChance = .95f;
        public static float increasePoorQualityChance = .9f;
        public static float increaseNormalQualityChance = .85f;
        public static float increaseGoodQualityChance = .6f;
        public static float increaseExcellentQualityChance = .25f;
        public static float increaseMasterworkQualityChance = .15f;

        public static float decreasePoorQualityChance = .02f;
        public static float decreaseNormalQualityChance = .07f;
        public static float decreaseGoodQualityChance = .12f;
        public static float decreaseExcellentQualityChance = .19f;
        public static float decreaseMasterworkQualityChance = .25f;

        public static float neededMaterialsAwfulQuality = .2f;
        public static float neededMaterialsPoorQuality = .6f;
        public static float neededMaterialsNormalQuality = .9f;
        public static float neededMaterialsGoodQuality = 1.25f;
        public static float neededMaterialsExcellentQuality = 2f;
        public static float neededMaterialsMasterworkQuality = 3f;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref increaseAwfulQualityChance, "increaseAwfulQualityChance", .95f);
            Scribe_Values.Look(ref increasePoorQualityChance, "increasePoorQualityChance", .9f);
            Scribe_Values.Look(ref increaseNormalQualityChance, "increaseNormalQualityChance", .85f);
            Scribe_Values.Look(ref increaseGoodQualityChance, "increaseGoodQualityChance", .6f);
            Scribe_Values.Look(ref increaseExcellentQualityChance, "increaseExcellentQualityChance", .25f);
            Scribe_Values.Look(ref increaseMasterworkQualityChance, "increaseMasterworkQualityChance", .15f);

            Scribe_Values.Look(ref decreasePoorQualityChance, "decreasePoorQualityChance", .02f);
            Scribe_Values.Look(ref decreaseNormalQualityChance, "decreaseNormalQualityChance", .07f);
            Scribe_Values.Look(ref decreaseGoodQualityChance, "decreaseGoodQualityChance", .12f);
            Scribe_Values.Look(ref decreaseExcellentQualityChance, "decreaseExcellentQualityChance", .19f);
            Scribe_Values.Look(ref decreaseMasterworkQualityChance, "decreaseMasterworkQualityChance", .25f);

            Scribe_Values.Look(ref neededMaterialsAwfulQuality, "neededMaterialsAwfulQuality", .2f);
            Scribe_Values.Look(ref neededMaterialsPoorQuality, "neededMaterialsPoorQuality", .6f);
            Scribe_Values.Look(ref neededMaterialsNormalQuality, "neededMaterialsNormalQuality", .9f);
            Scribe_Values.Look(ref neededMaterialsGoodQuality, "neededMaterialsGoodQuality", 1.25f);
            Scribe_Values.Look(ref neededMaterialsExcellentQuality, "neededMaterialsExcellentQuality", 2f);
            Scribe_Values.Look(ref neededMaterialsMasterworkQuality, "neededMaterialsMasterworkQuality", 3f);

            base.ExposeData();
        }
    }

    class EasyUpgradesMod : Mod
    {
        public EasyUpgradesMod(ModContentPack content) : base(content) 
        {
            GetSettings<EasyUpgradesSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            string lvl = EasyUpgrades.baseLevel.ToString();
            string awful = QualityCategory.Awful.GetLabel();
            string poor = QualityCategory.Poor.GetLabel();
            string norm = QualityCategory.Normal.GetLabel();
            string good = QualityCategory.Good.GetLabel();
            string excel = QualityCategory.Excellent.GetLabel();
            string master = QualityCategory.Masterwork.GetLabel();
            string legend = QualityCategory.Legendary.GetLabel();

            Listing_Standard listingStandard = new Listing_Standard();

            listingStandard.Begin(new Rect(inRect.x, inRect.y, inRect.width / 2.15f, inRect.height / 1.5f));
            listingStandard.Label("EU.Settings.SuccessTitle".Translate());

            listingStandard.GapLine(2);
            listingStandard.Gap(6);
            listingStandard.Label("EU.Settings.XtoY".Translate(awful, poor, EasyUpgradesSettings.increaseAwfulQualityChance.ToStringPercent()), -1, "EU.Settings.IncreaseItemQualityTooltip".Translate(awful, poor, lvl));
            EasyUpgradesSettings.increaseAwfulQualityChance = listingStandard.Slider(EasyUpgradesSettings.increaseAwfulQualityChance, 0f, 1f);
            
            listingStandard.Gap(1);
            listingStandard.Label("EU.Settings.XtoY".Translate(poor, norm, EasyUpgradesSettings.increasePoorQualityChance.ToStringPercent()), -1, "EU.Settings.IncreaseItemQualityTooltip".Translate(poor, norm, lvl));
            EasyUpgradesSettings.increasePoorQualityChance = listingStandard.Slider(EasyUpgradesSettings.increasePoorQualityChance, 0f, 1f);

            listingStandard.Gap(1);
            listingStandard.Label("EU.Settings.XtoY".Translate(norm, good, EasyUpgradesSettings.increaseNormalQualityChance.ToStringPercent()), -1, "EU.Settings.IncreaseItemQualityTooltip".Translate(norm, good, lvl));
            EasyUpgradesSettings.increaseNormalQualityChance = listingStandard.Slider(EasyUpgradesSettings.increaseNormalQualityChance, 0f, 1f);

            listingStandard.Gap(1);
            listingStandard.Label("EU.Settings.XtoY".Translate(good, excel, EasyUpgradesSettings.increaseGoodQualityChance.ToStringPercent()), -1, "EU.Settings.IncreaseItemQualityTooltip".Translate(good, excel, lvl));
            EasyUpgradesSettings.increaseGoodQualityChance = listingStandard.Slider(EasyUpgradesSettings.increaseGoodQualityChance, 0f, 1f);

            listingStandard.Gap(1);
            listingStandard.Label("EU.Settings.XtoY".Translate(excel, master, EasyUpgradesSettings.increaseExcellentQualityChance.ToStringPercent()), -1, "EU.Settings.IncreaseItemQualityTooltip".Translate(excel, master, lvl));
            EasyUpgradesSettings.increaseExcellentQualityChance = listingStandard.Slider(EasyUpgradesSettings.increaseExcellentQualityChance, 0f, 1f);

            listingStandard.Gap(1);
            listingStandard.Label("EU.Settings.XtoY".Translate(master, legend, EasyUpgradesSettings.increaseMasterworkQualityChance.ToStringPercent()), -1, "EU.Settings.IncreaseItemQualityTooltip".Translate(master, legend, lvl));
            EasyUpgradesSettings.increaseMasterworkQualityChance = listingStandard.Slider(EasyUpgradesSettings.increaseMasterworkQualityChance, 0f, 1f);

            listingStandard.End();

            listingStandard.Begin(new Rect(inRect.width / 2, inRect.y, inRect.width / 2f, inRect.height / 1.5f));
            listingStandard.Label("EU.Settings.FailTitle".Translate());

            listingStandard.GapLine(2);
            listingStandard.Gap(6);
            listingStandard.Label("EU.Settings.XtoY".Translate(poor, awful, EasyUpgradesSettings.decreasePoorQualityChance.ToStringPercent()), -1, "EU.Settings.DecreaseItemQualityTooltip".Translate(poor, awful, lvl));
            EasyUpgradesSettings.decreasePoorQualityChance = listingStandard.Slider(EasyUpgradesSettings.decreasePoorQualityChance, 0f, 1f);

            listingStandard.Gap(1);
            listingStandard.Label("EU.Settings.XtoY".Translate(norm, poor, EasyUpgradesSettings.decreaseNormalQualityChance.ToStringPercent()), -1, "EU.Settings.DecreaseItemQualityTooltip".Translate(norm, poor, lvl));
            EasyUpgradesSettings.decreaseNormalQualityChance = listingStandard.Slider(EasyUpgradesSettings.decreaseNormalQualityChance, 0f, 1f);

            listingStandard.Gap(1);
            listingStandard.Label("EU.Settings.XtoY".Translate(good, norm, EasyUpgradesSettings.decreaseGoodQualityChance.ToStringPercent()), -1, "EU.Settings.DecreaseItemQualityTooltip".Translate(good, norm, lvl));
            EasyUpgradesSettings.decreaseGoodQualityChance = listingStandard.Slider(EasyUpgradesSettings.decreaseGoodQualityChance, 0f, 1f);

            listingStandard.Gap(1);
            listingStandard.Label("EU.Settings.XtoY".Translate(excel, good, EasyUpgradesSettings.decreaseExcellentQualityChance.ToStringPercent()), -1, "EU.Settings.DecreaseItemQualityTooltip".Translate(excel, good, lvl));
            EasyUpgradesSettings.decreaseExcellentQualityChance = listingStandard.Slider(EasyUpgradesSettings.decreaseExcellentQualityChance, 0f, 1f);

            listingStandard.Gap(1);
            listingStandard.Label("EU.Settings.XtoY".Translate(master, excel, EasyUpgradesSettings.decreaseMasterworkQualityChance.ToStringPercent()), -1, "EU.Settings.DecreaseItemQualityTooltip".Translate(master, excel, lvl));
            EasyUpgradesSettings.decreaseMasterworkQualityChance = listingStandard.Slider(EasyUpgradesSettings.decreaseMasterworkQualityChance, 0f, 1f);

            listingStandard.End();


            listingStandard.Begin(new Rect(inRect.x, inRect.height / 1.5f, inRect.width, 35));
            listingStandard.Label("EU.Settings.MaterialsTitle".Translate());
            listingStandard.GapLine(2);
            listingStandard.Gap(6);
            listingStandard.End();

            listingStandard.Begin(new Rect(inRect.x, inRect.height / 1.5f + 35, inRect.width / 2.15f, 150f));
            listingStandard.Label("EU.Settings.MaterialsNeededFor".Translate(awful, EasyUpgradesSettings.neededMaterialsAwfulQuality.ToString()), -1, "EU.Settings.MaterialsNeededTooltip".Translate());
            EasyUpgradesSettings.neededMaterialsAwfulQuality = listingStandard.Slider(EasyUpgradesSettings.neededMaterialsAwfulQuality, 0f, 10f);

            listingStandard.Gap(1);
            listingStandard.Label("EU.Settings.MaterialsNeededFor".Translate(poor, EasyUpgradesSettings.neededMaterialsPoorQuality.ToString()), -1, "EU.Settings.MaterialsNeededTooltip".Translate());
            EasyUpgradesSettings.neededMaterialsPoorQuality = listingStandard.Slider(EasyUpgradesSettings.neededMaterialsPoorQuality, 0f, 10f);

            listingStandard.Gap(1);
            listingStandard.Label("EU.Settings.MaterialsNeededFor".Translate(norm, EasyUpgradesSettings.neededMaterialsNormalQuality.ToString()), -1, "EU.Settings.MaterialsNeededTooltip".Translate());
            EasyUpgradesSettings.neededMaterialsNormalQuality = listingStandard.Slider(EasyUpgradesSettings.neededMaterialsNormalQuality, 0f, 10f);
            listingStandard.End();

            listingStandard.Begin(new Rect(inRect.width / 2, inRect.height / 1.5f + 35, inRect.width / 2.15f, 150f));
            listingStandard.Label("EU.Settings.MaterialsNeededFor".Translate(good, EasyUpgradesSettings.neededMaterialsGoodQuality.ToString()), -1, "EU.Settings.MaterialsNeededTooltip".Translate());
            EasyUpgradesSettings.neededMaterialsGoodQuality = listingStandard.Slider(EasyUpgradesSettings.neededMaterialsGoodQuality, 0f, 10f);

            listingStandard.Gap(1);
            listingStandard.Label("EU.Settings.MaterialsNeededFor".Translate(excel, EasyUpgradesSettings.neededMaterialsExcellentQuality.ToString()), -1, "EU.Settings.MaterialsNeededTooltip".Translate());
            EasyUpgradesSettings.neededMaterialsExcellentQuality = listingStandard.Slider(EasyUpgradesSettings.neededMaterialsExcellentQuality, 0f, 10f);

            listingStandard.Gap(1);
            listingStandard.Label("EU.Settings.MaterialsNeededFor".Translate(master, EasyUpgradesSettings.neededMaterialsMasterworkQuality.ToString()), -1, "EU.Settings.MaterialsNeededTooltip".Translate());
            EasyUpgradesSettings.neededMaterialsMasterworkQuality = listingStandard.Slider(EasyUpgradesSettings.neededMaterialsMasterworkQuality, 0f, 10f);
            listingStandard.End();

            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Easy Upgrades Settings";
        }
    }
}
