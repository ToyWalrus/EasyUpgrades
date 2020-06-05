using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;

namespace EasyUpgrades
{
    public class Command_ModifyThing : Command
    {
        public ThingDef modifyTo;
        public ThingWithComps currentThing;
        public DesignationDef def;

        private DesignationDef uninstallDef => DesignationDefOf.Uninstall;
        private DesignationDef deconstructDef => DesignationDefOf.Deconstruct;

        // need to provide cancel ability too?
        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
            DesignationManager manager = currentThing.Map.designationManager;
            Designation modifyDesignation = manager.DesignationOn(currentThing, def);
            Designation uninstallDesignation = manager.DesignationOn(currentThing, uninstallDef);
            Designation deconstructDesignation = manager.DesignationOn(currentThing, deconstructDef);
            if (modifyDesignation == null)
            {
                if (uninstallDesignation != null)
                {                 
                    manager.TryRemoveDesignationOn(currentThing, uninstallDef);
                }
                if (deconstructDesignation != null)
                {                 
                    manager.TryRemoveDesignationOn(currentThing, deconstructDef);
                }
                manager.AddDesignation(new Designation(currentThing, def));
            }
        }
    }
}
