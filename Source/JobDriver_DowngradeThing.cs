using System;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace EasyUpgrades
{
    class JobDriver_DowngradeThing : JobDriver_ModifyThing
    {
        protected override DesignationDef Designation => EasyUpgradesDesignationDefOf.Downgrade;

        protected override ThingDef getModifyToThing(Thing t)
        {
            var downgrade = t.TryGetComp<CompDowngrade>();
            if (downgrade != null) return downgrade.downgradeTo;
            return null;
        }

        protected override List<ThingDefCountClass> getRefundedResources(Thing t)
        {
            var downgrade = t.TryGetComp<CompDowngrade>();
            if (downgrade != null) return downgrade.refundedResources;
            return null;
        }
    }
}
