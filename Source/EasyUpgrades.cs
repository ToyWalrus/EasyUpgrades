using Verse;
using RimWorld;

namespace EasyUpgrades
{
    public class EasyUpgrades
    {
        public static int baseLevel = 14;

        public static float GetSuccessChance(Pawn pawn, SkillDef activeSkill, Thing thing)
        {
            QualityCategory quality;
            thing.TryGetQuality(out quality);

            float qualityChance;
            switch (quality)
            {
                case QualityCategory.Awful:
                    qualityChance = EasyUpgradesSettings.increaseAwfulQualityChance;
                    break;
                case QualityCategory.Poor:
                    qualityChance = EasyUpgradesSettings.increasePoorQualityChance;
                    break;
                case QualityCategory.Normal:
                    qualityChance = EasyUpgradesSettings.increaseNormalQualityChance;
                    break;
                case QualityCategory.Good:
                    qualityChance = EasyUpgradesSettings.increaseGoodQualityChance;
                    break;
                case QualityCategory.Excellent:
                    qualityChance = EasyUpgradesSettings.increaseExcellentQualityChance;
                    break;
                case QualityCategory.Masterwork:
                    qualityChance = EasyUpgradesSettings.increaseMasterworkQualityChance;
                    break;
                default:
                    return 0;
            }

            int skillLevel = pawn.skills.GetSkill(activeSkill).Level;
            float skillPercent = skillLevel / 14f; // lvl 14 the base chance for increasing quality
            return qualityChance * skillPercent;
        }

        public static float GetFailChance(Pawn pawn, SkillDef activeSkill, Thing thing)
        {
            QualityCategory quality;
            thing.TryGetQuality(out quality);

            float qualityChance = 0;
            switch (quality)
            {
                case QualityCategory.Awful:
                    return 0;
                case QualityCategory.Poor:
                    qualityChance = EasyUpgradesSettings.decreasePoorQualityChance;
                    break;
                case QualityCategory.Normal:
                    qualityChance = EasyUpgradesSettings.decreaseNormalQualityChance;
                    break;
                case QualityCategory.Good:
                    qualityChance = EasyUpgradesSettings.decreaseGoodQualityChance;
                    break;
                case QualityCategory.Excellent:
                    qualityChance = EasyUpgradesSettings.decreaseExcellentQualityChance;
                    break;
                case QualityCategory.Masterwork:
                    qualityChance = EasyUpgradesSettings.decreaseMasterworkQualityChance;
                    break;
                default:
                    break;
            }

            int skillLevel = pawn.skills.GetSkill(activeSkill).Level;
            float skillPercent = (20 - skillLevel) / 20f;
            return qualityChance + (skillPercent * .15f);
        }

    }


    [DefOf]
    public static class EasyUpgradesJobDefOf
    {
        static EasyUpgradesJobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(EasyUpgradesJobDefOf));
        }

        public static JobDef UpgradeThing;
        public static JobDef DowngradeThing;
        public static JobDef IncreaseQuality_Building;
        public static JobDef IncreaseQuality_Crafting;
    }


    [DefOf]
    public static class EasyUpgradesDesignationDefOf
    {
        static EasyUpgradesDesignationDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(EasyUpgradesDesignationDefOf));
        }

        public static DesignationDef Upgrade;
        public static DesignationDef Downgrade;
        public static DesignationDef IncreaseQuality_Building;
        public static DesignationDef IncreaseQuality_Apparel;
        public static DesignationDef IncreaseQuality_Item;
        public static DesignationDef IncreaseQuality_Art;
    }


    [DefOf]
    public static class EasyUpgradesWorkGiverDefOf
    {
        static EasyUpgradesWorkGiverDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(EasyUpgradesWorkGiverDefOf));
        }

        public static WorkGiverDef UpgradeThing;
        public static WorkGiverDef DowngradeThing;
        public static WorkGiverDef IncreaseQuality_Building;
        public static WorkGiverDef IncreaseQuality_Apparel;
        public static WorkGiverDef IncreaseQuality_Item;
        public static WorkGiverDef IncreaseQuality_Art;
    }
}