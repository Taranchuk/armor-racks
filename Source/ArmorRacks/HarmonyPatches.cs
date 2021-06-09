using ArmorRacks.DefOfs;
using ArmorRacks.ThingComps;
using ArmorRacks.Things;
using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ArmorRacks
{
	[StaticConstructorOnStartup]
	internal static class ModCompatibilityUtils
	{
		public static Type ammoThingType;
		public static Type toolThingType;
		public static Type compInventoryType;
		public static bool CELoaded()
        {
			return ammoThingType != null;
        }
		public static bool ToolsFrameworkLoaded()
        {
			return toolThingType != null;
        }

		public static bool IsAmmo(this Thing thing)
        {
			return ammoThingType.IsAssignableFrom(thing.GetType());
        }

		public static bool IsTool(this Thing thing)
        {
			return toolThingType.IsAssignableFrom(thing.GetType());
		}

		public static bool CanAcceptNewThing(this Pawn pawn, Thing thing)
        {
			var takenBulk = thing.def.GetStatValueAbstract(StatDef.Named("Bulk"));
			var availableBulk = pawn.GetAvailableBulk();
			if (takenBulk > availableBulk)
			{
				return false;
			}
			var takenWeight = thing.def.GetStatValueAbstract(StatDef.Named("Mass"));
			var availableWeight = pawn.GetAvailableWeight();
			if (takenWeight > availableWeight)
			{
				return false;
			}
			return true;
		}
		public static float GetAvailableBulk(this Pawn pawn)
        {
			foreach (var comp in pawn.AllComps)
            {
				if (compInventoryType.IsAssignableFrom(comp.GetType()))
                {
					return Traverse.Create(comp).Property("availableBulk").GetValue<float>();
                }
            }
			return 0f;
        }

		public static float GetAvailableWeight(this Pawn pawn)
		{
			foreach (var comp in pawn.AllComps)
			{
				if (compInventoryType.IsAssignableFrom(comp.GetType()))
				{
					return Traverse.Create(comp).Property("availableWeight").GetValue<float>();
				}
			}
			return 0f;
		}
		static ModCompatibilityUtils()
		{
			Harmony harmony = new Harmony("ArmorRacks.HarmonyPatches");
			harmony.PatchAll();
			ammoThingType = AccessTools.TypeByName("CombatExtended.AmmoThing");
			compInventoryType = AccessTools.TypeByName("CombatExtended.CompInventory");
			toolThingType = AccessTools.TypeByName("ToolsFramework.Tool");
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