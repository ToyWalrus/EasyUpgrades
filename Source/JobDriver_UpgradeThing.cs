using System;
using RimWorld;
using Verse;

namespace EasyUpgrades
{
    class JobDriver_UpgradeThing : JobDriver_ModifyThing
    {
        protected override DesignationDef Designation => EasyUpgradesDesignationDefOf.Upgrade;

        protected override ThingDef getModifyToThing(Thing t)
        {
            var upgrade = t.TryGetComp<CompUpgrade>();
            if (upgrade != null) return upgrade.upgradeTo;

            return null;
        }
}
}
