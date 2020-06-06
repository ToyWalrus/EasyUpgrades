using Verse;
using System.Collections.Generic;
using Verse.AI;
using RimWorld;

namespace EasyUpgrades
{
    class WorkGiver_Downgrade : WorkGiver_Scanner
    {
        private DesignationDef DesDown => EasyUpgradesDesignationDefOf.Downgrade;
        private JobDef JobDowngrade => EasyUpgradesJobDefOf.DowngradeThing;

        public override PathEndMode PathEndMode
        {
            get => PathEndMode.Touch;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!pawn.CanReserve(t, 1, -1, null, forced))
            {
                return null;
            }
            if (t.IsForbidden(pawn) || t.IsBurning())
            {
                return null;
            }
            foreach (Designation des in pawn.Map.designationManager.AllDesignationsOn(t))
            {
                if (des.def == DesDown)
                {
                    return JobMaker.MakeJob(JobDowngrade, t);
                }
            }
            return null;
        }

        public override ThingRequest PotentialWorkThingRequest
        {
            get => ThingRequest.ForGroup(ThingRequestGroup.Construction);
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn p)
        {
            foreach (Designation designation in p.Map.designationManager.SpawnedDesignationsOfDef(EasyUpgradesDesignationDefOf.Downgrade))
            {
                yield return designation.target.Thing;
            }
        }
    }
}
