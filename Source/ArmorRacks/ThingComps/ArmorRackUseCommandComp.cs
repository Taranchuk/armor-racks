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
        public Dictionary<ArmorRack, JobDef> armorRackJobs = new Dictionary<ArmorRack, JobDef>();
        public JobDef CurArmorRackJobDef(ArmorRack armorRack)
        {
            if (armorRackJobs is null)
            {
                armorRackJobs = new Dictionary<ArmorRack, JobDef>();
            }
            if (armorRackJobs.TryGetValue(armorRack, out var job))
            {
                return job;
            }
            return ArmorRacksJobDefOf.ArmorRacks_JobTransferToRack;
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Collections.Look(ref armorRackJobs, "armorRackJobs", LookMode.Reference, LookMode.Def, ref armorRacksKeys, ref jobDefsValues);
        }

        private List<ArmorRack> armorRacksKeys;
        private List<JobDef> jobDefsValues;
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