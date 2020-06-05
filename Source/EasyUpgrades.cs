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
            // may not need harmony?
            //var harmony = new Harmony("com.github.toywalrus.easy_upgrades");
            //harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [StaticConstructorOnStartup]
    public static class ModifiableThings
    {
        public static List<string> defNames;

        static ModifiableThings()
        {
            defNames = new List<string>();
            defNames.Add("FueledStove");
            defNames.Add("ElectricStove");
            defNames.Add("HandTailoringBench");
            defNames.Add("ElectricTailoringBench");
            defNames.Add("Door");
            defNames.Add("Autodoor");
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

    //[DefOf]
    //public static class EasyUpgradesWorkTypeDefOf
    //{
    //    static EasyUpgradesWorkTypeDefOf()
    //    {
    //        DefOfHelper.EnsureInitializedInCtor(typeof(EasyUpgradesWorkTypeDefOf));
    //    }

    //    public static WorkTypeDef UpgradeThing;
    //    public static WorkTypeDef DowngradeThing;
    //}
}