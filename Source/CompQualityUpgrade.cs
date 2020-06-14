using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace EasyUpgrades
{
    class CompQualityUpgrade : ThingComp
    {
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction == Faction.OfPlayer)
            {
                if (!HasQualityIncreaseDesignation)
                {
                    yield return new Command_Action
                    {
                        icon = ContentFinder<Texture2D>.Get("UI/QualityUp"),
                        defaultLabel = "EU.IncreaseQuality".Translate(),
                        defaultDesc = "EU.TryIncreaseQualityTooltip".Translate(),
                        action = () =>
                        {
                            parent.Map.designationManager.AddDesignation(new Designation(parent, EasyUpgradesDesignationDefOf.IncreaseQuality));
                        }
                    };
                }
            }
        }

        bool HasQualityIncreaseDesignation => parent.Map.designationManager.DesignationOn(parent, EasyUpgradesDesignationDefOf.IncreaseQuality) != null;
    }
}
