﻿using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace EasyUpgrades
{
    class WorkGiver_IncreaseArtQuality : WorkGiver_IncreaseQuality
    {
        protected override DesignationDef Designation => EasyUpgradesDesignationDefOf.IncreaseQuality_Art;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn p)
        {
            foreach (Designation designation in p.Map.designationManager.SpawnedDesignationsOfDef(Designation))
            {
                yield return designation.target.Thing;
            }
        }

        protected override Job MakeIncreaseQualityJob(Thing t, Pawn pawn, List<ThingCountClass> resources)
        {
            if (!CanDoWorkType(WorkTypeDefOf.Crafting, pawn)) return null;

            Building closestSculptingBench = GetClosestNeededCraftingBuilding(pawn, t);
            if (closestSculptingBench == null)
            {
                JobFailReason.Is("EU.NoSculptingBench".Translate());
                return null;
            }

            Job job = JobMaker.MakeJob(EasyUpgradesJobDefOf.IncreaseQuality_Crafting, t, closestSculptingBench);
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
