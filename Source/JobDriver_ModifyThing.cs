using System.Collections.Generic;
using System.Linq;
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
            resourcesPlaced = new List<Thing>();

            if (getAdditionalRequiredResources(Target) != null)
            {
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
            modify.activeSkill = () => SkillDefOf.Construction;
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

        private Toil JumpToCollectNextThingForUpgrade(Toil gotoGetTargetToil, TargetIndex targetIdx)
        {
            return Toils_General.Do(() =>
            {
                Pawn actor = gotoGetTargetToil.actor;
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
                List<LocalTargetInfo> targetQueue = curJob.GetTargetQueue(targetIdx);
                if (targetQueue.NullOrEmpty())
                {
                    return;
                }

                for (int i = 0; i < targetQueue.Count; i++)
                {
                    int idx = i;
                    if (GenAI.CanUseItemForWork(actor, targetQueue[idx].Thing) && targetQueue[i].Thing.CanStackWith(actor.carryTracker.CarriedThing))
                    {
                        int amountCarried = (actor.carryTracker.CarriedThing == null) ? 0 : actor.carryTracker.CarriedThing.stackCount;
                        int amountToSatisfy = curJob.countQueue[idx];
                        amountToSatisfy = Mathf.Min(amountToSatisfy, targetQueue[idx].Thing.def.stackLimit - amountCarried);
                        amountToSatisfy = Mathf.Min(amountToSatisfy, actor.carryTracker.AvailableStackSpace(targetQueue[idx].Thing.def));
                        if (amountToSatisfy > 0)
                        {
                            curJob.count = amountToSatisfy;
                            curJob.SetTarget(targetIdx, targetQueue[idx].Thing);
                            List<int> countQueue = curJob.countQueue;
                            countQueue[idx] -= amountToSatisfy;
                            if (curJob.countQueue[idx] <= 0)
                            {
                                curJob.countQueue.RemoveAt(idx);
                                targetQueue.RemoveAt(idx);
                            }
                            actor.jobs.curDriver.JumpToToil(gotoGetTargetToil);
                            return;
                        }
                    }
                }
            });
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
            CompRefuelable refuelable = Building.TryGetComp<CompRefuelable>();
            if (refuelable != null)
            {
                int fuelToRefund = Mathf.CeilToInt(refuelable.Fuel);
                ThingDef fuelDef = refuelable.Props.fuelFilter.AllowedThingDefs.First();
                if (fuelDef != null && fuelToRefund > 0)
                {
                    Thing fuel = ThingMaker.MakeThing(fuelDef);
                    fuel.stackCount = fuelToRefund;
                    GenPlace.TryPlaceThing(fuel, position, Map, ThingPlaceMode.Near);
                }
            }

            Map.designationManager.RemoveAllDesignationsOn(Building);
            Building.DeSpawn();

            Thing newThing = ThingMaker.MakeThing(modifyTo, madeOf);
            newThing.SetFactionDirect(Faction.OfPlayer);
            newThing.HitPoints = newThing.MaxHitPoints;

            // Add bills from previous building
            if (currentBills != null)
            {
                foreach (Bill bill in currentBills)
                {
                    (newThing as Building_WorkTable).BillStack.AddBill(bill);
                }                
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
            foreach (Thing used in resourcesPlaced)
            {
                if (!used.Destroyed)
                { 
                    used.Destroy();
                }
            }
        }

        private Toil RecordPlacedResource()
        {
            return Toils_General.Do(() =>
            {
                resourcesPlaced.Add(TargetB.Thing);
            });
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<Thing>(ref resourcesPlaced, "resourcesPlaced", LookMode.Reference, new List<Thing>());
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
