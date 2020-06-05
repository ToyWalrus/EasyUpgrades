using Verse;
using System.Linq;
using System.Collections.Generic;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace EasyUpgrades
{
    class WorkGiver_Upgradable : WorkGiver_Scanner
    {
        private DesignationDef DesUp => EasyUpgradesDesignationDefOf.Upgrade;
        private DesignationDef DesDown => EasyUpgradesDesignationDefOf.Downgrade;

        private JobDef JobUpgrade => EasyUpgradesJobDefOf.UpgradeThing;
        private JobDef JobDowngrade => EasyUpgradesJobDefOf.DowngradeThing;

        //private static List<Thing> resourcesAvailable = new List<Thing>();


        public override PathEndMode PathEndMode
        {
            get
            {
                return PathEndMode.Touch;
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
                    return MakeUpgradeJob(t, pawn);
                }
                else if (des.def == DesDown)
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
            foreach (Designation designation in p.Map.designationManager.SpawnedDesignationsOfDef(EasyUpgradesDesignationDefOf.Upgrade))
            {
                yield return designation.target.Thing;
                yield break;
            }
            foreach (Designation designation in p.Map.designationManager.SpawnedDesignationsOfDef(EasyUpgradesDesignationDefOf.Downgrade))
            {
                yield return designation.target.Thing;
                yield break;
            }
        }

        private Job MakeUpgradeJob(Thing thingToUpgrade, Pawn pawn)
        {
            List<ThingDef> missingResources;
            List<ThingDefCountClass> neededResources = thingToUpgrade.TryGetComp<CompUpgrade>().additionalRequiredResources;
            List<Thing> foundResources = FindAvailableResources(pawn, neededResources, out missingResources);

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

            Job job = JobMaker.MakeJob(JobUpgrade, thingToUpgrade);
            job.targetQueueA = new List<LocalTargetInfo>();            
            for (int j = 0; j < foundResources.Count; j++)
            {
                job.targetQueueA.Add(foundResources[j]);
            }
            job.count = neededResources.Sum((t) => t.count);
            job.haulMode = HaulMode.ToCellNonStorage;
            return job;
        }

        private List<Thing> FindAvailableResources(Pawn pawn, List<ThingDefCountClass> neededResources, out List<ThingDef> missingResources)
        {
            missingResources = new List<ThingDef>();
            List<Thing> found = new List<Thing>();
            List<SlotGroup> zones = new List<SlotGroup>(pawn.Map.haulDestinationManager.AllGroups.ToList());

            foreach (ThingDefCountClass neededResource in neededResources)
            {
                int neededCount = neededResource.count;
                ThingDef neededThing = neededResource.thingDef;
                bool hasEnough = false;

                for (int i = 0; i < zones.Count; ++i)
                {
                    IEnumerator<Thing> enumerator = zones[i].HeldThings.GetEnumerator();                    
                    while(enumerator.MoveNext())
                    {
                        Thing current = enumerator.Current;
                        if (current.def == neededThing && !current.IsForbidden(pawn))
                        {
                            neededCount -= current.stackCount;
                            Thing t = ThingMaker.MakeThing(current.def);
                            

                            found.Add(current);

                            if (neededCount <= 0)
                            {
                                hasEnough = true;
                                break;
                            }
                        }
                    }

                    if (hasEnough)
                    {
                        break;
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
