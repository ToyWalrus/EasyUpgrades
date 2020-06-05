using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;

namespace EasyUpgrades
{
    class CompUpgrade : ThingComp
    {
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            upgradeTo = Props.linkedThing;
            additionalRequiredResources = Props.additionalRequiredResources;
        }
        
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (this.parent.Faction == Faction.OfPlayer)
            {
                yield return new Command_ModifyThing
                {
                    icon = ContentFinder<Texture2D>.Get("UI/Up"),
                    defaultLabel = "EU.Upgrade".Translate(),
                    defaultDesc = Props.keyedTooltipString.Translate(),
                    modifyTo = this.upgradeTo,
                    currentThing = this.parent,
                    def = EasyUpgradesDesignationDefOf.Upgrade
                };
            }
        }

        public ThingDef upgradeTo;
        public List<ThingDefCountClass> additionalRequiredResources;

        public CompProperties_Upgradable Props
        {
            get
            {
                return (CompProperties_Upgradable)this.props;
            }
        }
    }
}
