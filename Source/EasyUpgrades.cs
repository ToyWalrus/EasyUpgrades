using Verse;
using RimWorld;

namespace EasyUpgrades
{   
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
        public static DesignationDef IncreaseQuality;
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