using System;
using System.Collections.Generic;
using Verse;

namespace EasyUpgrades
{
    class CompProperties_Upgradable : CompProperties
    {
        public ResearchProjectDef researchPrerequisite;
        public ThingDef linkedThing;
        public string keyedTooltipString;
        public List<ThingDefCountClass> additionalRequiredResources;
        public List<ThingDefCountClass> refundedResources;

        public string linkedThingName
        {
            get
            {
                return linkedThing.label;
            }
        }
    }
}
