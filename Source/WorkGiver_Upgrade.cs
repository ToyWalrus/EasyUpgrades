using Verse;
using System.Linq;
using System.Collections.Generic;
using Verse.AI;
using RimWorld;
using UnityEngine;


namespace EasyUpgrades
{
    class WorkGiver_Upgrade : WorkGiver_Scanner
    {
        private DesignationDef DesUp => EasyUpgradesDesignationDefOf.Upgrade;
        private JobDef JobUpgrade => EasyUpgradesJobDefOf.UpgradeThing;

        public override ThingRequest PotentialWorkThingRequest
        {
            get => ThingRequest.ForGroup(ThingRequestGroup.Construction);
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn p)
        {
            foreach (Designation designation in p.Map.designationManager.SpawnedDesignationsOfDef(DesUp))
            {
                yield return designation.target.Thing;
            }
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
                if (des.def == DesUp)
                {
                    WorkTypeDef def = WorkTypeDefOf.Construction;
                    if (pawn.workSettings.GetPriority(def) == 0)
                    {
                        string reason;
                        if (pawn.WorkTypeIsDisabled(def))
                        {
                            reason = "CannotPrioritizeWorkTypeDisabled".Translate(def.gerundLabel);
                            JobFailReason.Is(reason.Substring(reason.IndexOf(":") + 2));
                            return null;
                        }
                        reason = "CannotPrioritizeNotAssignedToWorkType".Translate(def.gerundLabel);
                        JobFailReason.Is(reason.Substring(reason.IndexOf(":") + 2));
                        return null;
                    }

                    return MakeUpgradeJob(t, pawn);
                }
            }
            return null;
        }

        private Job MakeUpgradeJob(Thing thingToUpgrade, Pawn pawn)
        {
            List<ThingDef> missingResources;
            List<ThingDefCountClass> neededResources = thingToUpgrade.TryGetComp<CompUpgrade>().additionalRequiredResources;
            List<Thing> foundResources = FindAvailableResources(pawn, thingToUpgrade, neededResources, out missingResources);

            if (missingResources.Count == 1)
            {
                JobFailReason.Is("EU.LackingResourcesError_1".Translate(missingResources[0].label));
                return null;
            }
            else if (missingResources.Count > 0)
            {
                JobFailReason.Is("EU.LackingResourcesError_2".Translate(missingResources[0].label, missingResources[1].label));
                return null;
            }

            Dictionary<ThingDef, int> reservedResources = new Dictionary<ThingDef, int>();
            Job job = JobMaker.MakeJob(JobUpgrade, thingToUpgrade);
            job.targetQueueB = new List<LocalTargetInfo>();
            job.countQueue = new List<int>(foundResources.Count);
            for (int j = 0; j < foundResources.Count; j++)
            {
                job.targetQueueB.Add(foundResources[j]);

                ThingDef def = foundResources[j].def;
                int totalNeeded = neededResources.Where((t) => t.thingDef == foundResources[j].def).FirstOrDefault().count;
                int alreadyReserved;
                int amountToReserve;

                if (reservedResources.TryGetValue(def, out alreadyReserved))
                {
                    amountToReserve = Mathf.Min(foundResources[j].stackCount, totalNeeded - alreadyReserved);
                    reservedResources[def] = amountToReserve;
                }
                else
                {
                    amountToReserve = Mathf.Min(foundResources[j].stackCount, totalNeeded);
                    reservedResources.Add(def, amountToReserve);
                }

                job.countQueue.Add(amountToReserve);
            }            
            job.haulMode = HaulMode.ToCellNonStorage;
            return job;
        }

        private List<Thing> FindAvailableResources(Pawn pawn, Thing thingToUpgrade, List<ThingDefCountClass> neededResources, out List<ThingDef> missingResources)
        {
            missingResources = new List<ThingDef>();
            List<Thing> found = new List<Thing>();
            foreach (ThingDefCountClass neededResource in neededResources)
            {
                int neededCount = neededResource.count;
                ThingDef neededThing = neededResource.thingDef;
                int available = 0;
                bool hasEnough = false;                

                if (!pawn.Map.itemAvailability.ThingsAvailableAnywhere(neededResource, pawn))
                {
                    missingResources.Add(neededThing);
                    continue;
                }

                IntVec3 centerPoint = thingToUpgrade.Position;
                IEnumerable<Thing> allThingsOfTypeOnMap = pawn.Map.listerThings.ThingsOfDef(neededThing).OrderBy((t) => (centerPoint - t.Position).LengthManhattan);
                foreach (Thing nextThing in allThingsOfTypeOnMap)
                {                    
                    if (!nextThing.IsForbidden(pawn) && pawn.CanReserve(nextThing) && pawn.CanReach(nextThing, PathEndMode.ClosestTouch, Danger.Deadly))
                    {
                        available += nextThing.stackCount;
                        found.Add(nextThing);

                        if (available >= neededCount)
                        {
                            hasEnough = true;
                            break;
                        }
                    }
                }
                
                if (!hasEnough)
                {
                    missingResources.Add(neededThing);
                }
            }

            return found;
        }

    }
}
