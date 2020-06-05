using System;
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

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.Target, this.job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnForbidden(TargetIndex.A);
            if (getModifyToThing(TargetA.Thing) == null)
            {
                yield break;
            }

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell).FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            
            Toil modify = new Toil();

            modify.initAction = () =>
            {
                totalNeededWork = TotalNeededWork;
                workLeft = totalNeededWork;
            };

            modify.tickAction = () =>
            {
                workLeft -= modify.actor.GetStatValue(StatDefOf.ConstructionSpeed, true) * 1.3f;
                TickAction();
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
                    RemoveAndReplace(modify.actor);
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            
            yield break;
        }

        void RemoveAndReplace(Pawn pawn)
        {
            IntVec3 position = base.Building.Position;
            Rot4 rotation = base.Building.Rotation;
            BillStack currentBills = null;
            ThingDef modifyTo = getModifyToThing(TargetA.Thing);
            ThingDef madeOf = TargetA.Thing.Stuff;

            if (base.Building is Building_WorkTable)
            {
                currentBills = (base.Building as Building_WorkTable).BillStack;
            }

            base.Building.DeSpawn();

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

            GenSpawn.Spawn(newThing, position, Map, rotation, WipeMode.FullRefund, false);
        }
        
        protected override float TotalNeededWork
        {
            get
            {
                return Mathf.Clamp(base.Building.GetStatValue(StatDefOf.WorkToBuild, true), 20f, 3000f);
            }
        }


        protected abstract ThingDef getModifyToThing(Thing t);

        protected virtual List<ThingDef> getRefundedResources(Thing t) => null;

        protected virtual List<ThingDef> getAdditionalRequiredResources(Thing t) => null;
    }
}
