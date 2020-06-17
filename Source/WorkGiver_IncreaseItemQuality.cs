using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace EasyUpgrades
{
    class WorkGiver_IncreaseItemQuality : WorkGiver_IncreaseQuality
    {
        protected override DesignationDef Designation => EasyUpgradesDesignationDefOf.IncreaseQuality_Item;

        protected override Job MakeIncreaseQualityJob(Thing t, Pawn pawn, List<ThingCountClass> resources)
        {
            if (!CanDoWorkType(WorkTypeDefOf.Crafting, pawn)) return null;

            Building closestCraftingBench = GetClosestNeededCraftingBuilding(pawn, t);
            if (closestCraftingBench == null)
            {
                JobFailReason.Is("EU.NoCraftingBench".Translate());
                return null;
            }

            Job job = JobMaker.MakeJob(EasyUpgradesJobDefOf.IncreaseQuality_Crafting, t, closestCraftingBench);
            job.targetQueueA = new List<LocalTargetInfo>();
            job.countQueue = new List<int>();
            foreach (ThingCountClass resource in resources)
            {
                job.targetQueueA.Add(resource.thing);
                job.countQueue.Add(resource.Count);
            }
            job.haulMode = HaulMode.ToCellNonStorage;
            job.count = 1;
            return job;
        }
    }
}
