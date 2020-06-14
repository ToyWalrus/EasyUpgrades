using System;
using System.Collections.Generic;
using RimWorld;
using Verse.AI;

namespace EasyUpgrades
{
    class JobDriver_IncreaseQuality : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            throw new NotImplementedException();
        }
    }
}
