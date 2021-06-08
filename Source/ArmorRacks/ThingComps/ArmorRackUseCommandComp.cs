using System.Collections.Generic;
using System.Linq;
using ArmorRacks.Commands;
using ArmorRacks.DefOfs;
using ArmorRacks.Things;
using Verse;

namespace ArmorRacks.ThingComps
{
    public class ArmorRackUseCommandComp : ThingComp
    {
        public JobDef CurArmorRackJobDef = ArmorRacksJobDefOf.ArmorRacks_JobWearRack;
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Defs.Look(ref CurArmorRackJobDef, "CurArmorRackJobDef");
        }

        private static Dictionary<ArmorRack, ArmorRackUseCommand> cachedCommands = new Dictionary<ArmorRack, ArmorRackUseCommand>();
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent is Pawn pawn)
            {
                var racks = pawn.Map.listerBuildings.AllBuildingsColonistOfClass<ArmorRack>();
                foreach (var rack in racks)
                {
                    var c = rack.GetComp<CompAssignableToPawn_ArmorRacks>();
                    if (c.AssignedPawns.Contains(pawn))
                    {
                        if (!cachedCommands.TryGetValue(rack, out var command))
                        {
                            cachedCommands[rack] = command = new ArmorRackUseCommand(rack, pawn);
                        }
                        yield return command;
                    }
                }
            }
        }
    }
}