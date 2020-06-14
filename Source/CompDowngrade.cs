using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace EasyUpgrades
{
    class CompDowngrade : ThingComp
    {
        public CompProperties_Upgradable Props => (CompProperties_Upgradable)this.props;
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            downgradeTo = Props.linkedThing;
            refundedResources = Props.refundedResources;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (this.parent.Faction == Faction.OfPlayer)
            {                
                if (!HasDowngradeDesignation)
                {
                    yield return new Command_ModifyThing
                    {
                        icon = ContentFinder<Texture2D>.Get("UI/Down"),
                        defaultLabel = "EU.Downgrade".Translate(),
                        defaultDesc = Props.keyedTooltipString.Translate(),      
                        currentThing = parent,
                        def = EasyUpgradesDesignationDefOf.Downgrade
                    };
                }
            }
        }

        public ThingDef downgradeTo;
        public List<ThingDefCountClass> refundedResources;

        private bool HasDowngradeDesignation => parent.Map.designationManager.DesignationOn(parent, EasyUpgradesDesignationDefOf.Downgrade) != null;
    }
}
