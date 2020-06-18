using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;

namespace EasyUpgrades
{
    class CompUpgrade : ThingComp
    {
        public CompProperties_Upgradable Props => (CompProperties_Upgradable)this.props;
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            upgradeTo = Props.linkedThing;
            additionalRequiredResources = Props.additionalRequiredResources;
        }
        
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent.Faction == Faction.OfPlayer)
            {
                if (!HasUpgradeDesignation)
                {
                    bool disabled = false;
                    if (Props.researchPrerequisite != null)
                    {
                        disabled = Find.ResearchManager.GetProgress(Props.researchPrerequisite) < Props.researchPrerequisite.baseCost;
                    }
                    yield return new Command_ModifyThing
                    {
                        icon = ContentFinder<Texture2D>.Get("UI/Up"),
                        defaultLabel = "EU.Upgrade".Translate(),
                        defaultDesc = Props.keyedTooltipString.Translate(),
                        disabled = disabled,
                        disabledReason = "EU.UnresearchedError".Translate(Props.researchPrerequisite.label),
                        currentThing = parent,
                        def = EasyUpgradesDesignationDefOf.Upgrade
                    };
                }
            }
        }
    
        public ThingDef upgradeTo;
        public List<ThingDefCountClass> additionalRequiredResources;
        private bool HasUpgradeDesignation => parent.Map.designationManager.DesignationOn(parent, EasyUpgradesDesignationDefOf.Upgrade) != null;
    }
}
