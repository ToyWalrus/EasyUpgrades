using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace EasyUpgrades
{
    class CompDowngrade : ThingComp
    {
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
                yield return new Command_ModifyThing
                {
                    icon = ContentFinder<Texture2D>.Get("UI/Down"),
                    defaultLabel = "EU.Downgrade".Translate(),
                    defaultDesc = Props.keyedTooltipString.Translate(),      
                    modifyTo = this.downgradeTo,
                    currentThing = this.parent,
                    def = EasyUpgradesDesignationDefOf.Downgrade
                };
            }
        }

        public ThingDef downgradeTo;
        public List<ThingDefCountClass> refundedResources;

        public CompProperties_Upgradable Props
        {
            get
            {
                return (CompProperties_Upgradable)this.props;
            }
        }
    }
}
