using System.Collections.Generic;
using ArmorRacks.DefOfs;
using ArmorRacks.ThingComps;
using ArmorRacks.Things;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace ArmorRacks.Commands
{
    public class ArmorRackUseCommand : Command
    {
        public ArmorRack ArmorRack;
        public Pawn Pawn;

        public ArmorRackUseCommand(ArmorRack armorRack, Pawn pawn)
        {
            ArmorRack = armorRack;
            Pawn = pawn;
            icon = ContentFinder<Texture2D>.Get(armorRack.def.graphicData.texPath + "_south", false);
            defaultIconColor = armorRack.Stuff.stuffProps.color;
        }

        public override bool GroupsWith(Gizmo other)
        {
            return false;
        }

        public void Reset()
        {
            updateCount = 0;
            updateCountDesc = 0;
        }

        public static Dictionary<Pawn, ArmorRackUseCommandComp> cachedComps = new Dictionary<Pawn, ArmorRackUseCommandComp>();

        public string cachedStr = "";
        public int updateCount = 0;

        public string cachedStrDesc = "";
        public int updateCountDesc = 0;
        public override string Label
        {
            get
            {
                if (updateCount <= 0)
                {
                    updateCount = 60;
                    var str = "";
                    if (!cachedComps.TryGetValue(Pawn, out var comp))
                    {
                        cachedComps[Pawn] = comp = Pawn.GetComp<ArmorRackUseCommandComp>();
                    }
                    var selectedJobDef = comp.CurArmorRackJobDef(ArmorRack);
                    if (selectedJobDef == ArmorRacksJobDefOf.ArmorRacks_JobWearRack)
                    {
                        str += "ArmorRacks_WearRack_FloatMenuLabel".Translate();
                    }
                    else
                    {
                        str += "ArmorRacks_TransferToRack_FloatMenuLabel".Translate();
                    }
                    var additionalName = ArmorRack.AdditionalName;
                    if (additionalName.Length > 0)
                    {
                        cachedStr = str + " (" + additionalName.Truncate(156) + ")";
                    }
                    else
                    {
                        cachedStr = str;
                    }
                }
                updateCount--;
                return cachedStr;
            }
        }

        public override string Desc
        {
            get
            {

                if (updateCountDesc <= 0)
                {
                    updateCountDesc = 60;
                    var str = "";
                    if (!cachedComps.TryGetValue(Pawn, out var comp))
                    {
                        cachedComps[Pawn] = comp = Pawn.GetComp<ArmorRackUseCommandComp>();
                    }
                    var selectedJobDef = comp.CurArmorRackJobDef(ArmorRack);
                    if (selectedJobDef == ArmorRacksJobDefOf.ArmorRacks_JobWearRack)
                    {
                        str += "ArmorRacks_WearRack_FloatMenuLabel".Translate();
                    }
                    else
                    {
                        str += "ArmorRacks_TransferToRack_FloatMenuLabel".Translate();
                    }
                    var additionalName = ArmorRack.AdditionalName;
                    if (additionalName.Length > 0)
                    {
                        cachedStrDesc = str + " (" + additionalName + ")";
                    }
                    else
                    {
                        cachedStrDesc = str;
                    }
                }
                updateCountDesc--;
                return cachedStrDesc;
            }
        }

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                // Equip from
                var self = this;
                yield return new FloatMenuOption("ArmorRacks_WearRack_FloatMenuLabel".Translate(),
                    delegate
                    {
                        var comp = Pawn.GetComp<ArmorRackUseCommandComp>();
                        if (comp.armorRackJobs is null)
                        {
                            comp.armorRackJobs = new Dictionary<ArmorRack, JobDef>();
                        }
                        comp.armorRackJobs[ArmorRack] = ArmorRacksJobDefOf.ArmorRacks_JobWearRack;
                        Reset();
                    });
                
                // Transfer to
                yield return new FloatMenuOption("ArmorRacks_TransferToRack_FloatMenuLabel".Translate(),
                    delegate
                    {
                        var comp = Pawn.GetComp<ArmorRackUseCommandComp>();
                        if (comp.armorRackJobs is null)
                        {
                            comp.armorRackJobs = new Dictionary<ArmorRack, JobDef>();
                        }
                        comp.armorRackJobs[ArmorRack] = ArmorRacksJobDefOf.ArmorRacks_JobTransferToRack;
                        Reset();
                    });
            }
        }

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);
            var target_info = new LocalTargetInfo(ArmorRack);
            var selectedJobDef = Pawn.GetComp<ArmorRackUseCommandComp>().CurArmorRackJobDef(ArmorRack);
            var wearRackJob = new Job(selectedJobDef, target_info);
            Pawn.jobs.TryTakeOrderedJob(wearRackJob);
            Reset();
        }
    }
}