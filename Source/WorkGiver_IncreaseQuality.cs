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
        private DesignationDef buildingDes = EasyUpgradesDesignationDefOf.IncreaseQuality_Building;
        private DesignationDef apparelDes = EasyUpgradesDesignationDefOf.IncreaseQuality_Apparel;
        private DesignationDef artDes = EasyUpgradesDesignationDefOf.IncreaseQuality_Art;
        private DesignationDef itemDes = EasyUpgradesDesignationDefOf.IncreaseQuality_Item;
        private bool IsAnyIncreaseQualityDesignation(Designation des) => des.def == buildingDes || des.def == apparelDes || des.def == artDes || des.def == itemDes;

        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Art | ThingRequestGroup.BuildingFrame | ThingRequestGroup.Apparel | ThingRequestGroup.MinifiedThing | ThingRequestGroup.Weapon);

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn p)
        {
            foreach (Designation designation in p.Map.designationManager.SpawnedDesignationsOfDef(buildingDes))
            {
                Log.Message("Building designation exists!");
                yield return designation.target.Thing;
            }
            foreach (Designation designation in p.Map.designationManager.SpawnedDesignationsOfDef(apparelDes))
            {
                Log.Message("Apparel designation exists!");
                yield return designation.target.Thing;
            }
            foreach (Designation designation in p.Map.designationManager.SpawnedDesignationsOfDef(artDes))
            {
                Log.Message("Art designation exists!");
                yield return designation.target.Thing;
            }
            foreach (Designation designation in p.Map.designationManager.SpawnedDesignationsOfDef(itemDes))
            {
                Log.Message("Weapon designation exists!");
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
                if (IsAnyIncreaseQualityDesignation(des))
                {
                    List<ThingCountClass> resources;
                    ThingDefCountClass neededResource = GetStuffNeededForQualityIncrease(t);
                    if (!HasEnoughResourcesOfType(pawn, t, neededResource, out resources))
                    {
                        JobFailReason.Is("EU.LackingQualityResource".Translate(neededResource.Label));
                        return null;
                    }

                    if (des.def == buildingDes)
                    {
                        return MakeIncreaseBuildingQualityJob(t, pawn, resources);
                    }
                    else if (des.def == apparelDes)
                    {
                        return MakeIncreaseApparelQualityJob(t, pawn, resources);
                    }
                    else if (des.def == artDes)
                    {
                        return MakeIncreaseArtQualityJob(t, pawn, resources);
                    }
                    else if (des.def == itemDes)
                    {
                        return MakeIncreaseItemQualityJob(t, pawn, resources);
                    }
                }
            }
            return null;
        }

        private Job MakeIncreaseBuildingQualityJob(Thing t, Pawn pawn, List<ThingCountClass> resources)
        {
            if (!CanDoWorkType(WorkTypeDefOf.Construction, pawn)) return null;

            Job job = JobMaker.MakeJob(EasyUpgradesJobDefOf.IncreaseQuality_Building, t);
            job.targetQueueB = new List<LocalTargetInfo>();
            job.countQueue = new List<int>();
            foreach (ThingCountClass resource in resources)
            {
                job.targetQueueB.Add(resource.thing);
                job.countQueue.Add(resource.Count);
            }
            job.haulMode = HaulMode.ToCellNonStorage;
            job.count = job.countQueue.Count > 0 ? job.countQueue[0] : 1;
            return job;
        }

        private Job MakeIncreaseApparelQualityJob(Thing t, Pawn pawn, List<ThingCountClass> resources)
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

        private Job MakeIncreaseItemQualityJob(Thing t, Pawn pawn, List<ThingCountClass> resources)
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

        private Job MakeIncreaseArtQualityJob(Thing t, Pawn pawn, List<ThingCountClass> resources)
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

        private bool CanDoWorkType(WorkTypeDef def, Pawn pawn)
        {
            if (pawn.workSettings.GetPriority(def) == 0)
            {
                if (pawn.WorkTypeIsDisabled(def))
                {
                    JobFailReason.Is("EU.WorkTypeDisabled".Translate(def.gerundLabel));
                    return false;
                }
                JobFailReason.Is("EU.WorkTypeNotAssigned".Translate(def.gerundLabel));
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
                    neededForNextQualityLevel *= EasyUpgradesSettings.neededMaterialsAwfulQuality;
                    break;
                case QualityCategory.Poor:
                    neededForNextQualityLevel *= EasyUpgradesSettings.neededMaterialsPoorQuality;
                    break;
                case QualityCategory.Normal:
                    neededForNextQualityLevel *= EasyUpgradesSettings.neededMaterialsNormalQuality;
                    break;
                case QualityCategory.Good:
                    neededForNextQualityLevel *= EasyUpgradesSettings.neededMaterialsGoodQuality;
                    break;
                case QualityCategory.Excellent:
                    neededForNextQualityLevel *= EasyUpgradesSettings.neededMaterialsExcellentQuality;
                    break;
                case QualityCategory.Masterwork:
                    neededForNextQualityLevel *= EasyUpgradesSettings.neededMaterialsMasterworkQuality;
                    break;
                default:
                    return null;
            }

            return new ThingDefCountClass(neededThing, Mathf.Max(1, Mathf.CeilToInt(neededForNextQualityLevel * amountModifier)));
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
