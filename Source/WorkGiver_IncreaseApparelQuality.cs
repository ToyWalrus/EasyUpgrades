﻿using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace EasyUpgrades
{
    class WorkGiver_IncreaseApparelQuality : WorkGiver_IncreaseQuality
    {
        protected override DesignationDef Designation => EasyUpgradesDesignationDefOf.IncreaseQuality_Apparel;
        
        protected override Job MakeIncreaseQualityJob(Thing t, Pawn pawn, List<ThingCountClass> resources)
        {
            if (!CanDoWorkType(WorkTypeDefOf.Crafting, pawn)) return null;

            Building closestTailoringBench = GetClosestNeededCraftingBuilding(pawn, t);
            if (closestTailoringBench == null)
            {
                JobFailReason.Is("EU.NoTailoringBench".Translate());
                return null;
            }

            Job job = JobMaker.MakeJob(EasyUpgradesJobDefOf.IncreaseQuality_Crafting, t, closestTailoringBench);
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
