using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;

namespace EasyUpgrades
{
    abstract class WorkGiver_IncreaseQuality : WorkGiver_Scanner
    {
        protected virtual DesignationDef Designation => null;

        protected bool HasIncreaseQualityDesignation(Designation des) => des.def == Designation;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn p)
        {
            foreach (Designation designation in p.Map.designationManager.SpawnedDesignationsOfDef(Designation))
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
                if (HasIncreaseQualityDesignation(des))
                {
                    List<ThingCountClass> resources;
                    ThingDefCountClass neededResource = GetStuffNeededForQualityIncrease(t);
                    if (!HasEnoughResourcesOfType(pawn, t, neededResource, out resources))
                    {
                        JobFailReason.Is("EU.LackingQualityResource".Translate(neededResource.Label));
                        return null;
                    }

                    return MakeIncreaseQualityJob(t, pawn, resources);
                }
            }
            return null;
        }

        protected abstract Job MakeIncreaseQualityJob(Thing t, Pawn pawn, List<ThingCountClass> resources);

        protected bool CanDoWorkType(WorkTypeDef def, Pawn pawn)
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

        protected Building GetClosestNeededCraftingBuilding(Pawn pawn, Thing t)
        {
            List<string> defNames;
            if (t is MinifiedThing) {
               defNames = (t as MinifiedThing).InnerThing.def.recipeMaker.recipeUsers.ConvertAll((def) => def.defName);
            } else
            {
                defNames = t.def.recipeMaker.recipeUsers.ConvertAll((def) => def.defName);
            }
            return pawn.Map.listerBuildings.allBuildingsColonist
                .Where((b) => defNames.Contains(b.def.defName) && !b.IsForbidden(pawn) && !b.IsBurning())
                .OrderBy((b) => (b.Position - pawn.Position).LengthManhattan)
                .FirstOrDefault();
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
            if (t is MinifiedThing)
            {
                t = (t as MinifiedThing).InnerThing;
            }

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
    }
}
