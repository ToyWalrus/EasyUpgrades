using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace EasyUpgrades
{
    class CompIncreaseQuality : ThingComp
    {
        private DesignationDef buildingDes = EasyUpgradesDesignationDefOf.IncreaseQuality_Building;
        private DesignationDef apparelDes = EasyUpgradesDesignationDefOf.IncreaseQuality_Apparel;
        private DesignationDef artDes = EasyUpgradesDesignationDefOf.IncreaseQuality_Art;
        private DesignationDef itemDes = EasyUpgradesDesignationDefOf.IncreaseQuality_Item;
        private bool HasAnyIncreaseQualityDesignation => HasIncreaseApparelQualityDes || HasIncreaseBuildingQualityDes || HasIncreaseArtQualityDes || HasIncreaseItemQualityDes;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction == Faction.OfPlayer)
            {
                if (parent.def.IsArt && !HasAnyIncreaseQualityDesignation)
                {
                    yield return CreateCommandForDesignation(new Designation(parent, artDes));                    
                }
                else if (parent is Building && !HasAnyIncreaseQualityDesignation)
                {
                    yield return CreateCommandForDesignation(new Designation(parent, buildingDes));
                }
                else if (parent.def.IsApparel && !HasAnyIncreaseQualityDesignation)
                {
                    yield return CreateCommandForDesignation(new Designation(parent, apparelDes));
                }
                else if (parent.def.IsWeapon && !HasAnyIncreaseQualityDesignation)
                {
                    yield return CreateCommandForDesignation(new Designation(parent, itemDes));
                }
            }
        }

        private Command CreateCommandForDesignation(Designation des)
        {
            return new Command_Action
            {
                icon = ContentFinder<Texture2D>.Get("UI/QualityUp"),
                defaultLabel = "EU.IncreaseQuality".Translate(),
                defaultDesc = "EU.TryIncreaseQualityTooltip".Translate(),
                disabled = parent.TryGetComp<CompQuality>()?.Quality == QualityCategory.Legendary,
                disabledReason = "EU.CannotIncreaseLegendaryQuality".Translate(),
                action = () =>
                {
                    parent.Map.designationManager.AddDesignation(des);
                }
            };
        }

        bool HasIncreaseBuildingQualityDes => parent.Map.designationManager.DesignationOn(parent, buildingDes) != null;
        bool HasIncreaseArtQualityDes => parent.Map.designationManager.DesignationOn(parent, artDes) != null;
        bool HasIncreaseItemQualityDes => parent.Map.designationManager.DesignationOn(parent, itemDes) != null;
        bool HasIncreaseApparelQualityDes => parent.Map.designationManager.DesignationOn(parent, apparelDes) != null;
    }
}
