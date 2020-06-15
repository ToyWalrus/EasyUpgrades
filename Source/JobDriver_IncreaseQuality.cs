using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace EasyUpgrades
{
    class JobDriver_IncreaseQuality : JobDriver
    {
        List<Thing> resourcesPlaced;
        private bool IsCraftingJob => job.def == EasyUpgradesJobDefOf.IncreaseQuality_Crafting;
        private bool IsArtisticJob => job.GetTarget(TargetIndex.B).Thing?.def?.defName == "SculptingTable";
        private SkillDef ActiveSkillDef => IsArtisticJob
            ? SkillDefOf.Artistic
            : IsCraftingJob
                ? SkillDefOf.Crafting
                : SkillDefOf.Construction;
        private StatDef ActiveStatDef => IsCraftingJob
            ? StatDefOf.WorkSpeedGlobal
            : StatDefOf.ConstructionSpeed;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (IsCraftingJob)
            {
                return pawn.Reserve(TargetB, job, 1, -1, null, errorOnFailed);
            }
            else
            {
                return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            resourcesPlaced = new List<Thing>();
            IEnumerable<Toil> toils = IsCraftingJob ? MakeToilsForCrafting() : MakeToilsForBuilding();
            foreach (Toil toil in toils)
            {
                yield return toil;
            }

            Toil modify = new Toil().FailOnCannotTouch(IsCraftingJob ? TargetIndex.B : TargetIndex.A, PathEndMode.Touch);

            modify.initAction = () =>
            {
                totalWorkNeeded = TotalWorkNeeded;
                workLeft = totalWorkNeeded;
            };

            modify.tickAction = () =>
            {
                workLeft -= modify.actor.GetStatValue(ActiveStatDef, true) * 1.3f;
                modify.actor.skills.Learn(ActiveSkillDef, .08f * modify.actor.GetStatValue(StatDefOf.GlobalLearningFactor));
                if (workLeft <= 0f)
                {
                    modify.actor.jobs.curDriver.ReadyForNextToil();
                }
            };

            modify.defaultCompleteMode = ToilCompleteMode.Never;
            modify.WithProgressBar(IsCraftingJob ? TargetIndex.B : TargetIndex.A, () => 1f - workLeft / totalWorkNeeded, false, -0.5f);
            modify.activeSkill = (() => ActiveSkillDef);
            yield return modify;

            yield return new Toil
            {
                initAction = () =>
                {
                    DestroyPlacedResources();
                    NotifyQualityChanged(modify.actor);
                    RemoveDesignationsForQualityUpgrade(IsCraftingJob ? TargetC.Thing : TargetA.Thing);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            yield break;
        }

        // Index A is first the item to be worked on, then the current resource to be gathered
        // Index B is the workstation to be worked at
        // Index C is the item to be worked on (always)
        // Queue A is the resources to be gathered
        private IEnumerable<Toil> MakeToilsForCrafting()
        {
            this.FailOnForbidden(TargetIndex.A);
            this.FailOnForbidden(TargetIndex.B);
            Toil gotoWorkbench = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell).FailOnDestroyedNullOrForbidden(TargetIndex.B);

            // Take item to workbench
            Toil gotoNextHaulThing = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return gotoNextHaulThing;
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            yield return gotoWorkbench;

            Toil findPlaceTarget = Toils_JobTransforms.SetTargetToIngredientPlaceCell(TargetIndex.B, TargetIndex.A, TargetIndex.C);
            yield return findPlaceTarget;

            yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, findPlaceTarget, false);
            yield return RecordPlacedResource(TargetIndex.A);
            yield return Toils_General.Do(() =>
            {
                job.targetC = job.targetA;
            });

            // Take items from queue to workbench
            yield return Toils_Jump.JumpIf(gotoWorkbench, () => job.GetTargetQueue(TargetIndex.A).NullOrEmpty());
            Toil extract = Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.A);
            yield return extract;

            yield return gotoNextHaulThing;
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            yield return JumpToCollectNextThingForUpgrade(gotoNextHaulThing, TargetIndex.A);
            yield return gotoWorkbench;
            yield return findPlaceTarget;

            yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, findPlaceTarget, false);
            yield return RecordPlacedResource(TargetIndex.A);
            yield return Toils_Jump.JumpIfHaveTargetInQueue(TargetIndex.A, extract);

            extract = null;
            gotoNextHaulThing = null;
            findPlaceTarget = null;

            yield return gotoWorkbench;
        }

        // Index A is the building to be worked on
        // Index B is the current resource to be gathered
        // Queue B is the resources to be gathered
        private IEnumerable<Toil> MakeToilsForBuilding()
        {
            this.FailOnForbidden(TargetIndex.A);
            Toil gotoThingToWorkOn = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell).FailOnDestroyedNullOrForbidden(TargetIndex.A);
            yield return Toils_Jump.JumpIf(gotoThingToWorkOn, () => job.GetTargetQueue(TargetIndex.B).NullOrEmpty());

            Toil extract = Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.B);
            yield return extract;

            Toil gotoNextHaulThing = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
            yield return gotoNextHaulThing;

            yield return Toils_Haul.StartCarryThing(TargetIndex.B, true, false, true);
            yield return JumpToCollectNextThingForUpgrade(gotoNextHaulThing, TargetIndex.B);
            yield return gotoThingToWorkOn;

            yield return Toils_Jump.JumpIf(gotoNextHaulThing, () => pawn.carryTracker.CarriedThing == null);
            Toil findPlaceTarget = Toils_JobTransforms.SetTargetToIngredientPlaceCell(TargetIndex.A, TargetIndex.B, TargetIndex.C);
            yield return findPlaceTarget;

            yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, findPlaceTarget, false);
            yield return RecordPlacedResource(TargetIndex.B);
            yield return Toils_Jump.JumpIfHaveTargetInQueue(TargetIndex.B, extract);

            extract = null;
            gotoNextHaulThing = null;
            findPlaceTarget = null;

            yield return gotoThingToWorkOn;
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

        private void NotifyQualityChanged(Pawn pawn)
        {
            Thing thingModified = IsCraftingJob ? TargetC.Thing : TargetA.Thing;
            QualityCategory q;
            if (!thingModified.TryGetQuality(out q))
            {
                Log.Error("Unable to get comp quality on " + thingModified.Label);
                return;
            }

            // Chance from 0..1
            float successChance = GetSuccessChance(pawn, thingModified);
            float failChance = GetFailChance(pawn, thingModified);
            float randVal = Random.Range(0f, 1f);
            Log.Message("Current quality: " + q.GetLabel());
            Log.Message("Success Chance: " + successChance.ToString() + ", Fail Chance: " + failChance.ToString() + ", value: " + randVal.ToString());

            if (randVal < failChance)
            {
                Log.Message(pawn.Name + " messed up and lowered the item quality!");
            }
            else if (randVal < failChance + successChance)
            {
                Log.Message(pawn.Name + " succeeded and increased the item quality!");
            }
            else
            {
                Log.Message(pawn.Name + " tried to upgrade the item but nothing seems changed about it");
            }
        }

        private void DestroyPlacedResources()
        {
            // Despawn used resources
            foreach (Thing used in resourcesPlaced)
            {
                if (used.Destroyed)
                {
                    Log.Error("Tried to use up " + used.Label + " but it was already destroyed!");
                }
                else
                {
                    used.Destroy();
                }
            }
        }

        private Toil RecordPlacedResource(TargetIndex index)
        {
            return Toils_General.Do(() =>
            {
                resourcesPlaced.Add(job.GetTarget(index).Thing);
                Log.Message("Just placed " + job.GetTarget(index).Thing.stackCount + " " + job.GetTarget(index).Thing.def.label);
            });
        }

        private float GetSuccessChance(Pawn pawn, Thing thing)
        {
            QualityCategory quality;
            thing.TryGetQuality(out quality);

            float qualityChance;
            switch (quality)
            {
                case QualityCategory.Awful:
                    qualityChance = .95f;
                    break;
                case QualityCategory.Poor:
                    qualityChance = .9f;
                    break;
                case QualityCategory.Normal:
                    qualityChance = .85f;
                    break;
                case QualityCategory.Good:
                    qualityChance = .6f;
                    break;
                case QualityCategory.Excellent:
                    qualityChance = .25f;
                    break;
                case QualityCategory.Masterwork:
                    qualityChance = .15f;
                    break;
                default:
                    return 0;
            }

            int skillLevel = pawn.skills.GetSkill(ActiveSkillDef).Level;
            float skillPercent = skillLevel / 20f;
            return qualityChance * skillPercent;
        }

        private float GetFailChance(Pawn pawn, Thing thing)
        {
            QualityCategory quality;
            thing.TryGetQuality(out quality);

            float qualityChance = 0;
            switch (quality)
            {
                case QualityCategory.Awful:
                    return 0;
                case QualityCategory.Poor:
                    qualityChance = .05f;
                    break;
                case QualityCategory.Normal:
                    qualityChance = .1f;
                    break;
                case QualityCategory.Good:
                    qualityChance = .15f;
                    break;
                case QualityCategory.Excellent:
                    qualityChance = .2f;
                    break;
                case QualityCategory.Masterwork:
                    qualityChance = .25f;
                    break;
                default:
                    break;
            }

            int skillLevel = pawn.skills.GetSkill(ActiveSkillDef).Level;
            float skillPercent = (20 - skillLevel) / 20f;
            return qualityChance + (skillPercent * .15f);
        }

        private void RemoveDesignationsForQualityUpgrade(Thing t)
        {
            DesignationManager manager = Map.designationManager;
            manager.RemoveAllDesignationsOn(t);
        }

        private float TotalWorkNeeded
        {
            get
            {
                if (IsCraftingJob)
                {
                    return TargetC.Thing.def.recipeMaker.workAmount;
                }
                return Mathf.Clamp((TargetA.Thing as Building).GetStatValue(StatDefOf.WorkToBuild, true), 20f, 3000f);
            }
        }

        private float totalWorkNeeded;
        private float workLeft;
    }
}
