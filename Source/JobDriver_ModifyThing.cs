using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse.AI;
using UnityEngine;
using Verse;

namespace EasyUpgrades
{
    public abstract class JobDriver_ModifyThing : JobDriver_RemoveBuilding
    {
        public ThingDef modifyTo;
        private float totalNeededWork;
        private float workLeft;
        private List<Thing> resourcesUsed;
        
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (TargetB != null)
            {
                return pawn.Reserve(TargetB, job, 1, -1, null, errorOnFailed);
            }
            return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            if (getModifyToThing(Target) == null)
            {
                yield break;
            }
            this.FailOnForbidden(TargetIndex.A);

            List<ThingDefCountClass> thingDefCounts = getAdditionalRequiredResources(Target);
            if (thingDefCounts != null)
            {
                resourcesUsed = new List<Thing>();
                foreach (LocalTargetInfo requiredResource in additionalRequiredResourceTargetInfos)
                {
                    this.job.targetC = requiredResource;
                    int stackCount = thingDefCounts.Where((t) => t.thingDef == requiredResource.Thing.def).FirstOrDefault().count;
                    //Log.Message("Required resource: " + requiredResource.Label);
                    //Log.Message("Count remaining: " + job.count);

                    yield return Toils_Reserve.Reserve(TargetIndex.C, 1, stackCount);
                    yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(TargetIndex.C);
                    yield return Toils_Haul.StartCarryThing(TargetIndex.C, false, true);           
                    Toil carry = Toils_Haul.CarryHauledThingToCell(TargetIndex.A);
                    yield return carry;
                    yield return Toils_General.Do(() => 
                    {
                        //Log.Message("Using " + pawn.carryTracker.CarriedThing.Label + " in upgrade");
                        resourcesUsed.Add(pawn.carryTracker.CarriedThing);
                    });
                    yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.A, carry, false, true);
                    yield return Toils_Reserve.Reserve(TargetIndex.C, 1, stackCount);
                }
            }

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            
            Toil modify = new Toil().FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);            

            modify.initAction = () =>
            {
                totalNeededWork = TotalNeededWork;
                workLeft = totalNeededWork;
            };

            modify.tickAction = () =>
            {
                workLeft -= modify.actor.GetStatValue(StatDefOf.ConstructionSpeed, true) * 1.3f;
                if (workLeft <= 0f)
                {
                    modify.actor.jobs.curDriver.ReadyForNextToil();
                }
            };

            modify.defaultCompleteMode = ToilCompleteMode.Never;
            modify.WithProgressBar(TargetIndex.A, () => 1f - workLeft / totalNeededWork, false, -0.5f);
            modify.activeSkill = (() => SkillDefOf.Construction);
            modify.PlaySoundAtEnd(SoundDefOf.TinyBell);
            yield return modify;
            
            yield return new Toil
            {
                initAction = () =>
                {
                    DestroyUsedResources();
                    RemoveAndReplace(modify.actor);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            
            yield break;
        }

        void RemoveAndReplace(Pawn pawn)
        {
            IntVec3 position = Building.Position;
            Rot4 rotation = Building.Rotation;
            BillStack currentBills = null;
            List<ThingDefCountClass> refundedResources = getRefundedResources(Target);
            ThingDef modifyTo = getModifyToThing(Target);
            ThingDef madeOf = Target.Stuff;

            if (Building is Building_WorkTable)
            {
                currentBills = (Building as Building_WorkTable).BillStack;
            }

            Building.DeSpawn();

            Thing newThing = ThingMaker.MakeThing(modifyTo, madeOf);
            newThing.SetFactionDirect(Faction.OfPlayer);

            // Generate building quality, if applicable
            CompQuality compQuality = newThing.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                QualityCategory qualityCategory = QualityUtility.GenerateQualityCreatedByPawn(pawn, SkillDefOf.Construction);
                compQuality.SetQuality(qualityCategory, ArtGenerationContext.Colony);
                QualityUtility.SendCraftNotification(newThing, pawn);
            }
            newThing.HitPoints = newThing.MaxHitPoints;

            // Add bills from previous building
            if (currentBills != null)
            {
                (newThing as Building_WorkTable).billStack = currentBills;
            }

            // Attach to power source if applicable and available
            CompPower compPower = newThing.TryGetComp<CompPower>();            
            if (compPower != null)
            {
                CompPower transmitter = PowerConnectionMaker.BestTransmitterForConnector(position, Map);
                if (transmitter != null)
                {
                    compPower.ConnectToTransmitter(transmitter);
                    for (int i = 0; i < 5; i++)
                    {
                        MoteMaker.ThrowMetaPuff(position.ToVector3Shifted(), Map);
                    }
                    Map.mapDrawer.MapMeshDirty(position, MapMeshFlag.PowerGrid);
                    Map.mapDrawer.MapMeshDirty(position, MapMeshFlag.Things);
                }
            }

            GenSpawn.Spawn(newThing, position, Map, rotation, WipeMode.Vanish);
            
            // Refund resources, if applicable
            if (refundedResources != null)
            {
                foreach (ThingDefCountClass resource in refundedResources)
                {
                    Thing thing = ThingMaker.MakeThing(resource.thingDef);
                    thing.stackCount = resource.count;
                    GenPlace.TryPlaceThing(thing, position, Map, ThingPlaceMode.Near);
                }
            }            
        }

        private void DestroyUsedResources()
        {
            // Despawn used resources
            if (resourcesUsed != null)
            {
                foreach (Thing resource in resourcesUsed)
                {
                    resource.Destroy();
                }
            }
        }

        private List<LocalTargetInfo> additionalRequiredResourceTargetInfos
        {
            get => job.targetQueueA;
        }
        
        protected override float TotalNeededWork
        {
            get => Mathf.Clamp(Building.GetStatValue(StatDefOf.WorkToBuild, true), 20f, 3000f);
        }
        
        protected abstract ThingDef getModifyToThing(Thing t);
        protected virtual List<ThingDefCountClass> getRefundedResources(Thing t) => null;
        protected virtual List<ThingDefCountClass> getAdditionalRequiredResources(Thing t) => null;
    }
}
