using System;
using Verse;
using RimWorld;

namespace EasyUpgrades
{
    class JobDriver_DowngradeThing : JobDriver_ModifyThing
    {
        protected override DesignationDef Designation => EasyUpgradesDesignationDefOf.Downgrade;

        protected override ThingDef getModifyToThing(Thing t)
        {
            var upgrade = t.TryGetComp<CompDowngrade>();
            if (upgrade != null) return upgrade.downgradeTo;

            return null;
        }
    }
}
