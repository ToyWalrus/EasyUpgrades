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
        private bool IsArtisticJob => job.GetTarget(TargetIndex.B).Thing?.def?.defName == "TableSculpting";
        private SkillDef ActiveSkillDef => IsArtisticJob
            ? SkillDefOf.Artistic
            : IsCraftingJob
                ? SkillDefOf.Crafting
                : SkillDefOf.Construction;
        private StatDef ActiveStatDef => IsCraftingJob
            ? StatDefOf.WorkSpeedGlobal
            : StatDefOf.ConstructionSpeed;

        private Thing thingToWorkOn;

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
                    RemoveDesignationsForQualityUpgrade(IsCraftingJob ? thingToWorkOn : TargetA.Thing);
                    NotifyQualityChanged(modify.actor);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            yield break;
        }

        // Index A is first the item to be worked on, then the current resource to be gathered
        // Index B is the workstation to be worked at
        // Queue A is the resources to be gathered
        private IEnumerable<Toil> MakeToilsForCrafting()
        {
            this.FailOnForbidden(TargetIndex.A);
            this.FailOnForbidden(TargetIndex.B);
            Toil gotoWorkbench = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell).FailOnDestroyedNullOrForbidden(TargetIndex.B);
            Toil endGathering = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell).FailOnDestroyedNullOrForbidden(TargetIndex.B);

            // Take item to workbench
            Toil gotoNextHaulThing = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
            yield return gotoNextHaulThing;

            yield return SetThingToWorkOn();
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            yield return gotoWorkbench;

            Toil findPlaceTarget = Toils_JobTransforms.SetTargetToIngredientPlaceCell(TargetIndex.B, TargetIndex.A, TargetIndex.C);
            yield return findPlaceTarget;

            yield return Toils_Haul.PlaceHauledThingInCell(TargetIndex.C, findPlaceTarget, false);

            // Take items from queue to workbench
            yield return Toils_Jump.JumpIf(endGathering, () => job.GetTargetQueue(TargetIndex.A).NullOrEmpty());
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

            yield return endGathering;
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

        private Toil SetThingToWorkOn()
        {
            return Toils_General.Do(() =>
            {
                if (IsArtisticJob && !(job.targetA.Thing is MinifiedThing))
                {
                    LocalTargetInfo info = job.targetA;
                    Thing minifiedThing = job.targetA.Thing.TryMakeMinified();
                    if (GenPlace.TryPlaceThing(minifiedThing, info.Cell, Map, ThingPlaceMode.Direct))
                    {
                        thingToWorkOn = minifiedThing;
                        job.SetTarget(TargetIndex.A, thingToWorkOn);
                    }
                    else
                    {
                        Log.Error("Couldn't spawn the minified art thing!");
                    }
                }
                else
                {
                    thingToWorkOn = job.targetA.Thing;
                }
            });
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
            Thing thingModified = IsCraftingJob ? thingToWorkOn : TargetA.Thing;
            QualityCategory curQuality;
            if (!thingModified.TryGetQuality(out curQuality))
            {
                Log.Error("Unable to get comp quality on " + thingModified.Label);

                Log.Message("Other things A: " + (TargetA.Thing?.Label ?? "null"));
                Log.Message("Other things B: " + (TargetB.Thing?.Label ?? "null"));
                Log.Message("Other things C: " + (TargetC.Thing?.Label ?? "null"));
                return;
            }

            float successChance = EasyUpgrades.GetSuccessChance(pawn, ActiveSkillDef, thingModified);
            float failChance = EasyUpgrades.GetFailChance(pawn, ActiveSkillDef, thingModified);
            float randVal = Random.Range(0f, 1f);

            string msg;
            string itemLabel = thingModified.LabelNoCount;
            float xp;
            MessageTypeDef messageType;

            if (randVal < successChance)
            {
                msg = "EU.IncreaseQualityMessage_Success";
                xp = (int)curQuality * 80f;
                thingModified.TryGetComp<CompQuality>().SetQuality(curQuality + 1, ArtGenerationContext.Colony);
                thingModified.HitPoints = thingModified.MaxHitPoints;
                messageType = MessageTypeDefOf.PositiveEvent;
            }
            else if (randVal < successChance + failChance)
            {
                msg = "EU.IncreaseQualityMessage_Fail";
                xp = (int)curQuality * 40f;
                thingModified.TryGetComp<CompQuality>().SetQuality(curQuality - 1, ArtGenerationContext.Colony);
                thingModified.HitPoints -= Mathf.RoundToInt(thingModified.MaxHitPoints / 10f);
                messageType = MessageTypeDefOf.NegativeEvent;
            }
            else
            {
                msg = "EU.IncreaseQualityMessage_Neutral";
                xp = (int)curQuality * 50f;
                thingModified.HitPoints += Mathf.RoundToInt(thingModified.MaxHitPoints / 10f);
                messageType = MessageTypeDefOf.NeutralEvent;
            }

            pawn.skills.Learn(ActiveSkillDef, xp);
            Messages.Message(msg.Translate(pawn.NameShortColored, itemLabel.Substring(0, itemLabel.IndexOf("(") - 1), Mathf.Clamp(successChance, 0f, 1f).ToStringPercent()), pawn, messageType);
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

        private Toil RecordPlacedResource(TargetIndex index)
        {
            return Toils_General.Do(() =>
            {
                resourcesPlaced.Add(job.GetTarget(index).Thing);
            });
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
                    // StatWorker_MarketValue.CalculableRecipe(thingToWorkOn.def).workAmount
                    return thingToWorkOn.def.GetStatValueAbstract(StatDefOf.WorkToMake, thingToWorkOn.Stuff);
                }
                return Mathf.Clamp((TargetA.Thing as Building).GetStatValue(StatDefOf.WorkToBuild, true), 20f, 3000f);
            }
        }

        private float totalWorkNeeded;
        private float workLeft;
    }
}
