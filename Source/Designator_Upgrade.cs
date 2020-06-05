using RimWorld;
using Verse.AI;
using Verse;

namespace EasyUpgrades
{
    class Designator_Upgrade : Designator
    {
        protected override DesignationDef Designation => EasyUpgradesDesignationDefOf.Upgrade;

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            return false;
        }

        public override AcceptanceReport CanDesignateThing(Thing t)
        {
            return ModifiableThings.defNames.Contains(t.def.defName);
        }

        public override void DesignateThing(Thing t)
        {
            if (!CanDesignateThing(t).Accepted) return;

            var des = t.Map.designationManager.DesignationOn(t, Designation);
            if (des == null)
            {
                Log.Message("Added upgrade designation");
                t.Map.designationManager.AddDesignation(new Designation(t, Designation));
            }
            else
            {
                Log.Message("Removed upgrade designation");
                t.Map.designationManager.RemoveDesignation(des);
            }
        }
    }
}
