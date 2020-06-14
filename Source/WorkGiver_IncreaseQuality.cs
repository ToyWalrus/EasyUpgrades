using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace EasyUpgrades
{
    class WorkGiver_IncreaseQuality : WorkGiver_Scanner
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn p)
        {
            foreach (Designation designation in p.Map.designationManager.SpawnedDesignationsOfDef(EasyUpgradesDesignationDefOf.IncreaseQuality))
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
                if (des.def == EasyUpgradesDesignationDefOf.IncreaseQuality)
                {
                    List<ThingCountClass> resources;
                    Log.Message("Increase quality for " + t.Label);
                    ThingDefCountClass neededResource = GetStuffNeededForQualityIncrease(t);
                    if (!HasEnoughResourcesOfType(pawn, t, neededResource, out resources)) return null;

                    if (t.def.building != null)
                    {
                        Log.Message("Thing is building!");
                        return MakeIncreaseBuildingQualityJob(t, pawn, resources);
                    }
                    if (t.def.IsApparel)
                    {
                        Log.Message("Thing is apparel!");
                        return MakeIncreaseApparelQualityJob(t, pawn, resources);
                    }
                    if (t.def.IsArt)
                    {
                        Log.Message("Thing is art!");
                        return MakeIncreaseArtQualityJob(t, pawn, resources);
                    }
                    if (t.def.IsWeapon)
                    {
                        Log.Message("Thing is weapon!");
                        return MakeIncreaseItemQualityJob(t, pawn, resources);
                    }
                    Log.Message("Unknown item type for increase item quality job creation");
                    return null;
                }
            }
            return null;
        }

        private Job MakeIncreaseBuildingQualityJob(Thing t, Pawn pawn, List<ThingCountClass> resources)
        {
            if (!CanDoWorkType(WorkTypeDefOf.Construction, pawn)) return null;
            Log.Message("Try to increase building quality built from " + t.Stuff.label);

            Job job = JobMaker.MakeJob(EasyUpgradesJobDefOf.IncreaseQuality_Building, t);
            job.targetQueueB = new List<LocalTargetInfo>();
            job.countQueue = new List<int>();
            foreach (ThingCountClass resource in resources)
            {
                job.targetQueueB.Add(resource.thing);
                job.countQueue.Add(resource.Count);
            }
            job.haulMode = HaulMode.ToCellNonStorage;
            return job;
        }

        private Job MakeIncreaseApparelQualityJob(Thing t, Pawn pawn, List<ThingCountClass> resources)
        {
            if (!CanDoWorkType(WorkTypeDefOf.Crafting, pawn)) return null;
            Log.Message("Try to increase building quality built from " + t.Stuff);

            Building closestTailoringBench = GetClosestNeededCraftingBuilding(pawn, t);
            if (closestTailoringBench == null)
            {
                Log.Message("No tailoring bench exists to increase apparel quality");
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
            return job;
        }

        private Job MakeIncreaseItemQualityJob(Thing t, Pawn pawn, List<ThingCountClass> resources)
        {
            if (!CanDoWorkType(WorkTypeDefOf.Crafting, pawn)) return null;

            Building closestCraftingBench = GetClosestNeededCraftingBuilding(pawn, t);
            if (closestCraftingBench == null)
            {
                Log.Message("No crafting bench exists to increase item quality");
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
            return job;
        }

        private Job MakeIncreaseArtQualityJob(Thing t, Pawn pawn, List<ThingCountClass> resources)
        {
            if (!CanDoWorkType(WorkTypeDefOf.Crafting, pawn)) return null;

            Building closestSculptingBench = GetClosestNeededCraftingBuilding(pawn, t);
            if (closestSculptingBench == null)
            {
                Log.Message("No sculpting bench exists to increase art quality");
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
            return job;
        }

        private bool CanDoWorkType(WorkTypeDef def, Pawn pawn)
        {
            if (pawn.workSettings.GetPriority(def) == 0)
            {
                if (pawn.WorkTypeIsDisabled(def))
                {
                    JobFailReason.Is("CannotPrioritizeWorkTypeDisabled".Translate(def.gerundLabel));
                    return false;
                }
                JobFailReason.Is("CannotPrioritizeNotAssignedToWorkType".Translate(def.gerundLabel));
                return false;
            }
            return true;
        }

        private bool HasEnoughResourcesOfType(Pawn pawn, Thing t, ThingDefCountClass stuffDef, out List<ThingCountClass> resources)
        {
            resources = new List<ThingCountClass>();

            if (stuffDef == null)
            {
                return false;
            }

            int available = 0;
            int neededCount = stuffDef.count;
            ThingDef neededThing = stuffDef.thingDef;
            if (!pawn.Map.itemAvailability.ThingsAvailableAnywhere(stuffDef, pawn))
            {
                return false;
            }

            IntVec3 centerPoint = t.Position;
            IEnumerable<Thing> allThingsOfTypeOnMap = pawn.Map.listerThings.ThingsOfDef(neededThing).OrderBy((resource) => (centerPoint - resource.Position).LengthManhattan);
            foreach (Thing nextThing in allThingsOfTypeOnMap)
            {
                if (!nextThing.IsForbidden(pawn) && pawn.CanReserve(nextThing) && pawn.CanReach(nextThing, PathEndMode.ClosestTouch, Danger.Deadly))
                {
                    resources.Add(new ThingCountClass(nextThing, Mathf.Min(nextThing.stackCount, neededCount - available)));
                    available += nextThing.stackCount;
                    if (available >= neededCount)
                    {
                        break;
                    }
                }
            }

            return available >= neededCount;
        }

        private ThingDefCountClass GetStuffNeededForQualityIncrease(Thing t)
        {
            ThingDef neededThing;
            QualityCategory currentQuality;
            int amountModifier = 1;
            float neededForNextQualityLevel;
            ThingDef stuff = t.Stuff;

            if (!t.def.MadeFromStuff || stuff == null)
            {
                Log.Message("Couldn't get stuff for " + t.Label);
                return null;
            }

            if (!t.TryGetQuality(out currentQuality))
            {
                Log.Error("Couldn't get quality for " + t.Label);
                return null;
            }

            RecipeMakerProperties recipeMaker = t.def.recipeMaker;
            if (recipeMaker != null && recipeMaker.bulkRecipeCount > 0)
            {
                amountModifier = recipeMaker.bulkRecipeCount;
            }

            if (t.def.costList != null)
            {
                ThingDef highestCostThing = null;
                int highestCost = 0;
                foreach (ThingDefCountClass thingCount in t.def.costList)
                {
                    if (thingCount.count > highestCost)
                    {
                        highestCost = thingCount.count;
                        highestCostThing = thingCount.thingDef;
                    }
                }
                neededThing = highestCostThing;
                neededForNextQualityLevel = highestCost;
            }
            else
            {
                neededThing = stuff;
                neededForNextQualityLevel = t.def.costStuffCount;
            }

            switch (currentQuality)
            {
                case QualityCategory.Awful:
                    neededForNextQualityLevel *= 0.2f;
                    break;
                case QualityCategory.Poor:
                    neededForNextQualityLevel *= 0.6f;
                    break;
                case QualityCategory.Normal:
                    neededForNextQualityLevel *= 0.9f;
                    break;
                case QualityCategory.Good:
                    neededForNextQualityLevel *= 1.25f;
                    break;
                case QualityCategory.Excellent:
                    neededForNextQualityLevel *= 2f;
                    break;
                case QualityCategory.Masterwork:
                    neededForNextQualityLevel *= 3f;
                    break;
                case QualityCategory.Legendary:
                    Log.Message("Can't increase quality of a legendary thing!");
                    return null;
            }

            return new ThingDefCountClass(neededThing, Mathf.CeilToInt(neededForNextQualityLevel * amountModifier));
        }

        private Building GetClosestNeededCraftingBuilding(Pawn pawn, Thing t)
        {
            List<string> defNames = t.def.recipeMaker.recipeUsers.ConvertAll((def) => def.defName);
            return pawn.Map.listerBuildings.allBuildingsColonist
                .Where((b) => defNames.Contains(b.def.defName) && !b.IsForbidden(pawn) && !b.IsBurning())
                .OrderBy((b) => (b.Position - pawn.Position).LengthManhattan)
                .FirstOrDefault();
        }
    }
}
