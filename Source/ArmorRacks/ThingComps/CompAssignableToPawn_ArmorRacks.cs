using System.Linq;
using ArmorRacks.Things;
using RimWorld;
using Verse;

namespace ArmorRacks.ThingComps
{
    public class CompAssignableToPawn_ArmorRacks : CompAssignableToPawn
    {
        public new int MaxAssignedPawnsCount => 1;
        public override void TryAssignPawn(Pawn pawn)
        {
            if (this.assignedPawns.Contains(pawn))
                return;
            assignedPawns.Add(pawn);
            this.SortAssignedPawns();
        }

        public override bool AssignedAnything(Pawn pawn)
        {
            return false;
        }
    }
}