using System.Collections.Generic;
using HarmonyLib;
using Verse;
using RimWorld;

namespace EasyUpgrades
{
    [StaticConstructorOnStartup]
    public class EasyUpgrades
    {
        static EasyUpgrades()
        {
            //var harmony = new Harmony("com.github.toywalrus.easyupgrades");
            //harmony.PatchAll(Assembly.GetExecutingAssembly());
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
        public static JobDef IncreaseQuality;
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
        public static DesignationDef QualityUpgrade;
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
    }
}