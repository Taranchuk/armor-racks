using ArmorRacks.DefOfs;
using ArmorRacks.ThingComps;
using ArmorRacks.Things;
using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ArmorRacks
{
	[StaticConstructorOnStartup]
	internal static class HarmonyInit
	{
		public static Type ammoThingType;
		public static bool CELoaded()
        {
			return ammoThingType != null;
        }

		public static bool IsAmmo(this Thing thing)
        {
			return ammoThingType.IsAssignableFrom(thing.GetType());
        }
		static HarmonyInit()
		{
			Harmony harmony = new Harmony("ArmorRacks.HarmonyPatches");
			harmony.PatchAll();
			ammoThingType = AccessTools.TypeByName("CombatExtended.AmmoThing");
		}
	}

	[HarmonyPatch(typeof(Pawn_JobTracker), "TryTakeOrderedJob_NewTemp")]
	public class TryTakeOrderedJob_NewTempPatch
	{
		private static bool Prefix(Pawn_JobTracker __instance, Pawn ___pawn, Job job, JobTag? tag = 0, bool requestQueueing = false)
		{
			if (job.def.defName == "GearUpAndGo")
            {
				var racks = ___pawn.Map.listerBuildings.AllBuildingsColonistOfClass<ArmorRack>();
				foreach (var rack in racks)
				{
					var c = rack.GetComp<CompAssignableToPawn_ArmorRacks>();
					if (c.AssignedPawns.Contains(___pawn))
					{
						var target_info = new LocalTargetInfo(rack);
						var wearRackJob = new Job(ArmorRacksJobDefOf.ArmorRacks_JobWearRack, target_info);
						___pawn.jobs.TryTakeOrderedJob(wearRackJob);
						___pawn.jobs.jobQueue.EnqueueLast(job, tag);
						return false;
					}
				}
            }
			return true;
		}
	}
}