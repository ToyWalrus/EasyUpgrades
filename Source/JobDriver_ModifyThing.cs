using System.Collections.Generic;
using RimWorld;
using Verse.AI;
using UnityEngine;
using Verse;

namespace EasyUpgrades
{
    public abstract class JobDriver_ModifyThing : JobDriver_RemoveBuilding
    {
        private float totalNeededWork;
        private float workLeft;
        private List<Thing> resourcesPlaced;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            if (getModifyToThing(Target) == null)
            {
                yield break;
            }
            this.FailOnForbidden(TargetIndex.A);
            Toil gotoThingToUpgrade = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell).FailOnDestroyedNullOrForbidden(TargetIndex.A);

            if (getAdditionalRequiredResources(Target) != null)
            {
                resourcesPlaced = new List<Thing>();
                yield return Toils_Jump.JumpIf(gotoThingToUpgrade, () => job.GetTargetQueue(TargetIndex.B).NullOrEmpty());

                Toil extract = Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.B);
                yield return extract;

                Toil gotoNextHaulThing = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
                yield return gotoNextHaulThing;

                yield return Toils_Haul.StartCarryThing(TargetIndex.B, true, false, true);
                yield return JumpToCollectNextThingForUpgrade(gotoNextHaulThing, TargetIndex.B);
                yield return gotoThingToUpgrade;

                yield return Toils_Jump.JumpIf(gotoNextHaulThing, () => pawn.carryTracker.CarriedThing == null);
                Toil findPlaceTarget = Toils_JobTransforms.SetTargetToIngredientPlaceCell(TargetIndex.A, TargetIndex.B, TargetIndex.C);
                yield return findPlaceTarget;

                yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, findPlaceTarget, false);
                yield return RecordPlacedResource();
                yield return Toils_Jump.JumpIfHaveTargetInQueue(TargetIndex.B, extract);
                
                extract = null;
                gotoNextHaulThing = null;
                findPlaceTarget = null;
            }

            yield return gotoThingToUpgrade;
            
            Toil modify = new Toil().FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);

            modify.initAction = () =>
            {
                totalNeededWork = TotalNeededWork;
                workLeft = totalNeededWork;
            };

            modify.tickAction = () =>
            {
                workLeft -= modify.actor.GetStatValue(StatDefOf.ConstructionSpeed, true) * 1.3f;
                modify.actor.skills.Learn(SkillDefOf.Construction, .08f * modify.actor.GetStatValue(StatDefOf.GlobalLearningFactor));
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
                    DestroyPlacedResources();
                    RemoveAndReplace(modify.actor);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            yield break;
        }

        private Toil JumpToCollectNextThingForUpgrade(Toil gotoGetTargetToil, TargetIndex idx)
        {
            Toil toil = new Toil();
            toil.initAction = delegate ()
            {
                Pawn actor = toil.actor;
                if (actor.carryTracker.CarriedThing == null)
                {
                    Log.Error(actor + " is not carrying anything");
                    return;
                }

                if (actor.carryTracker.Full)
                {
                    return;
                }

                Job curJob = actor.jobs.curJob;
                List<LocalTargetInfo> targetQueue = curJob.GetTargetQueue(idx);
                if (targetQueue.NullOrEmpty())
                {
                    return;
                }

                for (int i = 0; i < targetQueue.Count; i++)
                {
                    Log.Message("Testing " + targetQueue[i].Thing.Label + " for work or something");
                    if (GenAI.CanUseItemForWork(actor, targetQueue[i].Thing) && targetQueue[i].Thing.CanStackWith(actor.carryTracker.CarriedThing))
                    {
                        Log.Message("Using item for work: " + targetQueue[i].Thing.Label);
                        int amountCarried = (actor.carryTracker.CarriedThing == null) ? 0 : actor.carryTracker.CarriedThing.stackCount;
                        int amountToSatisfy = curJob.countQueue[i];
                        amountToSatisfy = Mathf.Min(amountToSatisfy, targetQueue[i].Thing.def.stackLimit - amountCarried);
                        amountToSatisfy = Mathf.Min(amountToSatisfy, actor.carryTracker.AvailableStackSpace(targetQueue[i].Thing.def));
                        if (amountToSatisfy > 0)
                        {
                            curJob.count = amountToSatisfy;
                            curJob.SetTarget(idx, targetQueue[i].Thing);
                            List<int> countQueue = curJob.countQueue;
                            int index = i;
                            countQueue[index] -= amountToSatisfy;
                            if (curJob.countQueue[index] <= 0)
                            {
                                Log.Message("Finished count for " + targetQueue[index].Label);
                                curJob.countQueue.RemoveAt(index);
                                targetQueue.RemoveAt(index);
                            }
                            actor.jobs.curDriver.JumpToToil(gotoGetTargetToil);
                            return;
                        }
                    }
                }
            };
            return toil;
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

            // Try to refund the unused fuel
            //CompRefuelable refuelable = Building.TryGetComp<CompRefuelable>();
            //if (refuelable != null)
            //{
            //    refuelable.ParentHolder.GetDirectlyHeldThings().TryDropAll(Building.Position, Building.Map, ThingPlaceMode.Near);                
            //}

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

        private void DestroyPlacedResources()
        {
            // Despawn used resources
            if (job.placedThings != null)
            {      
                foreach (ThingCountClass used in job.placedThings)
                {
                    if (used.thing.Destroyed)
                    {
                        Log.Error("Tried to use up " + used.thing.Label + " but it was already destroyed!");                        
                    }
                    else
                    {
                        used.thing.Destroy();
                    }
                }
            }
        }
        
        private Toil RecordPlacedResource()
        {
            return Toils_General.Do(() =>
            {
                //resourcesPlaced.Add(TargetB.Thing);
                //Log.Message("Just placed " + TargetB.Thing.stackCount + " " + TargetB.Thing.def.label);
                //Log.Message("Resources placed so far:");
                foreach (ThingCountClass t in job.placedThings)
                {
                    Log.Message(t.thing.Label + ": " + t.Count);
                }
            });
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
